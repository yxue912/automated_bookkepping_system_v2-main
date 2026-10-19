<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\FixedAsset;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Validation\ValidationException;

class FixedAssetController extends Controller
{

public function index(Request $request): JsonResponse
{
    $query = FixedAsset::query();

    $search = trim((string) $request->query('search', ''));

    if ($search !== '') {
        $query->where(function ($builder) use ($search) {
            $builder->where('asset_code', 'like', '%' . $search . '%')
                ->orWhere('name', 'like', '%' . $search . '%')
                ->orWhere('category', 'like', '%' . $search . '%')
                ->orWhere('location', 'like', '%' . $search . '%');
        });
    }

    return response()->json([
        'success' => true,
        'data' => $query->orderBy('name')->get(),
    ]);
}


    public function store(Request $request): JsonResponse
{
    $validated = $request->validate([
        'name' => ['required', 'string', 'max:150'],
        'category' => ['nullable', 'string', 'max:100'],
        'acquisition_date' => ['nullable', 'date'],
        'acquisition_cost' => ['required', 'numeric', 'decimal:0,2', 'min:0'],
        'useful_life_years' => ['nullable', 'numeric', 'gt:0'],
        'salvage_value' => ['required', 'numeric', 'decimal:0,2', 'min:0'],
        'location' => ['nullable', 'string', 'max:150'],
        'account_id' => ['nullable', 'integer', 'exists:accounts,id'],
        'description' => ['nullable', 'string'],
    ]);

    $this->validateDepreciationBase($validated);

    $fixedAsset = \Illuminate\Support\Facades\DB::transaction(
        function () use ($validated) {
            // Generate the next available asset code.
            $lastNumber = FixedAsset::query()
                ->where('asset_code', 'like', 'AST-%')
                ->selectRaw(
                    "MAX(CAST(SUBSTR(asset_code, 5) AS INTEGER)) as last_number"
                )
                ->value('last_number');

            $nextNumber = ((int) $lastNumber) + 1;

            do {
                $assetCode = 'AST-' . str_pad(
                    (string) $nextNumber,
                    4,
                    '0',
                    STR_PAD_LEFT
                );

                $nextNumber++;
            } while (
                FixedAsset::where('asset_code', $assetCode)->exists()
            );

            $validated['asset_code'] = $assetCode;

            return FixedAsset::create($validated);
        }
    );

    return response()->json([
        'success' => true,
        'message' => 'Fixed asset created successfully.',
        'data' => $fixedAsset,
    ], 201);
}

    public function show(FixedAsset $fixedAsset): JsonResponse
    {
        return response()->json([
            'success' => true,
            'data' => $fixedAsset,
        ]);
    }

    public function update(
        Request $request,
        FixedAsset $fixedAsset
    ): JsonResponse {
        $validated = $request->validate([
            'asset_code' => [
                'required',
                'string',
                'max:20',
                'unique:fixed_assets,asset_code,' . $fixedAsset->id,
            ],
            'name' => ['required', 'string', 'max:150'],
            'category' => ['nullable', 'string', 'max:100'],
            'acquisition_date' => ['nullable', 'date'],
            'acquisition_cost' => ['required', 'numeric', 'decimal:0,2', 'min:0'],
            'useful_life_years' => ['nullable', 'numeric', 'gt:0'],
            'salvage_value' => ['required', 'numeric', 'decimal:0,2', 'min:0'],
            'location' => ['nullable', 'string', 'max:150'],
            'account_id' => ['nullable', 'integer', 'exists:accounts,id'],
            'description' => ['nullable', 'string'],
            'is_active' => ['sometimes', 'boolean'],
        ]);

        $this->validateDepreciationBase($validated);
        $fixedAsset->update($validated);

        return response()->json([
            'success' => true,
            'message' => 'Fixed asset updated successfully.',
            'data' => $fixedAsset->fresh(),
        ]);
    }

    public function destroy(FixedAsset $fixedAsset): JsonResponse
    {
        if (!$fixedAsset->is_active) {
            return response()->json([
                'success' => true,
                'message' => 'This fixed asset is already inactive.',
            ]);
        }

        $fixedAsset->update([
            'is_active' => false,
        ]);

        return response()->json([
            'success' => true,
            'message' => 'Fixed asset deactivated successfully.',
        ]);
    }

    private function validateDepreciationBase(array $validated): void
    {
        if (bccomp((string) $validated['salvage_value'], (string) $validated['acquisition_cost'], 2) > 0) {
            throw ValidationException::withMessages(['salvage_value' => 'Salvage value cannot exceed acquisition cost.']);
        }
    }
}
