<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Account;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;

class GeneralLedgerController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'start_date' => ['nullable', 'date_format:Y-m-d'],
            'end_date' => ['nullable', 'date_format:Y-m-d', 'after_or_equal:start_date'],
            'account_id' => ['nullable', 'integer', 'exists:accounts,id'],
        ]);

        $opening = collect();
        if (isset($validated['start_date'])) {
            $opening = DB::table('journal_entry_lines as lines')
                ->join('journal_entries as entries', 'entries.id', '=', 'lines.journal_entry_id')
                ->where('entries.status', 'Posted')
                ->whereDate('entries.journal_date', '<', $validated['start_date'])
                ->select('lines.account_id', DB::raw('SUM(lines.debit_amount) as debits'), DB::raw('SUM(lines.credit_amount) as credits'))
                ->groupBy('lines.account_id')->get()->keyBy('account_id');
        }

        $accounts = Account::query()
            ->when(isset($validated['account_id']), fn ($query) => $query->whereKey($validated['account_id']))
            ->where(function ($query) {
                $query->where('is_active', true)
                    ->orWhereHas('journalEntryLines', fn ($lines) => $lines->whereHas('journalEntry', fn ($entries) => $entries->where('status', 'Posted')));
            })
            ->with(['journalEntryLines' => function ($query) use ($validated) {
                $query->whereHas('journalEntry', function ($entries) use ($validated) {
                    $entries->where('status', 'Posted')
                        ->when(isset($validated['start_date']), fn ($q) => $q->whereDate('journal_date', '>=', $validated['start_date']))
                        ->when(isset($validated['end_date']), fn ($q) => $q->whereDate('journal_date', '<=', $validated['end_date']));
                })->with('journalEntry');
            }])->orderBy('account_code')->get();

        $rows = $accounts->map(function ($account) use ($opening) {
            $normalDebitBalance = in_array($account->account_type, ['Asset', 'Expense'], true);
            $prior = $opening->get($account->id);
            $openingBalance = $normalDebitBalance
                ? bcsub((string) ($prior->debits ?? '0'), (string) ($prior->credits ?? '0'), 2)
                : bcsub((string) ($prior->credits ?? '0'), (string) ($prior->debits ?? '0'), 2);
            $runningBalance = $openingBalance;
            $totalDebits = '0.00';
            $totalCredits = '0.00';
            $lines = $account->journalEntryLines->sort(function ($a, $b) {
                return [$a->journalEntry->journal_date->toDateString(), $a->journal_entry_id, $a->id]
                    <=> [$b->journalEntry->journal_date->toDateString(), $b->journal_entry_id, $b->id];
            })->map(function ($line) use (&$runningBalance, &$totalDebits, &$totalCredits, $normalDebitBalance) {
                $debit = (string) $line->debit_amount;
                $credit = (string) $line->credit_amount;
                $totalDebits = bcadd($totalDebits, $debit, 2);
                $totalCredits = bcadd($totalCredits, $credit, 2);
                $movement = $normalDebitBalance ? bcsub($debit, $credit, 2) : bcsub($credit, $debit, 2);
                $runningBalance = bcadd($runningBalance, $movement, 2);
                return [
                    'journal_entry_id' => $line->journal_entry_id,
                    'journal_number' => $line->journalEntry->journal_number,
                    'journal_date' => $line->journalEntry->journal_date,
                    'entry_description' => $line->journalEntry->description,
                    'line_description' => $line->description,
                    'debit' => $debit,
                    'credit' => $credit,
                    'running_balance' => $runningBalance,
                ];
            });
            return [
                'account_id' => $account->id,
                'account_code' => $account->account_code,
                'account_name' => $account->account_name,
                'account_type' => $account->account_type,
                'opening_balance' => $openingBalance,
                'total_debits' => $totalDebits,
                'total_credits' => $totalCredits,
                'closing_balance' => $runningBalance,
                'entries' => $lines->values(),
            ];
        });

        return response()->json(['data' => $rows->values()]);
    }
}
