<?php

namespace App\Services;

use App\Models\JournalEntry;
use App\Models\Account;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\ValidationException;

class JournalEntryService
{
    public function validateForPosting(JournalEntry $entry): void
    {
        if (! $entry->relationLoaded('lines')) {
            $entry->load('lines.account');
        }

        if ($entry->lines->count() < 2) {
            throw ValidationException::withMessages([
                'lines' => 'A journal entry must have at least two lines.',
            ]);
        }

        $totalDebits = '0.00';
        $totalCredits = '0.00';
        foreach ($entry->lines as $line) {
            $debit = (string) $line->debit_amount;
            $credit = (string) $line->credit_amount;

            if (bccomp($debit, '0', 2) < 0 || bccomp($credit, '0', 2) < 0) {
                throw ValidationException::withMessages([
                    'lines' => 'Debit and credit amounts cannot be negative.',
                ]);
            }

            if ((bccomp($debit, '0', 2) > 0 && bccomp($credit, '0', 2) > 0) ||
                (bccomp($debit, '0', 2) === 0 && bccomp($credit, '0', 2) === 0)) {
                throw ValidationException::withMessages([
                    'lines' => 'Each line must contain either a debit or a credit, not both or neither.',
                ]);
            }
            if (! $line->account?->is_active) {
                throw ValidationException::withMessages(['lines' => 'Posting requires active accounts.']);
            }
            $totalDebits = bcadd($totalDebits, $debit, 2);
            $totalCredits = bcadd($totalCredits, $credit, 2);
        }

        if (bccomp($totalDebits, $totalCredits, 2) !== 0) {
            throw ValidationException::withMessages([
                'lines' => 'Total debits must equal total credits before posting.',
            ]);
        }
    }

    public function post(JournalEntry $entry): void
    {
        DB::transaction(function () use ($entry) {
            $lockedEntry = JournalEntry::query()
                ->lockForUpdate()
                ->findOrFail($entry->id);

            if ($lockedEntry->status !== 'Draft') {
                throw ValidationException::withMessages([
                    'status' => 'Only draft journal entries can be posted.',
                ]);
            }

            $accountIds = $lockedEntry->lines()->pluck('account_id')->unique()->sort()->values();
            Account::whereIn('id', $accountIds)->orderBy('id')->lockForUpdate()->get();

            $this->validateForPosting($lockedEntry);

            $lockedEntry->status = 'Posted';
            $lockedEntry->posted_at = now();
            $lockedEntry->save();
        });
    }
}
