<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;

class DashboardController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'start_date' => ['nullable', 'date_format:Y-m-d'],
            'end_date' => [
                'nullable',
                'date_format:Y-m-d',
                'after_or_equal:start_date',
            ],
        ]);

        $startDate = $validated['start_date'] ?? null;
        $endDate = $validated['end_date'] ?? null;

        // Balance-sheet figures include all posted entries
        // up to the selected end date.
        $balancesQuery = DB::table('journal_entry_lines')
            ->join(
                'journal_entries',
                'journal_entry_lines.journal_entry_id',
                '=',
                'journal_entries.id'
            )
            ->join(
                'accounts',
                'journal_entry_lines.account_id',
                '=',
                'accounts.id'
            )
            ->where('journal_entries.status', 'Posted');

        if ($endDate) {
            $balancesQuery->whereDate(
                'journal_entries.journal_date',
                '<=',
                $endDate
            );
        }

        $balances = $balancesQuery
            ->select(
                'accounts.id',
                'accounts.account_code',
                'accounts.account_name',
                'accounts.account_type',
                DB::raw('SUM(journal_entry_lines.debit_amount) as total_debits'),
                DB::raw('SUM(journal_entry_lines.credit_amount) as total_credits')
            )
            ->groupBy(
                'accounts.id',
                'accounts.account_code',
                'accounts.account_name',
                'accounts.account_type'
            )
            ->get();

        $cashBalance = '0.00';
        $receivables = '0.00';
        $payables = '0.00';
        $assets = '0.00';
        $liabilities = '0.00';
        $equity = '0.00';

        foreach ($balances as $account) {
            $netDebit = bcsub((string) $account->total_debits, (string) $account->total_credits, 2);

            $type = strtolower($account->account_type);

            if ($type === 'asset') {
                $assets = bcadd($assets, $netDebit, 2);

                if (in_array($account->account_code, ['1000', '1010'], true)) {
                    $cashBalance = bcadd($cashBalance, $netDebit, 2);
                }

                if ($account->account_code === '1100') {
                    $receivables = bcadd($receivables, $netDebit, 2);
                }
            }

            if ($type === 'liability') {
                $liabilities = bcsub($liabilities, $netDebit, 2);
                if ($account->account_code === '2000') {
                    $payables = bcsub($payables, $netDebit, 2);
                }
            }

            if ($type === 'equity') {
                $equity = bcsub($equity, $netDebit, 2);
            }
        }

        // Income and expenses cover only the selected period.
        $periodQuery = DB::table('journal_entry_lines')
            ->join(
                'journal_entries',
                'journal_entry_lines.journal_entry_id',
                '=',
                'journal_entries.id'
            )
            ->join(
                'accounts',
                'journal_entry_lines.account_id',
                '=',
                'accounts.id'
            )
            ->where('journal_entries.status', 'Posted');

        if ($startDate) {
            $periodQuery->whereDate(
                'journal_entries.journal_date',
                '>=',
                $startDate
            );
        }

        if ($endDate) {
            $periodQuery->whereDate(
                'journal_entries.journal_date',
                '<=',
                $endDate
            );
        }

        $periodTotals = $periodQuery
            ->select(
                'accounts.account_type',
                DB::raw('SUM(journal_entry_lines.debit_amount) as total_debits'),
                DB::raw('SUM(journal_entry_lines.credit_amount) as total_credits')
            )
            ->groupBy('accounts.account_type')
            ->get();

        $income = '0.00';
        $expenses = '0.00';

        foreach ($periodTotals as $account) {
            $type = strtolower($account->account_type);
            $debits = (string) $account->total_debits;
            $credits = (string) $account->total_credits;

            if ($type === 'revenue') {
                $income = bcadd($income, bcsub($credits, $debits, 2), 2);
            }

            if ($type === 'expense') {
                $expenses = bcadd($expenses, bcsub($debits, $credits, 2), 2);
            }
        }
           // Fetch the 10 most recently created journal entries.
        $recentActivity = DB::table('journal_entries as j')
            ->leftJoin(
                'journal_entry_lines as l',
                'j.id',
                '=',
                'l.journal_entry_id'
            )
            ->select(
                'j.journal_number as reference',
                'j.journal_date as date',
                'j.description',
                'j.reference_type',
                'j.status',
                'j.created_at',
                DB::raw(
                    "COALESCE(SUM(l.debit_amount), 0) as amount"
                )
            )
            ->groupBy(
                'j.id',
                'j.journal_number',
                'j.journal_date',
                'j.description',
                'j.reference_type',
                'j.status',
                'j.created_at'
            )
            ->orderByDesc('j.created_at')
            ->orderByDesc('j.id')
            ->limit(10)
            ->get()
            ->map(function ($entry) {
                return [
                    'reference' => $entry->reference,
                    'date' => $entry->date,
                    'type' => $entry->reference_type ?: 'Journal Entry',
                    'description' => $entry->description,
                    'amount' => (float) $entry->amount,
                    'status' => $entry->status,
                ];
            });

        return response()->json([
            'success' => true,
            'filters' => [
                'start_date' => $startDate,
                'end_date' => $endDate,
            ],
            'data' => [
                'cash_balance' => (float) $cashBalance,
                'income' => (float) $income,
                'recent_activity' => $recentActivity,
                'expenses' => (float) $expenses,
                'receivables' => (float) $receivables,
                'payables' => (float) $payables,
                'liabilities' => (float) $liabilities,
                'assets' => (float) $assets,
                'financial_summary' => [
                    'total_assets' => (float) $assets,
                    'total_liabilities' => (float) $liabilities,
                    'total_equity' => (float) $equity,
                    'total_income' => (float) $income,
                    'total_expenses' => (float) $expenses,
                    'net_income' => (float) bcsub($income, $expenses, 2),
                ],
            ],
        ]);
    }
}
