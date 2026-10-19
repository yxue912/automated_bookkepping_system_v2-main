<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Account;
use Illuminate\Http\JsonResponse;
use Illuminate\Support\Facades\DB;

class TrialBalanceController extends Controller
{
    public function index(): JsonResponse
    {
        $validated = request()->validate(['as_of' => ['nullable', 'date_format:Y-m-d']]);
        $accounts = Account::query()
            ->where(function ($query) {
                $query->where('is_active', true)
                    ->orWhereHas('journalEntryLines', fn ($lines) => $lines->whereHas('journalEntry', fn ($entries) => $entries->where('status', 'Posted')));
            })
            ->orderBy('account_code')
            ->get();

        $balances = DB::table('journal_entry_lines as lines')
            ->join(
                'journal_entries as entries',
                'entries.id',
                '=',
                'lines.journal_entry_id'
            )
            ->where('entries.status', 'Posted')
            ->when(isset($validated['as_of']), fn ($query) => $query->whereDate('entries.journal_date', '<=', $validated['as_of']))
            ->select(
                'lines.account_id',
                DB::raw('SUM(lines.debit_amount) as total_debits'),
                DB::raw('SUM(lines.credit_amount) as total_credits')
            )
            ->groupBy('lines.account_id')
            ->get()
            ->keyBy('account_id');

        $totalDebits = '0.00';
        $totalCredits = '0.00';
        $rows = [];

        foreach ($accounts as $account) {
            $balance = $balances->get($account->id);

            $debits = (string) ($balance->total_debits ?? '0.00');
            $credits = (string) ($balance->total_credits ?? '0.00');

            $netBalance = bcsub($debits, $credits, 2);

            if (bccomp($netBalance, '0', 2) >= 0) {
                $debitBalance = $netBalance;
                $creditBalance = '0.00';
            } else {
                $debitBalance = '0.00';
                $creditBalance = bcsub('0', $netBalance, 2);
            }

            $totalDebits = bcadd($totalDebits, $debitBalance, 2);
            $totalCredits = bcadd($totalCredits, $creditBalance, 2);

            $rows[] = [
                'account_id' => $account->id,
                'account_code' => $account->account_code,
                'account_name' => $account->account_name,
                'account_type' => $account->account_type,
                'debit_balance' => $debitBalance,
                'credit_balance' => $creditBalance,
            ];
        }

        return response()->json([
            'success' => true,
            'data' => $rows,
            'totals' => [
                'debits' => $totalDebits,
                'credits' => $totalCredits,
                'is_balanced' => bccomp($totalDebits, $totalCredits, 2) === 0,
            ],
        ]);
    }
}
