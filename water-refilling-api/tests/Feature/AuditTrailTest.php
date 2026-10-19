<?php

namespace Tests\Feature;

use App\Models\AuditLog;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Facades\Hash;
use Spatie\Permission\Models\Permission;
use Spatie\Permission\Models\Role;
use Tests\TestCase;

class AuditTrailTest extends TestCase
{
    use RefreshDatabase;

    private function createRoleWithPermission(
        string $roleName,
        string $permissionName
    ): Role {
        $permission = Permission::firstOrCreate([
            'name' => $permissionName,
            'guard_name' => 'web',
        ]);

        $role = Role::firstOrCreate([
            'name' => $roleName,
            'guard_name' => 'web',
        ]);

        $role->givePermissionTo($permission);

        return $role;
    }

    private function createUser(string $name, string $email, Role $role): User
    {
        $user = User::create([
            'name' => $name,
            'email' => $email,
            'password' => Hash::make('test-password'),
        ]);

        $user->assignRole($role);

        return $user;
    }

    public function test_owner_with_audit_permission_can_view_audit_logs(): void
    {
        $role = $this->createRoleWithPermission('Owner', 'audit_logs.view');
        $owner = $this->createUser('Test Owner', 'owner-audit@example.test', $role);

        AuditLog::create([
            'user_id' => $owner->id,
            'event' => 'login.success',
            'description' => 'User logged in successfully.',
            'metadata' => ['source' => 'test'],
        ]);

        $token = $owner->createToken('test')->plainTextToken;

        $this->withToken($token)
            ->getJson('/api/audit-logs')
            ->assertOk()
            ->assertJsonPath('success', true)
            ->assertJsonPath('data.0.event', 'login.success');
    }

    public function test_staff_cannot_view_audit_logs_even_if_permission_is_granted(): void
    {
        $role = $this->createRoleWithPermission('Staff', 'audit_logs.view');
        $staff = $this->createUser('Test Staff', 'staff-audit@example.test', $role);

        $token = $staff->createToken('test')->plainTextToken;

        $this->withToken($token)
            ->getJson('/api/audit-logs')
            ->assertForbidden();
    }

    public function test_admin_cannot_view_audit_logs_even_if_permission_is_granted(): void
    {
        $role = $this->createRoleWithPermission('Admin', 'audit_logs.view');
        $admin = $this->createUser('Test Admin', 'admin-audit@example.test', $role);

        $token = $admin->createToken('test')->plainTextToken;

        $this->withToken($token)
            ->getJson('/api/audit-logs')
            ->assertForbidden();
    }

    public function test_unauthenticated_user_cannot_view_audit_logs(): void
    {
        $this->getJson('/api/audit-logs')->assertUnauthorized();
    }

    public function test_failed_login_is_logged_without_password_or_token(): void
    {
        $password = 'never-store-this-password';

        $this->postJson('/api/login', [
            'email' => 'missing-audit@example.test',
            'password' => $password,
        ])->assertUnauthorized();

        $log = AuditLog::where('event', 'login.failed')->first();

        $this->assertNotNull($log);
        $serializedLog = json_encode($log->toArray());

        $this->assertStringNotContainsString($password, $serializedLog);
        $this->assertStringNotContainsString('plainTextToken', $serializedLog);
    }
}