<?php

namespace Tests\Feature;

use App\Models\Account;
use App\Models\JournalEntry;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Facades\Hash;
use Spatie\Permission\Models\Permission;
use Tests\TestCase;

class AccountingReportsIntegrityTest extends TestCase
{
    use RefreshDatabase;

    private function token(): string
    {
        $user = User::create([
            'name' => 'Report Reader',
            'email' => 'reports@example.test',
            'password' => Hash::make('password'),
        ]);
        foreach (['ledger.view', 'trial_balance.view'] as $name) {
            Permission::firstOrCreate(['name' => $name, 'guard_name' => 'web']);
        }
        $user->givePermissionTo(['ledger.view', 'trial_balance.view']);
        return $user->createToken('test')->plainTextToken;
    }

    private function entry(string $number, string $date, string $status, Account $cash, Account $equity, string $amount): void
    {
        $entry = JournalEntry::create([
            'journal_number' => $number,
            'journal_date' => $date,
            'description' => $number,
            'status' => $status,
        ]);
        $entry->lines()->createMany([
            ['account_id' => $cash->id, 'debit_amount' => $amount, 'credit_amount' => '0.00'],
            ['account_id' => $equity->id, 'debit_amount' => '0.00', 'credit_amount' => $amount],
        ]);
    }

    public function test_ledger_opening_chronology_closing_and_inactive_history_reconcile_with_trial_balance(): void
    {
        $cash = Account::create(['account_code' => '1000', 'account_name' => 'Cash', 'account_type' => 'Asset', 'is_active' => false]);
        $equity = Account::create(['account_code' => '3000', 'account_name' => 'Equity', 'account_type' => 'Equity', 'is_active' => true]);
        $this->entry('JULY', '2026-07-01', 'Posted', $cash, $equity, '90.00');
        $this->entry('MAY', '2026-05-01', 'Posted', $cash, $equity, '20.00');
        $this->entry('JAN', '2026-01-01', 'Posted', $cash, $equity, '100.00');
        $this->entry('MAR', '2026-03-01', 'Posted', $cash, $equity, '10.00');
        $this->entry('DRAFT', '2026-04-01', 'Draft', $cash, $equity, '1000.00');
        $token = $this->token();

        $ledger = $this->withToken($token)->getJson('/api/general-ledger?account_id='.$cash->id.'&start_date=2026-02-01&end_date=2026-06-30')
            ->assertOk()->json('data.0');
        $this->assertSame('100.00', $ledger['opening_balance']);
        $this->assertSame('30.00', $ledger['total_debits']);
        $this->assertSame('130.00', $ledger['closing_balance']);
        $this->assertSame(['MAR', 'MAY'], array_column($ledger['entries'], 'journal_number'));
        $this->assertSame(['110.00', '130.00'], array_column($ledger['entries'], 'running_balance'));

        $equityLedger = $this->withToken($token)->getJson('/api/general-ledger?account_id='.$equity->id.'&start_date=2026-02-01&end_date=2026-06-30')
            ->assertOk()->json('data.0');
        $this->assertSame('100.00', $equityLedger['opening_balance']);
        $this->assertSame('130.00', $equityLedger['closing_balance']);

        $trial = $this->withToken($token)->getJson('/api/trial-balance?as_of=2026-06-30')->assertOk();
        $this->assertSame('130.00', collect($trial->json('data'))->firstWhere('account_id', $cash->id)['debit_balance']);
        $trial->assertJsonPath('totals.is_balanced', true);
        $this->withToken($token)->getJson('/api/trial-balance?as_of=2026-07-01')
            ->assertJsonPath('totals.debits', '220.00');
    }
}
