<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\AuditLog;
use App\Models\User;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Spatie\Permission\Models\Role;

class PendingUserController extends Controller
{
    /**
     * List accounts awaiting Owner approval.
     */
    public function index(): JsonResponse
    {
        $users = User::query()
            ->where('is_active', false)
            ->orderBy('created_at')
            ->get(['id', 'name', 'email', 'created_at']);

        return response()->json([
            'success' => true,
            'users' => $users,
        ]);
    }

    /**
     * Approve an account and assign the Staff role.
     */
    public function approve(Request $request, User $user): JsonResponse
    {
        if ($user->is_active) {
            return response()->json([
                'success' => false,
                'message' => 'This account is already active.',
            ], 409);
        }

        $staffRole = Role::where('name', 'Staff')
            ->where('guard_name', 'web')
            ->first();

        if (! $staffRole) {
            return response()->json([
                'success' => false,
                'message' => 'The Staff role is not configured.',
            ], 500);
        }

        DB::transaction(function () use ($user, $staffRole) {
            $user->is_active = true;
            $user->save();

            $user->syncRoles([$staffRole]);
        });

        try {
            AuditLog::create([
                'user_id' => $request->user()->id,
                'event' => 'account.approved',
                'description' => 'Owner approved a pending account and assigned Staff role.',
                'ip_address' => $request->ip(),
                'user_agent' => mb_substr(
                    (string) $request->userAgent(),
                    0,
                    500
                ),
                'metadata' => [
                    'approved_user_id' => $user->id,
                    'assigned_role' => 'Staff',
                ],
            ]);
        } catch (\Throwable $exception) {
            report($exception);
        }

        return response()->json([
            'success' => true,
            'message' => 'Account approved and assigned the Staff role.',
            'user' => [
                'id' => $user->id,
                'name' => $user->name,
                'email' => $user->email,
                'is_active' => $user->is_active,
                'roles' => $user->getRoleNames(),
            ],
        ]);
    }
}