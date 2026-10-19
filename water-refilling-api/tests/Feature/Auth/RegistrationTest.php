<?php

namespace Tests\Feature\Auth;

use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Tests\TestCase;

class RegistrationTest extends TestCase
{
    use RefreshDatabase;

    public function test_registration_is_closed_by_default(): void
    {
        config(['auth.allow_registration' => false]);

        $this->get('/register')->assertNotFound();
        $this->post('/register', [])->assertNotFound();
    }

    public function test_registration_screen_can_be_rendered(): void
    {
        config(['auth.allow_registration' => true]);

        $response = $this->get('/register');

        $response->assertStatus(200);
    }

    public function test_new_users_register_pending_owner_approval(): void
    {
        config(['auth.allow_registration' => true]);

        $response = $this->post('/register', [
            'name' => 'Test User',
            'email' => 'test@example.com',
            'password' => 'password',
            'password_confirmation' => 'password',
        ]);

        $user = User::where('email', 'test@example.com')->first();

        $this->assertNotNull($user);
        $this->assertFalse($user->is_active);
        $this->assertGuest();

        $response->assertRedirect(route('login'));
        $response->assertSessionHas(
            'status',
            'Account created successfully. Please wait for Owner approval before logging in.'
        );
    }
}