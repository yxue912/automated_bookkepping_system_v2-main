<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Account;
use App\Models\CashReceipt;
use App\Models\JournalEntry;
use App\Services\JournalEntryService;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\ValidationException;

class CashReceiptController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $query = CashReceipt::with(['account', 'depositAccount']);

        // Staff can see only transactions they created. Owner/Admin retain
        // the existing all-records view.
        if ($request->user()?->hasRole('Staff')) {
            $query->where('created_by', $request->user()->id);
        }

        return response()->json([
            'success' => true,
            'data' => $query->orderByDesc('receipt_date')->get(),
        ]);
    }

    public function store(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'receipt_date' => ['required', 'date'],
            'payor' => ['required', 'string', 'max:150'],
            'account_id' => ['required', 'integer', 'exists:accounts,id'],
            'deposit_account_id' => [
                'required',
                'integer',
                'exists:accounts,id',
            ],
            'amount' => ['required', 'numeric', 'decimal:0,2', 'min:0.01'],
            'bank_deposited' => ['nullable', 'string', 'max:150'],
            'remarks' => ['nullable', 'string'],
        ]);

        $receipt = DB::transaction(function () use ($validated, $request) {
            $depositAccount = Account::findOrFail(
                $validated['deposit_account_id']
            );

            $creditAccount = Account::findOrFail(
                $validated['account_id']
            );

            if (! $depositAccount->is_active ||
                ! $creditAccount->is_active) {
                throw ValidationException::withMessages([
                    'account_id' => 'Both selected accounts must be active.',
                ]);
            }

            if ($depositAccount->account_type !== 'Asset') {
                throw ValidationException::withMessages([
                    'deposit_account_id' =>
                        'The deposit account must be an Asset account.',
                ]);
            }

            if ($depositAccount->id === $creditAccount->id) {
                throw ValidationException::withMessages([
                    'account_id' =>
                        'The deposit account and GL account must differ.',
                ]);
            }

            if (! in_array(
                $creditAccount->account_type,
                ['Revenue', 'Liability', 'Equity'],
                true
            )) {
                throw ValidationException::withMessages([
                    'account_id' =>
                        'Cash receipts must use a Revenue, Liability, or Equity account.',
                ]);
            }

            // Create the record first to obtain its unique database ID.
            // The temporary value is replaced before the transaction commits.
            $receiptData = $validated;
            $receiptData['or_number'] = 'TMP-' . bin2hex(random_bytes(10));
            $receiptData['status'] = 'Draft';
            // Ownership always comes from the authenticated session.
            $receiptData['created_by'] = $request->user()->id;

            $receipt = CashReceipt::create($receiptData);

            // Generate the OR number from the unique receipt ID.
            // If an older record already uses the base number, add a suffix.
            $baseNumber = (int) $receipt->id;
            $baseOrNumber = 'OR-' . str_pad(
                (string) $baseNumber,
                4,
                '0',
                STR_PAD_LEFT
            );
            $orNumber = $baseOrNumber;
            $suffix = 1;

            while (CashReceipt::where('or_number', $orNumber)
                ->where('id', '!=', $receipt->id)
                ->exists()) {
                $orNumber = $baseOrNumber . '-' . $suffix;
                $suffix++;
            }

            $receipt->or_number = $orNumber;
            $receipt->save();

            $entry = JournalEntry::create([
                'journal_number' => 'CR-' . $receipt->id,
                'journal_date' => $validated['receipt_date'],
                'description' => 'Cash receipt OR '
                    . $receipt->or_number
                    . ' - '
                    . $validated['payor'],
                'reference_type' => 'CashReceipt',
                'reference_id' => $receipt->id,
                'status' => 'Draft',
                'created_by' => $request->user()?->id,
            ]);

            $amount = bcadd((string) $validated['amount'], '0', 2);

            $entry->lines()->createMany([
                [
                    'account_id' => $depositAccount->id,
                    'description' => 'Cash received - '
                        . $validated['payor'],
                    'debit_amount' => $amount,
                    'credit_amount' => '0.00',
                ],
                [
                    'account_id' => $creditAccount->id,
                    'description' => 'Receipt income/account - '
                        . $validated['payor'],
                    'debit_amount' => '0.00',
                    'credit_amount' => $amount,
                ],
            ]);

            return $receipt->load([
                'account',
                'depositAccount',
            ]);
        });

        return response()->json([
            'success' => true,
            'message' => 'Cash receipt saved as Draft.',
            'data' => $receipt,
        ], 201);
    }

    public function post(
        CashReceipt $cashReceipt,
        JournalEntryService $journalEntryService
    ): JsonResponse {
        $this->authorizeRecordAccess($cashReceipt);

        return DB::transaction(function () use (
            $cashReceipt,
            $journalEntryService
        ) {
            $receipt = CashReceipt::whereKey($cashReceipt->id)
                ->lockForUpdate()
                ->firstOrFail();

            if ($receipt->status !== 'Draft') {
                throw ValidationException::withMessages([
                    'status' => 'Only Draft receipts can be posted.',
                ]);
            }

            $entries = JournalEntry::where(
                'reference_type',
                'CashReceipt'
            )
                ->where('reference_id', $receipt->id)
                ->lockForUpdate()
                ->get();

            if ($entries->count() !== 1 ||
                $entries->first()->status !== 'Draft') {
                throw ValidationException::withMessages([
                    'journal_entry' =>
                        'This receipt requires exactly one Draft journal entry.',
                ]);
            }

            $journalEntryService->post($entries->first());

            $receipt->status = 'Posted';
            $receipt->save();

            // Record the audit event in the same transaction as posting.
            $now = now();

            DB::table('audit_logs')->insert([
                'user_id' => request()->user()?->id,
                'event' => 'cash_receipt.posted',
                'description' => 'Cash receipt OR '
                    . $receipt->or_number
                    . ' posted successfully.',
                'ip_address' => request()->ip(),
                'user_agent' => request()->userAgent(),
                'metadata' => json_encode([
                    'cash_receipt_id' => $receipt->id,
                    'or_number' => $receipt->or_number,
                    'amount' => $receipt->amount,
                ], JSON_THROW_ON_ERROR),
                'created_at' => $now,
                'updated_at' => $now,
            ]);

            return response()->json([
                'success' => true,
                'message' => 'Cash receipt posted successfully.',
                'data' => $receipt->load([
                    'account',
                    'depositAccount',
                ]),
            ]);
        });
    }

    public function show(CashReceipt $cashReceipt): JsonResponse
    {
        $this->authorizeRecordAccess($cashReceipt);

        return response()->json([
            'success' => true,
            'data' => $cashReceipt->load([
                'account',
                'depositAccount',
            ]),
        ]);
    }

    public function update(
        Request $request,
        CashReceipt $cashReceipt
    ): JsonResponse {
        $this->authorizeRecordAccess($cashReceipt);

        if ($cashReceipt->status !== 'Draft') {
            throw ValidationException::withMessages([
                'status' => 'Only Draft receipts can be edited.',
            ]);
        }

        $validated = $request->validate([
            'receipt_date' => ['required', 'date'],
            'payor' => ['required', 'string', 'max:150'],
            'account_id' => ['required', 'integer', 'exists:accounts,id'],
            'deposit_account_id' => [
                'required',
                'integer',
                'exists:accounts,id',
            ],
            'amount' => ['required', 'numeric', 'decimal:0,2', 'min:0.01'],
            'bank_deposited' => ['nullable', 'string', 'max:150'],
            'remarks' => ['nullable', 'string'],
        ]);

        DB::transaction(function () use ($validated, $cashReceipt) {
            $cashReceipt = CashReceipt::whereKey($cashReceipt->id)
                ->lockForUpdate()
                ->firstOrFail();

            if ($cashReceipt->status !== 'Draft') {
                throw ValidationException::withMessages([
                    'status' => 'Only Draft receipts can be edited.',
                ]);
            }

            $depositAccount = Account::findOrFail(
                $validated['deposit_account_id']
            );

            $creditAccount = Account::findOrFail(
                $validated['account_id']
            );

            if (! $depositAccount->is_active ||
                ! $creditAccount->is_active ||
                $depositAccount->account_type !== 'Asset' ||
                $depositAccount->id === $creditAccount->id) {
                throw ValidationException::withMessages([
                    'account_id' => 'The selected accounts are invalid.',
                ]);
            }

            if (! in_array(
                $creditAccount->account_type,
                ['Revenue', 'Liability', 'Equity'],
                true
            )) {
                throw ValidationException::withMessages([
                    'account_id' =>
                        'Cash receipts must use a Revenue, Liability, or Equity account.',
                ]);
            }

            $entries = JournalEntry::where(
                'reference_type',
                'CashReceipt'
            )
                ->where('reference_id', $cashReceipt->id)
                ->lockForUpdate()
                ->get();

            if ($entries->count() !== 1 ||
                $entries->first()->status !== 'Draft') {
                throw ValidationException::withMessages([
                    'journal_entry' =>
                        'This receipt requires exactly one Draft journal entry.',
                ]);
            }

            $entry = $entries->first();

            $cashReceipt->update($validated);

            $entry->update([
                'journal_date' => $validated['receipt_date'],
                'description' => 'Cash receipt OR '
                    . $cashReceipt->or_number
                    . ' - '
                    . $validated['payor'],
            ]);

            $amount = bcadd((string) $validated['amount'], '0', 2);

            $lines = $entry->lines()->orderBy('id')->get();

            if ($lines->count() !== 2) {
                throw ValidationException::withMessages([
                    'journal_entry' =>
                        'Expected two journal lines for this receipt.',
                ]);
            }

            $lines[0]->update([
                'account_id' => $depositAccount->id,
                'description' => 'Cash received - '
                    . $validated['payor'],
                'debit_amount' => $amount,
                'credit_amount' => '0.00',
            ]);

            $lines[1]->update([
                'account_id' => $creditAccount->id,
                'description' => 'Receipt income/account - '
                    . $validated['payor'],
                'debit_amount' => '0.00',
                'credit_amount' => $amount,
            ]);
        });

        return response()->json([
            'success' => true,
            'message' => 'Draft receipt updated successfully.',
            'data' => $cashReceipt->fresh()->load([
                'account',
                'depositAccount',
            ]),
        ]);
    }

    public function destroy(CashReceipt $cashReceipt): JsonResponse
    {
        $this->authorizeRecordAccess($cashReceipt);

        if ($cashReceipt->status !== 'Draft') {
            throw ValidationException::withMessages([
                'status' => 'Only Draft receipts can be deleted.',
            ]);
        }

        DB::transaction(function () use ($cashReceipt) {
            $cashReceipt = CashReceipt::whereKey($cashReceipt->id)
                ->lockForUpdate()
                ->firstOrFail();

            if ($cashReceipt->status !== 'Draft') {
                throw ValidationException::withMessages([
                    'status' => 'Only Draft receipts can be deleted.',
                ]);
            }

            $entries = JournalEntry::where(
                'reference_type',
                'CashReceipt'
            )
                ->where('reference_id', $cashReceipt->id)
                ->lockForUpdate()
                ->get();

            if ($entries->count() !== 1 ||
                $entries->first()->status !== 'Draft') {
                throw ValidationException::withMessages([
                    'journal_entry' =>
                        'This receipt requires exactly one Draft journal entry.',
                ]);
            }

            $entry = $entries->first();
            $entry->lines()->delete();
            $entry->delete();

            $cashReceipt->delete();
        });

        return response()->json([
            'success' => true,
            'message' => 'Draft receipt deleted successfully.',
        ]);
    }

    /** Prevent Staff from viewing or changing another user's transaction. */
    private function authorizeRecordAccess(CashReceipt $receipt): void
    {
        $user = request()->user();

        abort_unless(
            $user !== null &&
            (! $user->hasRole('Staff') || (int) $receipt->created_by === (int) $user->id),
            403,
            'You may only access your own cash receipts.'
        );
    }

}