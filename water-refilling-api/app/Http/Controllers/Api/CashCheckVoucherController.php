<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\CashCheckVoucher;
use App\Models\Rfp;
use App\Models\Account;
use App\Models\CashDisbursement;
use App\Models\JournalEntry;
use App\Services\JournalEntryService;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\Rule;
use Illuminate\Validation\ValidationException;

class CashCheckVoucherController extends Controller
{
    private function relations(): array
    {
        return ['rfp'];
    }

    private function rules(?CashCheckVoucher $voucher = null): array
    {
        $numberRule = Rule::unique(
            'cash_check_vouchers',
            'voucher_number'
        );

        if ($voucher) {
            $numberRule->ignore($voucher->id);
        }

        return [
            'voucher_number' => [
                $voucher ? 'sometimes' : 'required',
                'string',
                'max:50',
                $numberRule,
            ],

            'rfp_id' => [
                $voucher ? 'sometimes' : 'required',
                'integer',
                'exists:rfps,id',
            ],

            'voucher_date' => [
                $voucher ? 'sometimes' : 'required',
                'date',
            ],

            'payment_method' => [
                $voucher ? 'sometimes' : 'required',
                Rule::in(['Cash', 'Check']),
            ],

            'check_number' => [
                'sometimes',
                'nullable',
                'string',
                'max:100',
            ],

            'check_date' => [
                'sometimes',
                'nullable',
                'date',
            ],

            'bank_name' => [
                'sometimes',
                'nullable',
                'string',
                'max:150',
            ],

            'payee' => [
                $voucher ? 'sometimes' : 'required',
                'string',
                'max:150',
            ],

            'amount' => [
                $voucher ? 'sometimes' : 'required',
                'numeric',
                'decimal:0,2',
                'min:0.01',
            ],

            'currency' => [
                'sometimes',
                'required',
                'string',
                'max:10',
            ],

            'status' => ['prohibited'],

            'remarks' => [
                'sometimes',
                'nullable',
                'string',
            ],

            'prepared_by' => ['prohibited'],
            'prepared_by_user_id' => ['prohibited'],

            'prepared_date' => ['prohibited'],

            'approved_by' => ['prohibited'],

            'approved_date' => ['prohibited'],

            'released_by' => ['prohibited'],

            'released_date' => ['prohibited'],

            'received_by' => ['prohibited'],

            'received_date' => ['prohibited'],
        ];
    }

    private function validatePaymentMethod(array $data): void
    {
        if (($data['payment_method'] ?? null) === 'Cash') {
            if (!empty($data['check_number']) ||
                !empty($data['check_date']) ||
                !empty($data['bank_name'])) {
                abort(
                    response()->json([
                        'success' => false,
                        'message' => 'Cash vouchers must not contain check details.',
                    ], 422)
                );
            }
        }

        if (($data['payment_method'] ?? null) === 'Check') {
            if (empty($data['check_number']) ||
                empty($data['check_date']) ||
                empty($data['bank_name'])) {
                abort(
                    response()->json([
                        'success' => false,
                        'message' => 'Check vouchers require a check number, check date, and bank name.',
                    ], 422)
                );
            }
        }
    }

    public function index()
    {
        return response()->json([
            'success' => true,
            'data' => CashCheckVoucher::with($this->relations())
                ->latest()
                ->get(),
        ]);
    }

    public function store(Request $request)
    {
        $validated = $request->validate($this->rules());

        $this->validatePaymentMethod($validated);

        $voucher = DB::transaction(function () use ($validated, $request) {
            $rfp = Rfp::whereKey($validated['rfp_id'])->lockForUpdate()->firstOrFail();
            $this->validateRfp($rfp, $validated['amount'], $validated['currency'] ?? 'PHP');
            if ($rfp->cashCheckVoucher()->exists()) {
                throw ValidationException::withMessages(['rfp_id' => 'A voucher already exists for this RFP.']);
            }
            $validated['status'] = 'Draft';
            $validated['prepared_by'] = $request->user()->name;
            $validated['prepared_by_user_id'] = $request->user()->id;
            $validated['prepared_date'] = now()->toDateString();
            return CashCheckVoucher::create($validated);
        });

        return response()->json([
            'success' => true,
            'message' => 'Cash/Check Voucher created successfully.',
            'data' => $voucher->load($this->relations()),
        ], 201);
    }

    public function show(CashCheckVoucher $cashCheckVoucher)
    {
        return response()->json([
            'success' => true,
            'data' => $cashCheckVoucher->load($this->relations()),
        ]);
    }

