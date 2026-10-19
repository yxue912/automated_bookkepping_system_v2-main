<?php

namespace Tests\Feature;

use App\Models\Account;
use App\Models\JournalEntry;
use App\Models\User;
use Database\Seeders\AccountSeeder;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Facades\Hash;
use Spatie\Permission\Models\Permission;
use Tests\TestCase;

class ValidationIntegrityTest extends TestCase
{
    use RefreshDatabase;

    private function token(array $permissions): string
    {
        $user = User::create([
            'name' => fake()->name(),
            'email' => fake()->unique()->safeEmail(),
            'password' => Hash::make('password'),
        ]);
        foreach ($permissions as $permission) {
            Permission::firstOrCreate(['name' => $permission, 'guard_name' => 'web']);
        }
        $user->givePermissionTo($permissions);
        return $user->createToken('test')->plainTextToken;
    }

    public function test_fixed_asset_rejects_salvage_above_cost(): void
    {
        $this->withToken($this->token(['fixed_assets.create']))
            ->postJson('/api/fixed-assets', [
                'asset_code' => 'FA-100', 'name' => 'Pump',
                'acquisition_date' => '2026-10-10',
                'acquisition_cost' => '100.00', 'salvage_value' => '101.00',
                'useful_life_years' => 5,
            ])->assertUnprocessable()->assertJsonValidationErrors('salvage_value');
        $this->assertDatabaseMissing('fixed_assets', ['asset_code' => 'FA-100']);
    }

    public function test_invoice_calculates_line_amount_and_rejects_forged_approval(): void
    {
        $payload = [
            'invoice_number' => 'INV-100', 'invoice_date' => '2026-10-10',
            'customer_name' => 'Customer',
            'items' => [[
                'nature_of_service' => 'Water', 'quantity' => '1.25',
                'unit_cost_price' => '1.25', 'amount' => '1.56',
            ]],
        ];
        $token = $this->token(['invoices.create']);
        $this->withToken($token)->postJson('/api/invoices', array_merge($payload, ['approved_by' => 'Forged']))
            ->assertUnprocessable();
        $this->withToken($token)->postJson('/api/invoices', array_replace_recursive($payload, ['items' => [['amount' => '1.57']]]))
            ->assertUnprocessable()->assertJsonValidationErrors('items.0.amount');
        $this->withToken($token)->postJson('/api/invoices', $payload)->assertCreated();
    }

    public function test_account_type_with_posted_history_cannot_change_but_metadata_can(): void
    {
        $account = Account::create(['account_code' => '1000', 'account_name' => 'Cash', 'account_type' => 'Asset', 'is_active' => true]);
        $entry = JournalEntry::create(['journal_number' => 'J-100', 'journal_date' => '2026-10-10', 'description' => 'History', 'status' => 'Posted']);
        $entry->lines()->create(['account_id' => $account->id, 'debit_amount' => '1.00', 'credit_amount' => '0.00']);
        $token = $this->token(['accounts.edit']);
        $this->withToken($token)->patchJson("/api/accounts/{$account->id}", ['account_type' => 'Expense'])
            ->assertUnprocessable()->assertJsonValidationErrors('account_type');
        $this->withToken($token)->patchJson("/api/accounts/{$account->id}", ['description' => 'Updated'])
            ->assertOk()->assertJsonPath('data.description', 'Updated');
    }

    public function test_account_seeding_is_idempotent_and_preserves_custom_names(): void
    {
        $this->seed(AccountSeeder::class);
        $count = Account::count();
        Account::where('account_code', '1000')->update(['account_name' => 'Customized Cash']);
        $this->seed(AccountSeeder::class);
        $this->assertSame($count, Account::count());
        $this->assertDatabaseHas('accounts', ['account_code' => '1000', 'account_name' => 'Customized Cash']);
    }
}
