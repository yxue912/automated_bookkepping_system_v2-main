<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\AuditLog;
use App\Models\User;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Hash;
use Illuminate\Validation\Rules\Password;
use Laravel\Sanctum\PersonalAccessToken;

class AuthController extends Controller
{
    public function register(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'name' => ['required', 'string', 'max:255'],
            'email' => [
                'required',
                'string',
                'lowercase',
                'email',
                'max:255',
                'unique:users,email',
            ],
            'password' => ['required', 'confirmed', Password::defaults()],
        ]);

        $user = new User();
        $user->name = $validated['name'];
        $user->email = $validated['email'];
        $user->password = Hash::make($validated['password']);
        $user->is_active = false;
        $user->save();

        $this->recordAuditEvent(
            request: $request,
            event: 'account.registered',
            description: 'A new account registered and is awaiting approval.',
            userId: $user->id,
            metadata: ['registration_status' => 'pending']
        );

        return response()->json([
            'success' => true,
            'message' => 'Account created. Please wait for Owner approval before logging in.',
            'user' => [
                'id' => $user->id,
                'name' => $user->name,
                'email' => $user->email,
                'is_active' => $user->is_active,
            ],
        ], 201);
    }

    public function login(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'email' => ['required', 'email'],
            'password' => ['required', 'string'],
        ]);

        $user = User::where('email', $validated['email'])->first();

        if (! $user || ! Hash::check($validated['password'], $user->password)) {
            $this->recordAuditEvent(
                request: $request,
                event: 'login.failed',
                description: 'Login failed due to invalid credentials.',
                userId: $user?->id,
                metadata: ['reason' => 'invalid_credentials']
            );

            return response()->json([
                'success' => false,
                'message' => 'Invalid email or password.',
            ], 401);
        }

        if (! $user->is_active) {
            $this->recordAuditEvent(
                request: $request,
                event: 'login.pending_approval',
                description: 'A pending account attempted to log in.',
                userId: $user->id,
                metadata: ['reason' => 'account_pending_approval']
            );

            return response()->json([
                'success' => false,
                'message' => 'Your account is awaiting Owner approval. Please try again after approval.',
                'code' => 'ACCOUNT_PENDING_APPROVAL',
            ], 403);
        }

        $token = $user->createToken(
            'desktop-app',
            ['*'],
            now()->addHours(8)
        );

        $this->recordAuditEvent(
            request: $request,
            event: 'login.success',
            description: 'User logged in successfully.',
            userId: $user->id
        );

        return response()->json([
            'success' => true,
            'message' => 'Login successful.',
            'token' => $token->plainTextToken,
            'token_type' => 'Bearer',
            'user' => [
                'id' => $user->id,
                'name' => $user->name,
                'email' => $user->email,
                'roles' => $user->getRoleNames(),
                'permissions' => $user->getAllPermissions()->pluck('name')->values(),
            ],
        ]);
    }

    public function logout(Request $request): JsonResponse
    {
        $user = $request->user();
        $token = $user?->currentAccessToken();

        $this->recordAuditEvent(
            request: $request,
            event: 'logout.success',
            description: 'User logged out.',
            userId: $user?->id
        );

        if ($token instanceof PersonalAccessToken) {
            $token->delete();
        }

        return response()->json([
            'success' => true,
            'message' => 'Logged out successfully.',
        ]);
    }

    private function recordAuditEvent(
        Request $request,
        string $event,
        string $description,
        ?int $userId = null,
        array $metadata = []
    ): void {
        try {
            AuditLog::create([
                'user_id' => $userId,
                'event' => $event,
                'description' => $description,
                'ip_address' => $request->ip(),
                'user_agent' => mb_substr(
                    (string) $request->userAgent(),
                    0,
                    500
                ),
                'metadata' => $metadata,
            ]);
        } catch (\Throwable $exception) {
            // Audit logging must not expose internal errors or passwords.
            // Report the failure to server logs for later investigation.
            report($exception);
        }
    }
}