<?php

namespace Tests\Feature;

use App\Models\Account;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Facades\Auth;
use Spatie\Permission\Models\Permission;
use Tests\TestCase;

class JournalEntryIntegrityTest extends TestCase
{
    use RefreshDatabase;

    private function token(array $permissions): string
    {
        $user = User::create(['name' => 'Poster', 'email' => fake()->unique()->safeEmail(), 'password' => Hash::make('password')]);
        foreach ($permissions as $permission) {
            Permission::firstOrCreate(['name' => $permission, 'guard_name' => 'web']);
        }
        $user->givePermissionTo($permissions);
        return $user->createToken('test')->plainTextToken;
    }

    private function accounts(): array
    {
        return [
            Account::create(['account_code' => '1000', 'account_name' => 'Cash', 'account_type' => 'Asset', 'is_active' => true]),
            Account::create(['account_code' => '3000', 'account_name' => 'Capital', 'account_type' => 'Equity', 'is_active' => true]),
        ];
    }

    private function payload(Account $debit, Account $credit): array
    {
        return [
            'journal_number' => 'JV-1', 'journal_date' => '2026-10-10',
            'description' => 'Test voucher',
            'lines' => [
                ['account_id' => $debit->id, 'debit_amount' => '0.30', 'credit_amount' => '0.00'],
                ['account_id' => $credit->id, 'debit_amount' => '0.00', 'credit_amount' => '0.30'],
            ],
        ];
    }

    public function test_invalid_line_shapes_and_source_reference_are_rejected(): void
    {
        [$debit, $credit] = $this->accounts();
        $payload = $this->payload($debit, $credit);
        $token = $this->token(['journal_vouchers.create']);
        $this->withToken($token)->postJson('/api/journal-entries', $payload + ['reference_type' => 'CashReceipt', 'reference_id' => 1])
            ->assertUnprocessable();
        $this->withToken($token)->postJson('/api/journal-entries', $payload + ['reference_type' => 'App\\Models\\Cash_Receipt', 'reference_id' => 1])
            ->assertUnprocessable();
        $bad = $payload;
        $bad['lines'][0]['credit_amount'] = '0.01';
        $this->withToken($token)->postJson('/api/journal-entries', $bad)->assertUnprocessable();
        $bad = $payload;
        $bad['lines'][0]['debit_amount'] = '0.00';
        $this->withToken($token)->postJson('/api/journal-entries', $bad)->assertUnprocessable();
        $bad = $payload;
        $bad['lines'][0]['debit_amount'] = '-0.01';
        $this->withToken($token)->postJson('/api/journal-entries', $bad)->assertUnprocessable();
        $bad = $payload;
        $bad['lines'][1]['credit_amount'] = '0.29';
        $this->withToken($token)->postJson('/api/journal-entries', $bad)->assertUnprocessable();
        $this->assertDatabaseMissing('journal_entries', ['journal_number' => 'JV-1']);
    }

    public function test_posting_requires_permission_active_accounts_and_draft_status(): void
    {
        [$debit, $credit] = $this->accounts();
        $creator = $this->token(['journal_vouchers.create']);
        $id = $this->withToken($creator)->postJson('/api/journal-entries', $this->payload($debit, $credit))
            ->assertCreated()->json('id');
        $this->withToken($creator)->postJson("/api/journal-entries/{$id}/post")->assertForbidden();
        $poster = $this->token(['journal_vouchers.post']);
        Auth::forgetGuards();
        $credit->update(['is_active' => false]);
        $this->withToken($poster)->postJson("/api/journal-entries/{$id}/post")->assertUnprocessable();
        $credit->update(['is_active' => true]);
        $this->withToken($poster)->postJson("/api/journal-entries/{$id}/post")
            ->assertOk()->assertJsonPath('status', 'Posted');
        $this->withToken($poster)->postJson("/api/journal-entries/{$id}/post")->assertUnprocessable();
    }

    public function test_decimal_parts_balance_exactly_and_extra_precision_is_rejected(): void
    {
        [$debit, $credit] = $this->accounts();
        $token = $this->token(['journal_vouchers.create', 'journal_vouchers.post']);
        $payload = $this->payload($debit, $credit);
        $payload['lines'] = [
            ['account_id' => $debit->id, 'debit_amount' => '0.10', 'credit_amount' => '0.00'],
            ['account_id' => $debit->id, 'debit_amount' => '0.20', 'credit_amount' => '0.00'],
            ['account_id' => $credit->id, 'debit_amount' => '0.00', 'credit_amount' => '0.30'],
        ];
        $bad = $payload;
        $bad['lines'][0]['debit_amount'] = '0.001';
        $this->withToken($token)->postJson('/api/journal-entries', $bad)->assertUnprocessable();
        $id = $this->withToken($token)->postJson('/api/journal-entries', $payload)->assertCreated()->json('id');
        $this->withToken($token)->postJson("/api/journal-entries/{$id}/post")
            ->assertOk()->assertJsonPath('status', 'Posted');
    }
}
