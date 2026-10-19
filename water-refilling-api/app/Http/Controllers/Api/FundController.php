<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Fund;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class FundController extends Controller
{
    public function index(): JsonResponse
    {
        return response()->json([
            'success' => true,
            'data' => Fund::orderBy('name')->get(),
        ]);
    }

    public function store(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'fund_code' => ['required', 'string', 'max:20', 'unique:funds,fund_code'],
            'name' => ['required', 'string', 'max:150'],
            'description' => ['nullable', 'string'],
        ]);

        $fund = Fund::create($validated);

        return response()->json([
            'success' => true,
            'message' => 'Fund created successfully.',
            'data' => $fund,
        ], 201);
    }

    public function show(Fund $fund): JsonResponse
    {
        return response()->json([
            'success' => true,
            'data' => $fund,
        ]);
    }

    public function update(Request $request, Fund $fund): JsonResponse
    {
        $validated = $request->validate([
            'fund_code' => [
                'required',
                'string',
                'max:20',
                'unique:funds,fund_code,' . $fund->id,
            ],
            'name' => ['required', 'string', 'max:150'],
            'description' => ['nullable', 'string'],
            'is_active' => ['sometimes', 'boolean'],
        ]);

        $fund->update($validated);

        return response()->json([
            'success' => true,
            'message' => 'Fund updated successfully.',
            'data' => $fund,
        ]);
    }

    public function destroy(Fund $fund): JsonResponse
    {
        $fund->update([
            'is_active' => false,
        ]);

        return response()->json([
            'success' => true,
            'message' => 'Fund deactivated successfully.',
        ]);
    }
}