    public function update(
        Request $request,
        CashCheckVoucher $cashCheckVoucher
    ) {
        if ($cashCheckVoucher->status !== 'Draft') {
            return response()->json([
                'success' => false,
                'message' => 'Released, paid, or cancelled vouchers cannot be edited.',
            ], 422);
        }

        $validated = $request->validate(
            $this->rules($cashCheckVoucher)
        );

        $this->validatePaymentMethod(array_merge(
            $cashCheckVoucher->toArray(),
            $validated
        ));

        $rfpId = $validated['rfp_id'] ?? $cashCheckVoucher->rfp_id;
        $amount = $validated['amount'] ?? $cashCheckVoucher->amount;

        DB::transaction(function () use ($cashCheckVoucher, $validated, $rfpId, $amount) {
            $voucher = CashCheckVoucher::whereKey($cashCheckVoucher->id)->lockForUpdate()->firstOrFail();
            if ($voucher->status !== 'Draft') {
                throw ValidationException::withMessages(['status' => 'Only Draft vouchers can be edited.']);
            }
            $rfp = Rfp::whereKey($rfpId)->lockForUpdate()->firstOrFail();
            $this->validateRfp($rfp, $amount, $validated['currency'] ?? $voucher->currency);
            if ($rfp->cashCheckVoucher()->where('id', '!=', $voucher->id)->exists()) {
                throw ValidationException::withMessages(['rfp_id' => 'A voucher already exists for this RFP.']);
            }
            $voucher->update($validated);
        });

        return response()->json([
            'success' => true,
            'message' => 'Cash/Check Voucher updated successfully.',
            'data' => $cashCheckVoucher->fresh()->load($this->relations()),
        ]);
    }

    public function destroy(CashCheckVoucher $cashCheckVoucher)
    {
        DB::transaction(function () use ($cashCheckVoucher) {
            $voucher = CashCheckVoucher::whereKey($cashCheckVoucher->id)->lockForUpdate()->firstOrFail();
            if ($voucher->status !== 'Draft') {
                throw ValidationException::withMessages(['status' => 'Only Draft vouchers can be deleted.']);
            }
            $voucher->delete();
        });

        return response()->json([
            'success' => true,
            'message' => 'Cash/Check Voucher deleted successfully.',
        ]);
    }

    private function validateRfp(Rfp $rfp, string|float|int $amount, string $currency): void
    {
        if ($rfp->status !== 'Approved') {
            throw ValidationException::withMessages(['rfp_id' => 'The linked RFP must be approved.']);
        }
        if (bccomp((string) $amount, (string) $rfp->amount, 2) !== 0) {
            throw ValidationException::withMessages(['amount' => 'Voucher amount must match the approved RFP amount.']);
        }
        if ($currency !== $rfp->currency) {
            throw ValidationException::withMessages(['currency' => 'Voucher currency must match the approved RFP currency.']);
        }
    }

    private function validateTransitionRequest(Request $request): void
    {
        $request->validate([
            'status' => ['prohibited'],
            'prepared_by' => ['prohibited'],
            'prepared_by_user_id' => ['prohibited'],
            'prepared_date' => ['prohibited'],
            'approved_by' => ['prohibited'],
            'approved_date' => ['prohibited'],
            'released_by' => ['prohibited'],
            'released_date' => ['prohibited'],
            'received_by' => ['prohibited'],
            'received_date' => ['prohibited'],
        ]);
    }

    public function approve(Request $request, CashCheckVoucher $cashCheckVoucher)
    {
        $this->validateTransitionRequest($request);
        $voucher = DB::transaction(function () use ($request, $cashCheckVoucher) {
            $voucher = CashCheckVoucher::whereKey($cashCheckVoucher->id)->lockForUpdate()->firstOrFail();
            $rfp = Rfp::whereKey($voucher->rfp_id)->lockForUpdate()->firstOrFail();
            if ($voucher->status !== 'Draft' || $rfp->user_id === null ||
                $rfp->user_id === $request->user()->id ||
                $voucher->prepared_by_user_id === $request->user()->id ||
                ($voucher->prepared_by_user_id === null && $voucher->prepared_by === $request->user()->name)) {
                throw ValidationException::withMessages(['status' => 'Voucher is not approvable by this user.']);
            }
            $this->validateRfp($rfp, $voucher->amount, $voucher->currency);
            $voucher->update(['status' => 'Approved', 'approved_by' => $request->user()->name, 'approved_date' => now()->toDateString()]);
            return $voucher->load($this->relations());
        });
        return response()->json(['success' => true, 'data' => $voucher]);
    }

