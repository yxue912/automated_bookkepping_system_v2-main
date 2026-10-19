<?php

namespace Tests\Feature;

use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\Hash;
use Spatie\Permission\Models\Permission;
use Spatie\Permission\Models\Role;
use Tests\TestCase;

class ApiAuthenticationTest extends TestCase
{
    use RefreshDatabase;

    public function test_private_api_rejects_requests_without_a_token(): void
    {
        $this->getJson('/api/accounts')->assertUnauthorized();
    }

    public function test_valid_login_issues_token_that_can_access_authorized_api(): void
    {
        $user = User::create([
            'name' => 'Test Accountant',
            'email' => 'accountant@example.test',
            'password' => Hash::make('correct-password'),
        ]);

        Permission::create([
            'name' => 'accounts.view',
            'guard_name' => 'web',
        ]);

        $user->givePermissionTo('accounts.view');

        $login = $this->postJson('/api/login', [
            'email' => $user->email,
            'password' => 'correct-password',
        ])->assertOk()
            ->assertJsonStructure([
                'token',
                'token_type',
                'user' => ['id', 'email', 'roles', 'permissions'],
            ])
            ->assertJsonPath('token_type', 'Bearer');

        $token = $login->json('token');
        $this->assertNotEmpty($token);

        $this->withToken($token)
            ->getJson('/api/accounts')
            ->assertOk()
            ->assertJsonStructure(['success', 'data']);
    }

    public function test_registration_creates_inactive_account_without_a_role_or_token(): void
    {
        $response = $this->postJson('/api/register', [
            'name' => 'Pending Staff',
            'email' => 'pending@example.test',
            'password' => 'Strong-Pass-123!',
            'password_confirmation' => 'Strong-Pass-123!',
        ]);

        $response->assertCreated()
            ->assertJsonPath('success', true)
            ->assertJsonPath('user.is_active', false)
            ->assertJsonMissingPath('token');

        $user = User::where('email', 'pending@example.test')->first();

        $this->assertNotNull($user);
        $this->assertFalse($user->is_active);
        $this->assertCount(0, $user->getRoleNames());
    }

    public function test_pending_user_cannot_log_in_or_receive_a_token(): void
    {
        $user = User::create([
            'name' => 'Pending User',
            'email' => 'pending-login@example.test',
            'password' => Hash::make('correct-password'),
        ]);

        $user->is_active = false;
        $user->save();

        $this->postJson('/api/login', [
            'email' => $user->email,
            'password' => 'correct-password',
        ])->assertForbidden()
            ->assertJsonPath('code', 'ACCOUNT_PENDING_APPROVAL')
            ->assertJsonMissingPath('token');

        $this->assertCount(0, $user->tokens);
    }

    public function test_owner_can_approve_pending_user_and_assign_staff_role(): void
    {
        $ownerRole = Role::create([
            'name' => 'Owner',
            'guard_name' => 'web',
        ]);

        $manageUsers = Permission::create([
            'name' => 'users.manage',
            'guard_name' => 'web',
        ]);

        $ownerRole->givePermissionTo($manageUsers);

        $owner = User::create([
            'name' => 'Company Owner',
            'email' => 'owner@example.test',
            'password' => Hash::make('correct-password'),
        ]);

        $owner->assignRole($ownerRole);

        Role::create([
            'name' => 'Staff',
            'guard_name' => 'web',
        ]);

        $pending = User::create([
            'name' => 'New Staff',
            'email' => 'new-staff@example.test',
            'password' => Hash::make('correct-password'),
        ]);

        $pending->is_active = false;
        $pending->save();

        $token = $owner->createToken('test')->plainTextToken;

        $this->withToken($token)
            ->getJson('/api/pending-users')
            ->assertOk()
            ->assertJsonPath('users.0.email', 'new-staff@example.test');

        $this->withToken($token)
            ->postJson("/api/pending-users/{$pending->id}/approve")
            ->assertOk()
            ->assertJsonPath('success', true)
            ->assertJsonPath('user.is_active', true)
            ->assertJsonPath('user.roles.0', 'Staff');

        $pending->refresh();

        $this->assertTrue($pending->is_active);
        $this->assertTrue($pending->hasRole('Staff'));
        $this->assertFalse($pending->hasRole('Owner'));
        $this->assertFalse($pending->hasRole('Admin'));
    }

    public function test_staff_cannot_access_owner_approval_routes(): void
    {
        $staffRole = Role::create([
            'name' => 'Staff',
            'guard_name' => 'web',
        ]);

        $manageUsers = Permission::create([
            'name' => 'users.manage',
            'guard_name' => 'web',
        ]);

        // Even if this permission is mistakenly granted, the role check
        // must still prevent Staff from approving accounts.
        $staffRole->givePermissionTo($manageUsers);

        $staff = User::create([
            'name' => 'Staff User',
            'email' => 'staff@example.test',
            'password' => Hash::make('correct-password'),
        ]);

        $staff->assignRole($staffRole);

        $token = $staff->createToken('test')->plainTextToken;

        $this->withToken($token)
            ->getJson('/api/pending-users')
            ->assertForbidden();
    }

    public function test_authenticated_user_without_permission_cannot_access_route(): void
    {
        $user = User::create([
            'name' => 'Restricted Accountant',
            'email' => 'restricted@example.test',
            'password' => Hash::make('correct-password'),
        ]);

        $token = $user->createToken('test')->plainTextToken;

        $this->withToken($token)
            ->getJson('/api/accounts')
            ->assertForbidden();
    }

    public function test_logout_revokes_the_current_token(): void
    {
        $user = User::create([
            'name' => 'Test Accountant',
            'email' => 'logout@example.test',
            'password' => Hash::make('correct-password'),
        ]);

        $token = $user->createToken('test')->plainTextToken;

        $this->withToken($token)
            ->postJson('/api/logout')
            ->assertOk();

        $this->assertCount(0, $user->tokens);

        Auth::forgetGuards();

        $this->withToken($token)
            ->getJson('/api/accounts')
            ->assertUnauthorized();
    }

    public function test_invalid_credentials_and_expired_tokens_are_rejected(): void
    {
        $user = User::create([
            'name' => 'Login User',
            'email' => 'login@example.test',
            'password' => Hash::make('correct-password'),
        ]);

        $this->postJson('/api/login', [
            'email' => $user->email,
            'password' => 'wrong',
        ])->assertUnauthorized();

        $this->postJson('/api/login', [
            'email' => 'invalid',
            'password' => 'wrong',
        ])->assertUnprocessable();

        $token = $user->createToken(
            'expired',
            ['*'],
            now()->subMinute()
        )->plainTextToken;

        $this->withToken($token)
            ->getJson('/api/accounts')
            ->assertUnauthorized();
    }

    public function test_login_is_rate_limited(): void
    {
        for ($attempt = 0; $attempt < 5; $attempt++) {
            $this->postJson('/api/login', [
                'email' => 'missing@example.test',
                'password' => 'wrong',
            ])->assertUnauthorized();
        }

        $this->postJson('/api/login', [
            'email' => 'missing@example.test',
            'password' => 'wrong',
        ])->assertStatus(429);
    }
}