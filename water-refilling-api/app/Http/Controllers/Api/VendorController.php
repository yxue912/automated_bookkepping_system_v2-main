<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Vendor;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;

class VendorController extends Controller
{
    public function index(): JsonResponse
    {
        return response()->json([
            'success' => true,
            'data' => Vendor::orderBy('name')->get(),
        ]);
    }

    public function store(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'vendor_code' => ['required', 'string', 'max:20', 'unique:vendors,vendor_code'],
            'name' => ['required', 'string', 'max:150'],
            'contact_number' => ['nullable', 'string', 'max:30'],
            'email' => ['nullable', 'email', 'max:150'],
            'address' => ['nullable', 'string'],
        ]);

        $vendor = Vendor::create($validated);

        return response()->json([
            'success' => true,
            'message' => 'Vendor created successfully.',
            'data' => $vendor,
        ], 201);
    }

    public function show(Vendor $vendor): JsonResponse
    {
        return response()->json([
            'success' => true,
            'data' => $vendor,
        ]);
    }

    public function update(Request $request, Vendor $vendor): JsonResponse
    {
        $validated = $request->validate([
            'vendor_code' => [
                'required',
                'string',
                'max:20',
                'unique:vendors,vendor_code,' . $vendor->id,
            ],
            'name' => ['required', 'string', 'max:150'],
            'contact_number' => ['nullable', 'string', 'max:30'],
            'email' => ['nullable', 'email', 'max:150'],
            'address' => ['nullable', 'string'],
            'is_active' => ['sometimes', 'boolean'],
        ]);

        $vendor->update($validated);

        return response()->json([
            'success' => true,
            'message' => 'Vendor updated successfully.',
            'data' => $vendor,
        ]);
    }

    public function destroy(Vendor $vendor): JsonResponse
    {
        $vendor->update([
            'is_active' => false,
        ]);

        return response()->json([
            'success' => true,
            'message' => 'Vendor deactivated successfully.',
        ]);
    }
}