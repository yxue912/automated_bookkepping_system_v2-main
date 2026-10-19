<?php

namespace Tests\Feature;

use App\Models\Account;
use App\Models\JournalEntry;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Facades\Hash;
use Spatie\Permission\Models\Permission;
use Tests\TestCase;

class CashReceiptIntegrityTest extends TestCase
{
    use RefreshDatabase;

    public function test_draft_receipt_edit_and_delete_reject_duplicate_posted_journal(): void
    {
        $user = User::create(['name' => 'Clerk', 'email' => 'clerk@example.test', 'password' => Hash::make('password')]);
        foreach (['cash_receipts.create', 'cash_receipts.edit', 'cash_receipts.delete'] as $name) {
            Permission::firstOrCreate(['name' => $name, 'guard_name' => 'web']);
        }
        $user->givePermissionTo(['cash_receipts.create', 'cash_receipts.edit', 'cash_receipts.delete']);
        $cash = Account::create(['account_code' => '1000', 'account_name' => 'Cash', 'account_type' => 'Asset', 'is_active' => true]);
        $revenue = Account::create(['account_code' => '4000', 'account_name' => 'Revenue', 'account_type' => 'Revenue', 'is_active' => true]);
        $payload = [
            'receipt_date' => '2026-10-10', 'or_number' => 'OR-1', 'payor' => 'Customer',
            'account_id' => $revenue->id, 'deposit_account_id' => $cash->id, 'amount' => '10.00',
        ];
        $token = $user->createToken('test')->plainTextToken;
        $id = $this->withToken($token)->postJson('/api/cash-receipts', $payload)->assertCreated()->json('data.id');
        $entry = JournalEntry::where('reference_type', 'CashReceipt')->where('reference_id', $id)->firstOrFail();
        $this->withToken($token)->putJson("/api/cash-receipts/{$id}", array_merge($payload, ['amount' => '20.00']))
            ->assertOk()->assertJsonPath('data.amount', '20.00');
        $this->assertSame('20.00', $entry->fresh()->lines()->orderBy('id')->first()->debit_amount);

        $duplicate = JournalEntry::create([
            'journal_number' => 'CR-duplicate', 'journal_date' => '2026-10-10',
            'description' => 'Legacy duplicate', 'reference_type' => 'CashReceipt',
            'reference_id' => $id, 'status' => 'Posted', 'created_by' => $user->id,
        ]);
        $this->withToken($token)->putJson("/api/cash-receipts/{$id}", array_merge($payload, ['amount' => '30.00']))
            ->assertUnprocessable();
        $this->withToken($token)->deleteJson("/api/cash-receipts/{$id}")->assertUnprocessable();
        $this->assertDatabaseHas('cash_receipts', ['id' => $id, 'amount' => '20.00']);
        $this->assertDatabaseHas('journal_entries', ['id' => $duplicate->id, 'status' => 'Posted']);
        $this->assertDatabaseHas('journal_entries', ['id' => $entry->id, 'status' => 'Draft']);
    }
}
