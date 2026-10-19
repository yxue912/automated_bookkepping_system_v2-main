<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Rfp;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\Rule;
use Illuminate\Validation\ValidationException;

class RfpController extends Controller
{
    private function relations(): array
    {
        return ['account', 'user', 'details.account'];
    }

    private function rules(?Rfp $rfp = null): array
    {
        $numberRule = Rule::unique('rfps', 'rfp_number');

        if ($rfp) {
            $numberRule->ignore($rfp->id);
        }

        return [
            'rfp_number' => [$rfp ? 'sometimes' : 'required', 'string', 'max:30', $numberRule],
            'rfp_date' => [$rfp ? 'sometimes' : 'required', 'date'],
            'requested_by' => [$rfp ? 'sometimes' : 'required', 'string', 'max:150'],
            'payee' => [$rfp ? 'sometimes' : 'required', 'string', 'max:150'],
            'purpose' => [$rfp ? 'sometimes' : 'required', 'string'],
            'amount' => [$rfp ? 'sometimes' : 'required', 'numeric', 'decimal:0,2', 'min:0.01'],
            'status' => ['prohibited'],
            'remarks' => ['sometimes', 'nullable', 'string'],
            'account_id' => ['sometimes', 'nullable', 'integer', 'exists:accounts,id'],
            'user_id' => ['prohibited'],

            'reference_invoice_no' => ['sometimes', 'nullable', 'string', 'max:100'],
            'tax_identification_number' => ['sometimes', 'nullable', 'string', 'max:30'],
            'currency' => ['sometimes', 'required', 'string', 'max:10'],

            'prepared_by' => ['prohibited'],
            'prepared_by_date' => ['prohibited'],
            'reviewed_by' => ['prohibited'],
            'reviewed_by_date' => ['prohibited'],
            'approved_by' => ['prohibited'],
            'approved_by_date' => ['prohibited'],
            'requested_by_date' => ['prohibited'],
            'received_by' => ['prohibited'],
            'received_by_date' => ['prohibited'],

            'details' => ['sometimes', 'array'],
            'details.*.account_id' => ['nullable', 'integer', 'exists:accounts,id'],
            'details.*.account_code' => ['nullable', 'string', 'max:30'],
            'details.*.account_title' => ['nullable', 'string', 'max:150'],
            'details.*.location_project' => ['nullable', 'string', 'max:150'],
            'details.*.debit_amount' => ['required_with:details', 'numeric', 'decimal:0,2', 'min:0'],
            'details.*.credit_amount' => ['required_with:details', 'numeric', 'decimal:0,2', 'min:0'],
        ];
    }

    public function index()
    {
        return response()->json([
            'success' => true,
            'data' => Rfp::with($this->relations())->latest()->get(),
        ]);
    }

    public function store(Request $request)
    {
        $validated = $request->validate($this->rules());
        $validated['status'] = 'Draft';
        $validated['user_id'] = $request->user()->id;
        $validated['prepared_by'] = $request->user()->name;
        $validated['prepared_by_date'] = now()->toDateString();

        $details = $validated['details'] ?? [];
        unset($validated['details']);

        $rfp = DB::transaction(function () use ($validated, $details) {
            $rfp = Rfp::create($validated);

            if (!empty($details)) {
                $rfp->details()->createMany($details);
            }

            return $rfp;
        });

        return response()->json([
            'success' => true,
            'message' => 'RFP / voucher created successfully.',
            'data' => $rfp->load($this->relations()),
        ], 201);
    }

    public function show(Rfp $rfp)
    {
        return response()->json([
            'success' => true,
            'data' => $rfp->load($this->relations()),
        ]);
    }

    public function update(Request $request, Rfp $rfp)
    {
        $validated = $request->validate($this->rules($rfp));

        $hasDetails = array_key_exists('details', $validated);
        $details = $validated['details'] ?? [];
        unset($validated['details']);

        DB::transaction(function () use ($rfp, $validated, $hasDetails, $details) {
            $rfp = Rfp::whereKey($rfp->id)->lockForUpdate()->firstOrFail();
            if (! in_array($rfp->status, ['Draft', 'Rejected'], true)) {
                throw ValidationException::withMessages(['status' => 'Only Draft or Rejected requests can be edited.']);
            }
            if ($rfp->status === 'Rejected' && array_key_exists('remarks', $validated)) {
                throw ValidationException::withMessages(['remarks' => 'The documented rejection reason cannot be changed.']);
            }
            $rfp->update($validated);

            if ($hasDetails) {
                $rfp->details()->delete();

                if (!empty($details)) {
                    $rfp->details()->createMany($details);
                }
            }
        });

        return response()->json([
            'success' => true,
            'message' => 'RFP / voucher updated successfully.',
            'data' => $rfp->fresh()->load($this->relations()),
        ]);
    }

