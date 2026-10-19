<?php

namespace Tests\Feature;

use App\Models\Rfp;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\Hash;
use Spatie\Permission\Models\Permission;
use Tests\TestCase;

class RfpWorkflowTest extends TestCase
{
    use RefreshDatabase;

    public function test_rfp_can_be_created_as_a_server_owned_draft(): void
    {
        $user = $this->userWithPermissions(['rfp.create']);
        $this->authenticatedAs($user)->postJson('/api/rfps', [
            'rfp_number' => 'RFP-DRAFT-CREATE',
            'rfp_date' => '2026-10-10',
            'requested_by' => 'Requester',
            'payee' => 'Payee',
            'purpose' => 'Supplies',
            'amount' => '1000.00',
        ])->assertCreated()
            ->assertJsonPath('data.status', 'Draft')
            ->assertJsonPath('data.user_id', $user->id)
            ->assertJsonPath('data.prepared_by', $user->name);
    }

    public function test_rfp_creation_cannot_set_workflow_status_or_actor_metadata(): void
    {
        $user = $this->userWithPermissions(['rfp.create']);

        $this->authenticatedAs($user)
            ->postJson('/api/rfps', [
                'rfp_number' => 'RFP-001',
                'rfp_date' => '2026-10-10',
                'requested_by' => 'Requester',
                'payee' => 'Payee',
                'purpose' => 'Test request',
                'amount' => 1000,
                'status' => 'Approved',
                'approved_by' => 'Forged approver',
            ])
            ->assertUnprocessable();

        $this->assertDatabaseMissing('rfps', ['rfp_number' => 'RFP-001']);
    }

    public function test_only_draft_or_rejected_rfps_can_be_edited_and_workflow_fields_are_prohibited(): void
    {
        $user = $this->userWithPermissions(['rfp.edit']);
        $rfp = $this->createRfp('Submitted', $user);
        $token = $user->createToken('test')->plainTextToken;

        $this->authenticatedAs($user, $token)
            ->putJson("/api/rfps/{$rfp->id}", [
                'purpose' => 'Attempted edit',
            ])
            ->assertUnprocessable();

        $draft = $this->createRfp('Draft', $user, 'RFP-DRAFT');

        $this->authenticatedAs($user, $token)
            ->putJson("/api/rfps/{$draft->id}", [
                'status' => 'Approved',
                'approved_by' => 'Forged approver',
            ])
            ->assertUnprocessable();

        $this->assertSame('Submitted', $rfp->fresh()->status);
        $this->assertSame('Draft', $draft->fresh()->status);
    }

    public function test_review_and_approval_transitions_record_authenticated_actors(): void
    {
        $submitter = $this->userWithPermissions(['rfp.submit']);
        $reviewer = $this->userWithPermissions(['approvals.review']);
        $approver = $this->userWithPermissions(['rfp.approve']);
        $rfp = $this->createRfp('Draft', $submitter);

        $this->authenticatedAs($submitter)
            ->postJson("/api/rfps/{$rfp->id}/submit")
            ->assertOk()
            ->assertJsonPath('data.status', 'Submitted');

        $this->authenticatedAs($reviewer)
            ->postJson("/api/rfps/{$rfp->id}/review")
            ->assertOk()
            ->assertJsonPath('data.status', 'Under Review')
            ->assertJsonPath('data.reviewed_by', $reviewer->name);

        $this->authenticatedAs($approver)
            ->postJson("/api/rfps/{$rfp->id}/approve")
            ->assertOk()
            ->assertJsonPath('data.status', 'Approved')
            ->assertJsonPath('data.approved_by', $approver->name)
            ->assertJsonPath(
                'data.approved_by_date',
                now()->startOfDay()->toISOString()
            );
    }

    public function test_invalid_workflow_transition_and_missing_permission_are_rejected(): void
    {
        $user = $this->userWithPermissions(['rfp.approve']);
        $rfp = $this->createRfp('Draft', $user);
        $token = $user->createToken('test')->plainTextToken;

        $this->authenticatedAs($user, $token)
            ->postJson("/api/rfps/{$rfp->id}/approve")
            ->assertUnprocessable();

        $unauthorized = $this->userWithPermissions([]);

        $this->authenticatedAs($unauthorized)
            ->postJson("/api/rfps/{$rfp->id}/approve")
            ->assertForbidden();

        $this->assertSame('Draft', $rfp->fresh()->status);
    }

    public function test_rfp_requester_cannot_approve_their_own_request(): void
    {
        $user = $this->userWithPermissions([
            'rfp.submit',
            'approvals.review',
            'rfp.approve',
        ]);
        $rfp = $this->createRfp('Draft', $user);

        $this->authenticatedAs($user)
            ->postJson("/api/rfps/{$rfp->id}/submit")
            ->assertOk();

        $this->authenticatedAs($user)
            ->postJson("/api/rfps/{$rfp->id}/review")
            ->assertOk();

        $this->authenticatedAs($user)
            ->postJson("/api/rfps/{$rfp->id}/approve")
            ->assertUnprocessable()
            ->assertJsonValidationErrors('approver');

        $this->assertSame('Under Review', $rfp->fresh()->status);
        $this->assertNull($rfp->fresh()->approved_by);
    }

    public function test_rejection_requires_a_reason_and_only_reviewed_rfps_can_be_rejected(): void
    {
        $user = $this->userWithPermissions(['rfp.reject']);
        $rfp = $this->createRfp('Under Review', $user);
        $token = $user->createToken('test')->plainTextToken;

        $this->authenticatedAs($user, $token)
            ->postJson("/api/rfps/{$rfp->id}/reject")
            ->assertUnprocessable();

        $this->authenticatedAs($user, $token)
            ->postJson("/api/rfps/{$rfp->id}/reject", [
                'reason' => 'Please provide supporting documentation.',
            ])
            ->assertOk()
            ->assertJsonPath('data.status', 'Rejected')
            ->assertJsonPath('data.remarks', 'Please provide supporting documentation.');
    }

    private function userWithPermissions(array $permissions): User
    {
        $user = User::create([
            'name' => fake()->name(),
            'email' => fake()->unique()->safeEmail(),
            'password' => Hash::make('correct-password'),
        ]);

        foreach ($permissions as $permission) {
            Permission::firstOrCreate([
                'name' => $permission,
                'guard_name' => 'web',
            ]);
        }

        $user->givePermissionTo($permissions);

        return $user;
    }

    private function authenticatedAs(User $user, ?string $token = null)
    {
        Auth::forgetGuards();

        return $this->withToken(
            $token ?? $user->createToken('test')->plainTextToken
        );
    }

    private function createRfp(
        string $status,
        User $user,
        string $number = 'RFP-TEST'
    ): Rfp {
        return Rfp::create([
            'rfp_number' => $number,
            'rfp_date' => '2026-10-10',
            'requested_by' => 'Requester',
            'payee' => 'Payee',
            'purpose' => 'Test request',
            'amount' => 1000,
            'status' => $status,
            'user_id' => $user->id,
        ]);
    }
}
