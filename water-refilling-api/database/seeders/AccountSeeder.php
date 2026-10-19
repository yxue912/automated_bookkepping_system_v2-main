<?php

namespace Database\Seeders;

use App\Models\Account;
use Illuminate\Database\Seeder;

class AccountSeeder extends Seeder
{
    public function run(): void
    {
        $accounts = [

            // =========================
            // ASSETS
            // =========================

            [
                'account_code' => '1000',
                'account_name' => 'Cash',
                'account_type' => 'Asset',
                'account_group' => 'Current Assets',
            ],

            [
                'account_code' => '1010',
                'account_name' => 'Cash in Bank',
                'account_type' => 'Asset',
                'account_group' => 'Current Assets',
            ],

            [
                'account_code' => '1100',
                'account_name' => 'Accounts Receivable',
                'account_type' => 'Asset',
                'account_group' => 'Current Assets',
            ],

            [
                'account_code' => '1200',
                'account_name' => 'Inventory',
                'account_type' => 'Asset',
                'account_group' => 'Current Assets',
            ],

            [
                'account_code' => '1500',
                'account_name' => 'Equipment',
                'account_type' => 'Asset',
                'account_group' => 'Fixed Assets',
            ],

            // =========================
            // LIABILITIES
            // =========================

            [
                'account_code' => '2000',
                'account_name' => 'Accounts Payable',
                'account_type' => 'Liability',
                'account_group' => 'Current Liabilities',
            ],

            [
                'account_code' => '2100',
                'account_name' => 'Accrued Expenses',
                'account_type' => 'Liability',
                'account_group' => 'Current Liabilities',
            ],

            // =========================
            // EQUITY
            // =========================

            [
                'account_code' => '3000',
                'account_name' => 'Owner Capital',
                'account_type' => 'Equity',
                'account_group' => 'Owner Equity',
            ],

            [
                'account_code' => '3100',
                'account_name' => 'Owner Drawings',
                'account_type' => 'Equity',
                'account_group' => 'Owner Equity',
            ],

            [
                'account_code' => '3200',
                'account_name' => 'Retained Earnings',
                'account_type' => 'Equity',
                'account_group' => 'Owner Equity',
            ],

            // =========================
            // REVENUE
            // =========================

            [
                'account_code' => '4000',
                'account_name' => 'Water Sales',
                'account_type' => 'Revenue',
                'account_group' => 'Operating Revenue',
            ],

            // =========================
            // EXPENSES
            // =========================

            [
                'account_code' => '5000',
                'account_name' => 'Water Production Expense',
                'account_type' => 'Expense',
                'account_group' => 'Operating Expenses',
            ],

            [
                'account_code' => '5100',
                'account_name' => 'Electricity Expense',
                'account_type' => 'Expense',
                'account_group' => 'Operating Expenses',
            ],

            [
                'account_code' => '5200',
                'account_name' => 'Fuel and Transportation Expense',
                'account_type' => 'Expense',
                'account_group' => 'Operating Expenses',
            ],

            [
                'account_code' => '5300',
                'account_name' => 'Supplies Expense',
                'account_type' => 'Expense',
                'account_group' => 'Operating Expenses',
            ],

            [
                'account_code' => '5400',
                'account_name' => 'Salaries and Wages Expense',
                'account_type' => 'Expense',
                'account_group' => 'Operating Expenses',
            ],

            [
                'account_code' => '5500',
                'account_name' => 'Maintenance and Repairs Expense',
                'account_type' => 'Expense',
                'account_group' => 'Operating Expenses',
            ],

            [
                'account_code' => '5600',
                'account_name' => 'Other Operating Expenses',
                'account_type' => 'Expense',
                'account_group' => 'Operating Expenses',
            ],
        ];

        foreach ($accounts as $account) {
            Account::firstOrCreate(
                ['account_code' => $account['account_code']],
                $account
            );
        }
    }
}