    public function destroy(Rfp $rfp)
    {
        DB::transaction(function () use ($rfp) {
            $rfp = Rfp::whereKey($rfp->id)->lockForUpdate()->firstOrFail();
            if ($rfp->status !== 'Draft' || $rfp->cashCheckVoucher()->exists()) {
                throw ValidationException::withMessages(['status' => 'Only an unvouchered Draft request can be deleted.']);
            }
            $rfp->details()->delete();
            $rfp->delete();
        });

        return response()->json([
            'success' => true,
            'message' => 'RFP / voucher deleted successfully.',
        ]);
    }

    private function transition(Rfp $rfp, array $from, string $to, array $attributes = [], ?int $approverId = null): Rfp
    {
        return DB::transaction(function () use ($rfp, $from, $to, $attributes, $approverId) {
            $rfp = Rfp::whereKey($rfp->id)->lockForUpdate()->firstOrFail();
            if (! in_array($rfp->status, $from, true)) {
                throw ValidationException::withMessages(['status' => 'Invalid RFP status transition.']);
            }
            if ($approverId !== null && ($rfp->user_id === null || $rfp->user_id === $approverId)) {
                throw ValidationException::withMessages(['approver' => 'A distinct, identified requester is required for approval.']);
            }
            if ($to === 'Submitted' && $rfp->details()->exists()) {
                $debits = '0.00';
                $credits = '0.00';
                foreach ($rfp->details as $detail) {
                    $hasDebit = bccomp((string) $detail->debit_amount, '0', 2) > 0;
                    $hasCredit = bccomp((string) $detail->credit_amount, '0', 2) > 0;
                    if ($hasDebit === $hasCredit) {
                        throw ValidationException::withMessages(['details' => 'Each detail line requires one nonzero debit or credit.']);
                    }
                    $debits = bcadd($debits, (string) $detail->debit_amount, 2);
                    $credits = bcadd($credits, (string) $detail->credit_amount, 2);
                }
                if (bccomp($debits, $credits, 2) !== 0 || bccomp($debits, (string) $rfp->amount, 2) !== 0) {
                    throw ValidationException::withMessages(['details' => 'Detail debits and credits must each equal the request amount.']);
                }
            }
            $rfp->update(array_merge($attributes, ['status' => $to]));
            return $rfp->load($this->relations());
        });
    }

    private function validateTransitionRequest(Request $request): void
    {
        $request->validate([
            'status' => ['prohibited'],
            'user_id' => ['prohibited'],
            'prepared_by' => ['prohibited'],
            'prepared_by_date' => ['prohibited'],
            'reviewed_by' => ['prohibited'],
            'reviewed_by_date' => ['prohibited'],
            'approved_by' => ['prohibited'],
            'approved_by_date' => ['prohibited'],
            'requested_by_date' => ['prohibited'],
            'received_by' => ['prohibited'],
            'received_by_date' => ['prohibited'],
        ]);
    }

    public function submit(Request $request, Rfp $rfp)
    {
        $this->validateTransitionRequest($request);
        $rfp = $this->transition($rfp, ['Draft', 'Rejected'], 'Submitted', [
            'reviewed_by' => null,
            'reviewed_by_date' => null,
        ]);
        return response()->json(['success' => true, 'data' => $rfp]);
    }

    public function review(Request $request, Rfp $rfp)
    {
        $this->validateTransitionRequest($request);
        $rfp = $this->transition($rfp, ['Submitted'], 'Under Review', [
            'reviewed_by' => $request->user()->name,
            'reviewed_by_date' => now()->toDateString(),
        ]);
        return response()->json(['success' => true, 'data' => $rfp]);
    }

    public function approve(Request $request, Rfp $rfp)
    {
        $this->validateTransitionRequest($request);
        $rfp = $this->transition($rfp, ['Under Review'], 'Approved', [
            'approved_by' => $request->user()->name,
            'approved_by_date' => now()->toDateString(),
        ], $request->user()->id);
        return response()->json(['success' => true, 'data' => $rfp]);
    }

    public function reject(Request $request, Rfp $rfp)
    {
        $this->validateTransitionRequest($request);
        $validated = $request->validate(['reason' => ['required', 'string', 'min:3']]);
        $rfp = $this->transition($rfp, ['Under Review'], 'Rejected', [
            'remarks' => $validated['reason'],
        ]);
        return response()->json(['success' => true, 'data' => $rfp]);
    }
}
