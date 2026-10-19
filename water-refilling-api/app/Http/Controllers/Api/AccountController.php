<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Account;
use Illuminate\Http\Request;
use Illuminate\Validation\Rule;
use Illuminate\Validation\ValidationException;
use Illuminate\Support\Facades\DB;

class AccountController extends Controller
{
    /**
     * Display all chart of accounts records.
     */
    public function index(Request $request)
    {
        $query = Account::query();

        if ($request->filled('search')) {
            $search = $request->input('search');

            $query->where(function ($q) use ($search) {
                $q->where('account_code', 'like', "%{$search}%")
                    ->orWhere('account_name', 'like', "%{$search}%")
                    ->orWhere('account_group', 'like', "%{$search}%");
            });
        }

        if ($request->filled('account_type')) {
            $query->where('account_type', $request->input('account_type'));
        }

        if ($request->filled('is_active')) {
            $query->where('is_active', $request->boolean('is_active'));
        }

        $accounts = $query
            ->orderBy('account_code')
            ->get();

        return response()->json([
            'success' => true,
            'data' => $accounts,
        ]);
    }

    /**
     * Store a new account.
     */
    public function store(Request $request)
    {
        $validated = $request->validate([
            'account_code' => [
                'required',
                'string',
                'max:20',
                'unique:accounts,account_code',
            ],
            'account_name' => ['required', 'string', 'max:150'],
            'account_type' => [
                'required',
                Rule::in([
                    'Asset',
                    'Liability',
                    'Equity',
                    'Revenue',
                    'Expense',
                ]),
            ],
            'account_group' => ['nullable', 'string', 'max:100'],
            'description' => ['nullable', 'string'],
            'is_active' => ['sometimes', 'boolean'],
        ]);

        $account = Account::create($validated);

        return response()->json([
            'success' => true,
            'message' => 'Account created successfully.',
            'data' => $account,
        ], 201);
    }

    /**
     * Display a specific account.
     */
    public function show(Account $account)
    {
        return response()->json([
            'success' => true,
            'data' => $account,
        ]);
    }

    /**
     * Update an existing account.
     */
    public function update(Request $request, Account $account)
    {
        $validated = $request->validate([
            'account_code' => [
                'sometimes',
                'required',
                'string',
                'max:20',
                Rule::unique('accounts', 'account_code')->ignore($account->id),
            ],
            'account_name' => ['sometimes', 'required', 'string', 'max:150'],
            'account_type' => [
                'sometimes',
                'required',
                Rule::in([
                    'Asset',
                    'Liability',
                    'Equity',
                    'Revenue',
                    'Expense',
                ]),
            ],
            'account_group' => ['sometimes', 'nullable', 'string', 'max:100'],
            'description' => ['sometimes', 'nullable', 'string'],
            'is_active' => ['sometimes', 'boolean'],
        ]);

        DB::transaction(function () use ($account, $validated) {
            $locked = Account::whereKey($account->id)->lockForUpdate()->firstOrFail();
            if (isset($validated['account_type']) &&
                $validated['account_type'] !== $locked->account_type &&
                $locked->journalEntryLines()->whereHas('journalEntry', fn ($query) => $query->where('status', 'Posted'))->exists()) {
                throw ValidationException::withMessages(['account_type' => 'An account with posted journal history cannot change accounting type.']);
            }
            $locked->update($validated);
        });

        return response()->json([
            'success' => true,
            'message' => 'Account updated successfully.',
            'data' => $account->fresh(),
        ]);
    }

    /**
     * Deactivate an account without deleting its history.
     */
    public function destroy(Account $account)
    {
        if ($account->journalEntryLines()->exists()) {
            $account->update(['is_active' => false]);

            return response()->json([
                'success' => true,
                'message' => 'Account has journal history and was deactivated instead of deleted.',
                'data' => $account->fresh(),
            ]);
        }

        $account->delete();

        return response()->json([
            'success' => true,
            'message' => 'Account deleted successfully.',
        ]);
    }
}
