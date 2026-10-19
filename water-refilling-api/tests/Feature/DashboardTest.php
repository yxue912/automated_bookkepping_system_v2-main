<?php

namespace Tests\Feature;

use App\Models\Account;
use App\Models\JournalEntry;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Facades\Hash;
use Spatie\Permission\Models\Permission;
use Tests\TestCase;

class DashboardTest extends TestCase
{
    use RefreshDatabase;

    public function test_dashboard_uses_posted_entries_and_selected_period_boundaries(): void
    {
        $user = User::create([
            'name' => 'Dashboard Reader',
            'email' => 'dashboard-reader@example.test',
            'password' => Hash::make('correct-password'),
        ]);
        Permission::create([
            'name' => 'dashboard.view',
            'guard_name' => 'web',
        ]);
        $user->givePermissionTo('dashboard.view');
        $token = $user->createToken('dashboard-test')->plainTextToken;

        $cash = $this->account('1000', 'Cash on Hand', 'Asset');
        $receivables = $this->account('1100', 'Accounts Receivable', 'Asset');
        $liabilities = $this->account('2000', 'Accounts Payable', 'Liability');
        $revenue = $this->account('4000', 'Program Revenue', 'Revenue');
        $expense = $this->account('5000', 'Program Expense', 'Expense');
        $equity = $this->account('3000', 'Net Assets', 'Equity');

        $this->entry('OPENING', '2025-12-31', 'Posted', [
            [$cash->id, 1000, 0],
            [$equity->id, 0, 1000],
        ]);

        $this->entry('PERIOD', '2026-05-15', 'Posted', [
            [$cash->id, 600, 0],
            [$revenue->id, 0, 600],
            [$expense->id, 200, 0],
            [$cash->id, 0, 200],
            [$receivables->id, 300, 0],
            [$revenue->id, 0, 300],
            [$liabilities->id, 0, 100],
            [$cash->id, 100, 0],
        ]);

        $this->entry('AFTER-END', '2026-07-01', 'Posted', [
            [$cash->id, 900, 0],
            [$revenue->id, 0, 900],
        ]);
        $this->entry('DRAFT', '2026-06-01', 'Draft', [
            [$cash->id, 5000, 0],
            [$revenue->id, 0, 5000],
        ]);

        $response = $this->withToken($token)
            ->getJson('/api/dashboard?start_date=2026-01-01&end_date=2026-06-30')
            ->assertOk();

        $response->assertJsonPath('filters.start_date', '2026-01-01')
            ->assertJsonPath('filters.end_date', '2026-06-30')
            ->assertJsonPath('data.cash_balance', 1500)
            ->assertJsonPath('data.receivables', 300)
            ->assertJsonPath('data.liabilities', 100)
            ->assertJsonPath('data.payables', 100)
            ->assertJsonPath('data.income', 900)
            ->assertJsonPath('data.expenses', 200)
            ->assertJsonPath('data.assets', 1800)
            ->assertJsonPath('data.financial_summary.total_liabilities', 100);
    }

    private function account(
        string $code,
        string $name,
        string $type
    ): Account {
        return Account::create([
            'account_code' => $code,
            'account_name' => $name,
            'account_type' => $type,
            'is_active' => true,
        ]);
    }

    private function entry(
        string $number,
        string $date,
        string $status,
        array $lines
    ): void {
        $entry = JournalEntry::create([
            'journal_number' => $number,
            'journal_date' => $date,
            'description' => 'Dashboard test ' . $number,
            'status' => $status,
        ]);

        $entry->lines()->createMany(array_map(
            fn (array $line): array => [
                'account_id' => $line[0],
                'debit_amount' => $line[1],
                'credit_amount' => $line[2],
            ],
            $lines
        ));
    }
}
