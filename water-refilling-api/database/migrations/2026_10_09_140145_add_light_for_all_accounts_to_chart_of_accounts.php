<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Support\Facades\DB;

return new class extends Migration
{
    public function up(): void
    {
        $now = now();

        $accounts = [
            [
                'account_code' => '101001',
                'account_name' => 'Cash in Bank (Old Account)',
                'account_type' => 'Asset',
                'account_group' => 'Cash and Cash Equivalents',
                'description' => 'Client bank account - Old Account',
            ],
            [
                'account_code' => '101002',
                'account_name' => 'Cash in Bank (LFA)',
                'account_type' => 'Asset',
                'account_group' => 'Cash and Cash Equivalents',
                'description' => 'Client bank account - LFA',
            ],
            [
                'account_code' => '101003',
                'account_name' => 'Cash in Bank (Thanksgiving)',
                'account_type' => 'Asset',
                'account_group' => 'Cash and Cash Equivalents',
                'description' => 'Client bank account - Thanksgiving',
            ],
            [
                'account_code' => '101004',
                'account_name' => 'Cash in Bank (Other)',
                'account_type' => 'Asset',
                'account_group' => 'Cash and Cash Equivalents',
                'description' => 'Client bank account - Other',
            ],
            [
                'account_code' => '401001',
                'account_name' => 'Donations/Revenue',
                'account_type' => 'Revenue',
                'account_group' => 'Donations and Revenue',
                'description' => 'Donations and other revenue received',
            ],
            [
                'account_code' => '402001',
                'account_name' => 'Interest Income',
                'account_type' => 'Revenue',
                'account_group' => 'Other Revenue',
                'description' => 'Interest earned from bank accounts',
            ],
        ];

        foreach ($accounts as $account) {
            DB::table('accounts')->insert([
                ...$account,
                'is_active' => true,
                'created_at' => $now,
                'updated_at' => $now,
            ]);
        }
    }

    public function down(): void
    {
        DB::table('accounts')
            ->whereIn('account_code', [
                '101001',
                '101002',
                '101003',
                '101004',
                '401001',
                '402001',
            ])
            ->delete();
    }
};