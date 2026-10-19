<?php

namespace Tests\Feature;

use App\Models\Account;
use App\Models\CashCheckVoucher;
use App\Models\JournalEntry;
use App\Models\Rfp;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Facades\Auth;
use Spatie\Permission\Models\Permission;
use Tests\TestCase;

class FinancialWorkflowIntegrityTest extends TestCase
{
    use RefreshDatabase;

    private function user(array $permissions): User
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
        return $user;
    }

    private function account(string $code, string $type): Account
    {
        return Account::create([
            'account_code' => $code,
            'account_name' => $code,
            'account_type' => $type,
            'is_active' => true,
        ]);
    }

    private function approvedRfp(User $requester, Account $account): Rfp
    {
        return Rfp::create([
            'rfp_number' => 'RFP-100',
            'rfp_date' => '2026-10-10',
            'requested_by' => $requester->name,
            'payee' => 'Supplier',
            'purpose' => 'Supplies',
            'amount' => '100.00',
            'currency' => 'PHP',
            'status' => 'Approved',
            'account_id' => $account->id,
            'user_id' => $requester->id,
        ]);
    }

    private function voucherPayload(Rfp $rfp): array
    {
        return [
            'voucher_number' => 'V-100',
            'rfp_id' => $rfp->id,
            'voucher_date' => '2026-10-10',
            'payment_method' => 'Cash',
            'payee' => 'Supplier',
            'amount' => '100.00',
            'currency' => 'PHP',
        ];
    }

    public function test_voucher_rejects_forgery_duplicate_and_mismatched_financial_values(): void
    {
        $requester = $this->user([]);
        $creator = $this->user(['vouchers.create']);
        $rfp = $this->approvedRfp($requester, $this->account('5000', 'Expense'));
        $token = $creator->createToken('test')->plainTextToken;

        $this->withToken($token)->postJson('/api/cash-check-vouchers', $this->voucherPayload($rfp) + ['status' => 'Paid'])
            ->assertUnprocessable();
        $this->withToken($token)->postJson('/api/cash-check-vouchers', array_merge($this->voucherPayload($rfp), ['currency' => 'USD']))
            ->assertUnprocessable();
        $this->withToken($token)->postJson('/api/cash-check-vouchers', array_merge($this->voucherPayload($rfp), ['amount' => '101.00']))
            ->assertUnprocessable();
        $this->withToken($token)->postJson('/api/cash-check-vouchers', array_merge($this->voucherPayload($rfp), ['payment_method' => 'Check']))
            ->assertUnprocessable();
        $this->withToken($token)->postJson('/api/cash-check-vouchers', $this->voucherPayload($rfp))
            ->assertCreated()->assertJsonPath('data.status', 'Draft');
        $this->withToken($token)->postJson('/api/cash-check-vouchers', array_merge($this->voucherPayload($rfp), ['voucher_number' => 'V-101']))
            ->assertUnprocessable();
        $this->assertSame(1, CashCheckVoucher::count());
    }

    public function test_releasing_voucher_posts_once_and_paid_acknowledgment_requires_release(): void
    {
        $requester = $this->user([]);
        $creator = $this->user(['vouchers.create', 'vouchers.approve']);
        $actor = $this->user([
            'vouchers.approve', 'vouchers.post', 'rfp.pay',
            'cash_disbursements.create', 'cash_disbursements.approve', 'cash_disbursements.post',
        ]);
        $rfp = $this->approvedRfp($requester, $this->account('5000', 'Expense'));
        $cash = $this->account('1000', 'Asset');
        $token = $actor->createToken('test')->plainTextToken;
        $creatorToken = $creator->createToken('test')->plainTextToken;
        $voucherId = $this->withToken($creatorToken)->postJson('/api/cash-check-vouchers', $this->voucherPayload($rfp))
            ->assertCreated()->json('data.id');
        $this->withToken($creatorToken)->postJson("/api/cash-check-vouchers/{$voucherId}/approve")
            ->assertUnprocessable();
        Auth::forgetGuards();
        $this->withToken($token)->postJson("/api/cash-check-vouchers/{$voucherId}/pay")
            ->assertUnprocessable();
        $this->withToken($token)->postJson("/api/cash-check-vouchers/{$voucherId}/approve", ['approved_by' => 'Forged'])
            ->assertUnprocessable()->assertJsonValidationErrors('approved_by');
        $this->withToken($token)->postJson("/api/cash-check-vouchers/{$voucherId}/approve")
            ->assertOk()->assertJsonPath('data.status', 'Approved');
        $this->withToken($token)->postJson("/api/cash-check-vouchers/{$voucherId}/release", ['payment_account_id' => $cash->id])
            ->assertOk()->assertJsonPath('data.status', 'Released');
        $this->withToken($token)->postJson("/api/cash-check-vouchers/{$voucherId}/release", ['payment_account_id' => $cash->id])
            ->assertUnprocessable();
        $this->assertDatabaseHas('cash_disbursements', ['voucher_number' => 'V-100', 'status' => 'Posted']);
        $this->assertDatabaseHas('journal_entries', ['journal_number' => 'CD-1', 'status' => 'Posted']);
        $this->assertSame('Paid', $rfp->fresh()->status);
        $this->withToken($token)->postJson("/api/cash-check-vouchers/{$voucherId}/pay")
            ->assertOk()->assertJsonPath('data.status', 'Paid');
        $this->withToken($token)->postJson("/api/cash-check-vouchers/{$voucherId}/pay")
            ->assertUnprocessable();
    }

    public function test_create_permission_only_saves_draft_disbursement_and_direct_source_post_is_rejected(): void
    {
        $user = $this->user(['cash_disbursements.create', 'journal_vouchers.post']);
        $expense = $this->account('5000', 'Expense');
        $cash = $this->account('1000', 'Asset');
        $token = $user->createToken('test')->plainTextToken;
        $id = $this->withToken($token)->postJson('/api/cash-disbursements', [
            'disbursement_date' => '2026-10-10', 'voucher_number' => 'CDV-1',
            'payee' => 'Supplier', 'account_id' => $expense->id,
            'payment_account_id' => $cash->id, 'amount' => '10.00',
            'payment_method' => 'Cash',
        ])->assertCreated()->assertJsonPath('data.status', 'Draft')->json('data.id');
        $entry = JournalEntry::where('reference_type', 'CashDisbursement')->where('reference_id', $id)->firstOrFail();
        $this->assertSame('Draft', $entry->status);
        $this->withToken($token)->postJson("/api/journal-entries/{$entry->id}/post")
            ->assertUnprocessable();
        $this->withToken($token)->postJson("/api/cash-disbursements/{$id}/post")
            ->assertForbidden();
    }

    public function test_one_step_disbursement_post_requires_both_approval_and_post_permissions(): void
    {
        $user = $this->user(['cash_disbursements.create', 'cash_disbursements.post']);
        $expense = $this->account('5000', 'Expense');
        $cash = $this->account('1000', 'Asset');
        $payload = [
            'disbursement_date' => '2026-10-10', 'voucher_number' => 'CDV-2',
            'payee' => 'Supplier', 'account_id' => $expense->id,
            'payment_account_id' => $cash->id, 'amount' => '10.00',
            'payment_method' => 'Cash', 'post_now' => true,
        ];
        $token = $user->createToken('test')->plainTextToken;
        $this->withToken($token)->postJson('/api/cash-disbursements', $payload)->assertForbidden();
        $this->assertSame(0, JournalEntry::count());
        Permission::firstOrCreate(['name' => 'cash_disbursements.approve', 'guard_name' => 'web']);
        $user->givePermissionTo('cash_disbursements.approve');
        Auth::forgetGuards();
        $this->withToken($token)->postJson('/api/cash-disbursements', $payload)
            ->assertCreated()->assertJsonPath('data.status', 'Posted');
        $this->assertDatabaseHas('journal_entries', ['journal_number' => 'CD-1', 'status' => 'Posted']);
    }

    public function test_draft_disbursement_edit_and_delete_keep_journal_in_sync(): void
    {
        $user = $this->user(['cash_disbursements.create', 'cash_disbursements.edit', 'cash_disbursements.delete']);
        $expense = $this->account('5000', 'Expense');
        $cash = $this->account('1000', 'Asset');
        $payload = [
            'disbursement_date' => '2026-10-10', 'voucher_number' => 'CDV-3',
            'payee' => 'Supplier', 'account_id' => $expense->id,
            'payment_account_id' => $cash->id, 'amount' => '10.00',
            'payment_method' => 'Cash',
        ];
        $token = $user->createToken('test')->plainTextToken;
        $id = $this->withToken($token)->postJson('/api/cash-disbursements', $payload)
            ->assertCreated()->json('data.id');
        $this->withToken($token)->putJson("/api/cash-disbursements/{$id}", array_merge($payload, ['amount' => '20.00']))
            ->assertOk()->assertJsonPath('data.amount', '20.00');
        $entry = JournalEntry::where('reference_type', 'CashDisbursement')->where('reference_id', $id)->firstOrFail();
        $this->assertSame('20.00', $entry->lines()->orderBy('id')->first()->debit_amount);
        $this->withToken($token)->deleteJson("/api/cash-disbursements/{$id}")->assertOk();
        $this->assertDatabaseMissing('journal_entries', ['id' => $entry->id]);
        $this->assertDatabaseMissing('cash_disbursements', ['id' => $id]);
    }
}