    public function release(Request $request, CashCheckVoucher $cashCheckVoucher, JournalEntryService $journalEntryService)
    {
        $this->validateTransitionRequest($request);
        foreach (['cash_disbursements.create', 'cash_disbursements.approve', 'cash_disbursements.post', 'rfp.pay'] as $permission) {
            abort_unless($request->user()->can($permission), 403);
        }
        $validated = $request->validate(['payment_account_id' => ['required', 'integer', 'exists:accounts,id']]);
        $voucher = DB::transaction(function () use ($request, $cashCheckVoucher, $journalEntryService, $validated) {
            $voucher = CashCheckVoucher::whereKey($cashCheckVoucher->id)->lockForUpdate()->firstOrFail();
            $rfp = Rfp::whereKey($voucher->rfp_id)->lockForUpdate()->firstOrFail();
            if ($voucher->status !== 'Approved') {
                throw ValidationException::withMessages(['status' => 'Only Approved vouchers can be released.']);
            }
            $this->validateRfp($rfp, $voucher->amount, $voucher->currency);
            if ($voucher->currency !== 'PHP' || ! $rfp->account_id || strlen($voucher->voucher_number) > 30) {
                throw ValidationException::withMessages(['voucher' => 'PHP currency, a linked debit account, and a voucher number of at most 30 characters are required for disbursement posting.']);
            }
            $debitAccount = Account::whereKey($rfp->account_id)->where('is_active', true)->first();
            $paymentAccount = Account::whereKey($validated['payment_account_id'])->where('is_active', true)->first();
            if (! $debitAccount || ! $paymentAccount || $paymentAccount->account_type !== 'Asset' || $debitAccount->id === $paymentAccount->id) {
                throw ValidationException::withMessages(['payment_account_id' => 'Select a distinct active Asset payment account and an active RFP debit account.']);
            }
            if (CashDisbursement::where('voucher_number', $voucher->voucher_number)->exists()) {
                throw ValidationException::withMessages(['voucher_number' => 'This voucher already has a disbursement number.']);
            }
            $disbursement = CashDisbursement::create([
                'disbursement_date' => now()->toDateString(),
                'voucher_number' => $voucher->voucher_number,
                'payee' => $voucher->payee,
                'account_id' => $rfp->account_id,
                'payment_account_id' => $paymentAccount->id,
                'amount' => $voucher->amount,
                'payment_method' => $voucher->payment_method,
                'check_number' => $voucher->check_number,
                'remarks' => $voucher->remarks,
                'status' => 'Draft',
            ]);
            $entry = JournalEntry::create([
                'journal_number' => 'CD-' . $disbursement->id,
                'journal_date' => $disbursement->disbursement_date,
                'description' => 'Cash disbursement to ' . $disbursement->payee . ' (Voucher ' . $voucher->voucher_number . ')',
                'reference_type' => 'CashDisbursement',
                'reference_id' => $disbursement->id,
                'status' => 'Draft',
                'created_by' => $request->user()->id,
            ]);
            $entry->lines()->createMany([
                ['account_id' => $rfp->account_id, 'debit_amount' => $voucher->amount, 'credit_amount' => '0.00'],
                ['account_id' => $paymentAccount->id, 'debit_amount' => '0.00', 'credit_amount' => $voucher->amount],
            ]);
            $journalEntryService->post($entry);
            $disbursement->update(['status' => 'Posted']);
            $voucher->update(['status' => 'Released', 'released_by' => $request->user()->name, 'released_date' => now()->toDateString()]);
            $rfp->update(['status' => 'Paid']);
            return $voucher->load($this->relations());
        });
        return response()->json(['success' => true, 'data' => $voucher]);
    }

    public function pay(Request $request, CashCheckVoucher $cashCheckVoucher)
    {
        $this->validateTransitionRequest($request);
        $voucher = DB::transaction(function () use ($request, $cashCheckVoucher) {
            $voucher = CashCheckVoucher::whereKey($cashCheckVoucher->id)->lockForUpdate()->firstOrFail();
            if ($voucher->status !== 'Released') {
                throw ValidationException::withMessages(['status' => 'Only Released vouchers can be marked Paid.']);
            }
            $rfp = Rfp::whereKey($voucher->rfp_id)->lockForUpdate()->firstOrFail();
            if ($rfp->status !== 'Paid') {
                throw ValidationException::withMessages(['rfp_id' => 'The linked RFP must reflect the released payment.']);
            }
            $disbursement = CashDisbursement::where('voucher_number', $voucher->voucher_number)->first();
            if (! $disbursement || $disbursement->status !== 'Posted' ||
                bccomp((string) $disbursement->amount, (string) $voucher->amount, 2) !== 0 ||
                JournalEntry::where('reference_type', 'CashDisbursement')
                ->where('reference_id', $disbursement->id)->where('status', 'Posted')->count() !== 1) {
                throw ValidationException::withMessages(['journal_entry' => 'A posted disbursement journal is required.']);
            }
            $voucher->update(['status' => 'Paid', 'received_by' => $request->user()->name, 'received_date' => now()->toDateString()]);
            return $voucher->load($this->relations());
        });
        return response()->json(['success' => true, 'data' => $voucher]);
    }
}
