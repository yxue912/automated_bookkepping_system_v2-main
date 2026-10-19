<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Account;
use App\Models\AuditLog;
use App\Models\CashDisbursement;
use App\Models\CashCheckVoucher;
use App\Models\JournalEntry;
use App\Services\JournalEntryService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\ValidationException;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\Gate;

class CashDisbursementController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $query = CashDisbursement::with(['account', 'paymentAccount']);

        // Staff can see only transactions they created. Owner/Admin retain
        // the existing all-records view.
        if ($request->user()?->hasRole('Staff')) {
            $query->where('created_by', $request->user()->id);
        }

        return response()->json([
            'success' => true,
            'data' => $query->orderByDesc('disbursement_date')->get(),
        ]);
    }

    public function store(
        Request $request,
        JournalEntryService $journalEntryService
    ): JsonResponse {
        $validated = $request->validate([
            'disbursement_date' => ['required', 'date'],
            'payee' => ['required', 'string', 'max:150'],
            'account_id' => ['required', 'integer', 'exists:accounts,id'],
            'payment_account_id' => [
                'required',
                'integer',
                'different:account_id',
                'exists:accounts,id',
            ],
            'amount' => [
                'required',
                'numeric',
                'decimal:0,2',
                'min:0.01',
            ],
            'payment_method' => [
                'required',
                'string',
                'in:Cash,Check,Bank Transfer',
            ],
            'check_number' => [
                'nullable',
                'string',
                'max:50',
                'required_if:payment_method,Check',
                'prohibited_unless:payment_method,Check',
            ],
            'remarks' => ['nullable', 'string'],
            'post_now' => ['sometimes', 'boolean'],
        ]);

        $postNow = (bool) ($validated['post_now'] ?? false);
        unset($validated['post_now']);

        $disbursement = DB::transaction(function () use (
            $validated,
            $request,
            $journalEntryService,
            $postNow
        ) {
            $debitAccount = Account::whereKey($validated['account_id'])
                ->where('is_active', true)
                ->first();

            $paymentAccount = Account::whereKey(
                $validated['payment_account_id']
            )
                ->where('is_active', true)
                ->first();

            if (
                ! $debitAccount ||
                ! $paymentAccount ||
                $paymentAccount->account_type !== 'Asset' ||
                $debitAccount->id === $paymentAccount->id
            ) {
                throw ValidationException::withMessages([
                    'account_id' =>
                        'Select distinct active debit and Asset payment accounts.',
                ]);
            }

            // Generate an unpredictable voucher number on the server.
            // The database unique index remains the final duplicate safeguard.
            $disbursementData = $validated;
            $disbursementData['voucher_number'] = $this->generateUniqueVoucherNumber();
            $disbursementData['status'] = 'Draft';
            // Always assign ownership from the authenticated session, never request input.
            $disbursementData['created_by'] = $request->user()->id;

            $disbursement = CashDisbursement::create(
                $disbursementData
            );

            $entry = JournalEntry::create([
                'journal_number' => 'CD-' . $disbursement->id,
                'journal_date' => $validated['disbursement_date'],
                'description' => 'Cash disbursement voucher '
                    . $disbursement->voucher_number
                    . ' - '
                    . $validated['payee'],
                'reference_type' => 'CashDisbursement',
                'reference_id' => $disbursement->id,
                'status' => 'Draft',
                'created_by' => $request->user()?->id,
            ]);

            $amount = bcadd(
                (string) $validated['amount'],
                '0',
                2
            );

            $entry->lines()->createMany([
                [
                    'account_id' => $debitAccount->id,
                    'description' => 'Payment expense - '
                        . $validated['payee'],
                    'debit_amount' => $amount,
                    'credit_amount' => '0.00',
                ],
                [
                    'account_id' => $paymentAccount->id,
                    'description' => 'Cash paid - '
                        . $validated['payee'],
                    'debit_amount' => '0.00',
                    'credit_amount' => $amount,
                ],
            ]);

            if ($postNow) {
                abort_unless(
                    $request->user() &&
                    $request->user()->can(
                        'cash_disbursements.approve'
                    ),
                    403
                );

                $journalEntryService->post($entry);
                $disbursement->update(['status' => 'Posted']);
            }

            return $disbursement->load([
                'account',
                'paymentAccount',
            ]);
        });

        if ($postNow) {
            $user = $request->user();

            AuditLog::create([
                'user_id' => $user ? $user->id : null,
                'event' => 'cash_disbursement.posted',
                'description' => 'Cash disbursement voucher '
                    . $disbursement->voucher_number
                    . ' created and posted successfully.',
                'ip_address' => $request->ip(),
                'user_agent' => $request->userAgent(),
                'metadata' => [
                    'cash_disbursement_id' => $disbursement->id,
                    'voucher_number' => $disbursement->voucher_number,
                ],
            ]);
        }

        return response()->json([
            'success' => true,
            'message' => $postNow
                ? 'Cash disbursement created and journal entry posted successfully.'
                : 'Cash disbursement saved as Draft.',
            'data' => $disbursement,
        ], 201);
    }

    public function post(
        CashDisbursement $cashDisbursement,
        JournalEntryService $journalEntryService
    ): JsonResponse {
        $this->authorizeRecordAccess($cashDisbursement);
        $user = Auth::user();

        $user = Auth::user();

        abort_unless(
            $user !== null &&
            Gate::forUser($user)->allows('cash_disbursements.approve'),
            403
    );

        $postedDisbursement = DB::transaction(function () use (
            $cashDisbursement,
            $journalEntryService
        ) {
            $disbursement = CashDisbursement::whereKey(
                $cashDisbursement->id
            )
                ->lockForUpdate()
                ->firstOrFail();

            if ($disbursement->status !== 'Draft') {
                throw ValidationException::withMessages([
                    'status' =>
                        'Only Draft disbursements can be posted.',
                ]);
            }

            $entries = JournalEntry::where(
                'reference_type',
                'CashDisbursement'
            )
                ->where('reference_id', $disbursement->id)
                ->lockForUpdate()
                ->get();

            if (
                $entries->count() !== 1 ||
                $entries->first()->status !== 'Draft'
            ) {
                throw ValidationException::withMessages([
                    'journal_entry' =>
                        'This disbursement requires exactly one Draft journal entry.',
                ]);
            }

            $journalEntryService->post($entries->first());

            $disbursement->update(['status' => 'Posted']);

            return $disbursement->load([
                'account',
                'paymentAccount',
            ]);
        });

        AuditLog::create([
            'user_id' => $user->id,
            'event' => 'cash_disbursement.posted',
            'description' => 'Cash disbursement voucher '
                . $postedDisbursement->voucher_number
                . ' posted successfully.',
            'ip_address' => request()->ip(),
            'user_agent' => request()->userAgent(),
            'metadata' => [
                'cash_disbursement_id' => $postedDisbursement->id,
                'voucher_number' => $postedDisbursement->voucher_number,
            ],
        ]);

        return response()->json([
            'success' => true,
            'message' => 'Cash disbursement posted successfully.',
            'data' => $postedDisbursement,
        ]);
    }

    public function show(
        CashDisbursement $cashDisbursement
    ): JsonResponse {
        $this->authorizeRecordAccess($cashDisbursement);

        return response()->json([
            'success' => true,
            'data' => $cashDisbursement->load([
                'account',
                'paymentAccount',
            ]),
        ]);
    }

    /** Generate CV- plus eight random digits, checking both voucher tables. */
    private function generateUniqueVoucherNumber(): string
    {
        for ($attempt = 0; $attempt < 20; $attempt++) {
            $candidate = 'CV-' . str_pad(
                (string) random_int(0, 99999999),
                8,
                '0',
                STR_PAD_LEFT
            );

            $existsInDisbursements = CashDisbursement::where(
                'voucher_number',
                $candidate
            )->exists();
            $existsInCheckVouchers = CashCheckVoucher::where(
                'voucher_number',
                $candidate
            )->exists();

            if (! $existsInDisbursements && ! $existsInCheckVouchers) {
                return $candidate;
            }
        }

        throw ValidationException::withMessages([
            'voucher_number' => 'A unique voucher number could not be generated. Please try saving again.',
        ]);
    }

    public function update(
        Request $request,
        CashDisbursement $cashDisbursement
    ): JsonResponse {
        $this->authorizeRecordAccess($cashDisbursement);

        $validated = $request->validate([
            'disbursement_date' => ['required', 'date'],
            'payee' => ['required', 'string', 'max:150'],
            'account_id' => ['required', 'integer', 'exists:accounts,id'],
            'payment_account_id' => [
                'required',
                'integer',
                'different:account_id',
                'exists:accounts,id',
            ],
            'amount' => [
                'required',
                'numeric',
                'decimal:0,2',
                'min:0.01',
            ],
            'payment_method' => [
                'required',
                'string',
                'in:Cash,Check,Bank Transfer',
            ],
            'check_number' => [
                'nullable',
                'string',
                'max:50',
                'required_if:payment_method,Check',
                'prohibited_unless:payment_method,Check',
            ],
            'remarks' => ['nullable', 'string'],
        ]);

        DB::transaction(function () use (
            $cashDisbursement,
            $validated
        ) {
            $disbursement = CashDisbursement::whereKey(
                $cashDisbursement->id
            )
                ->lockForUpdate()
                ->firstOrFail();

            if ($disbursement->status !== 'Draft') {
                throw ValidationException::withMessages([
                    'status' =>
                        'Only Draft disbursements can be edited.',
                ]);
            }

            if (
                CashCheckVoucher::where(
                    'voucher_number',
                    $disbursement->voucher_number
                )->exists()
            ) {
                throw ValidationException::withMessages([
                    'voucher_number' =>
                        'Release this cash/check voucher through its approved voucher workflow.',
                ]);
            }

            $debitAccount = Account::whereKey(
                $validated['account_id']
            )
                ->where('is_active', true)
                ->first();

            $paymentAccount = Account::whereKey(
                $validated['payment_account_id']
            )
                ->where('is_active', true)
                ->first();

            if (
                ! $debitAccount ||
                ! $paymentAccount ||
                $paymentAccount->account_type !== 'Asset' ||
                $debitAccount->id === $paymentAccount->id
            ) {
                throw ValidationException::withMessages([
                    'account_id' =>
                        'Select distinct active debit and Asset payment accounts.',
                ]);
            }

            $entries = JournalEntry::where(
                'reference_type',
                'CashDisbursement'
            )
                ->where('reference_id', $disbursement->id)
                ->lockForUpdate()
                ->get();

            if (
                $entries->count() !== 1 ||
                $entries->first()->status !== 'Draft'
            ) {
                throw ValidationException::withMessages([
                    'journal_entry' =>
                        'This disbursement requires exactly one Draft journal entry.',
                ]);
            }

            $entry = $entries->first();

            $disbursement->update($validated);

            $entry->update([
                'journal_date' => $validated['disbursement_date'],
                'description' => 'Cash disbursement voucher '
                    . $disbursement->voucher_number
                    . ' - '
                    . $validated['payee'],
            ]);

            $amount = bcadd(
                (string) $validated['amount'],
                '0',
                2
            );

            $lines = $entry->lines()->orderBy('id')->get();

            if ($lines->count() !== 2) {
                throw ValidationException::withMessages([
                    'journal_entry' =>
                        'Expected two journal lines for this disbursement.',
                ]);
            }

            $lines[0]->update([
                'account_id' => $debitAccount->id,
                'description' => 'Payment expense - '
                    . $validated['payee'],
                'debit_amount' => $amount,
                'credit_amount' => '0.00',
            ]);

            $lines[1]->update([
                'account_id' => $paymentAccount->id,
                'description' => 'Cash paid - '
                    . $validated['payee'],
                'debit_amount' => '0.00',
                'credit_amount' => $amount,
            ]);
        });

        return response()->json([
            'success' => true,
            'message' => 'Draft disbursement updated successfully.',
            'data' => $cashDisbursement->fresh()->load([
                'account',
                'paymentAccount',
            ]),
        ]);
    }

    public function destroy(
        CashDisbursement $cashDisbursement
    ): JsonResponse {
        $this->authorizeRecordAccess($cashDisbursement);

        if ($cashDisbursement->status !== 'Draft') {
            throw ValidationException::withMessages([
                'status' =>
                    'Only Draft disbursements can be deleted.',
            ]);
        }

        DB::transaction(function () use ($cashDisbursement) {
            $disbursement = CashDisbursement::whereKey(
                $cashDisbursement->id
            )
                ->lockForUpdate()
                ->firstOrFail();

            if ($disbursement->status !== 'Draft') {
                throw ValidationException::withMessages([
                    'status' =>
                        'Only Draft disbursements can be deleted.',
                ]);
            }

            $entries = JournalEntry::where(
                'reference_type',
                'CashDisbursement'
            )
                ->where('reference_id', $disbursement->id)
                ->lockForUpdate()
                ->get();

            if (
                $entries->count() !== 1 ||
                $entries->first()->status !== 'Draft'
            ) {
                throw ValidationException::withMessages([
                    'journal_entry' =>
                        'This disbursement requires exactly one Draft journal entry.',
                ]);
            }

            $entry = $entries->first();
            $entry->lines()->delete();
            $entry->delete();

            $disbursement->delete();
        });

        return response()->json([
            'success' => true,
            'message' => 'Draft disbursement deleted successfully.',
        ]);
    }

    /** Prevent Staff from viewing or changing another user's transaction. */
    private function authorizeRecordAccess(CashDisbursement $disbursement): void
    {
        $user = request()->user();

        abort_unless(
            $user !== null &&
            (! $user->hasRole('Staff') || (int) $disbursement->created_by === (int) $user->id),
            403,
            'You may only access your own cash disbursements.'
        );
    }

}