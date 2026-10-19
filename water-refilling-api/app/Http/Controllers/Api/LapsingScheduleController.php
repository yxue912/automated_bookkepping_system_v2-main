<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\FixedAsset;
use Carbon\Carbon;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Validation\Rule;

class LapsingScheduleController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'as_of' => ['sometimes', 'date_format:Y-m-d'],
            'date' => ['sometimes', 'date_format:Y-m-d'],
            'period' => ['sometimes', Rule::in(['daily', 'monthly'])],
        ]);
        $asOf = Carbon::parse($validated['date'] ?? $validated['as_of'] ?? now()->toDateString())->startOfDay();
        $period = $validated['period'] ?? 'daily';
        $periodStart = $period === 'monthly' ? $asOf->copy()->startOfMonth() : $asOf->copy();

        $includeFuture = isset($validated['period']) || isset($validated['date']);
        $assets = FixedAsset::query()
            ->when(! $includeFuture, fn ($query) => $query->where(function ($query) use ($asOf) {
                $query->whereNull('acquisition_date')->orWhereDate('acquisition_date', '<=', $asOf->toDateString());
            }))
            ->orderBy('asset_code')->get()
            ->map(function (FixedAsset $asset) use ($asOf, $periodStart) {
                $cost = (string) $asset->acquisition_cost;
                $salvage = (string) $asset->salvage_value;
                $life = (string) ($asset->useful_life_years ?? '0');
                $acquired = $asset->acquisition_date
                    ? Carbon::parse($asset->acquisition_date)->startOfDay() : null;
                $notYetAcquired = $acquired === null || $acquired->greaterThan($asOf);
                $base = bccomp($cost, $salvage, 2) > 0 ? bcsub($cost, $salvage, 2) : '0.00';
                $eligible = ! $notYetAcquired && bccomp($life, '0', 2) > 0 && bccomp($base, '0', 2) > 0;
                $annual = $eligible ? bcdiv($base, $life, 8) : '0.00';
                $daily = $eligible ? bcdiv($annual, '365.2425', 8) : '0.00';
                $elapsed = $eligible ? (int) $acquired->diffInDays($asOf) : 0;
                $accumulatedRaw = bcmul($daily, (string) $elapsed, 8);
                $accumulated = bccomp($accumulatedRaw, $base, 8) > 0 ? $base : $accumulatedRaw;
                $firstDay = $acquired && $acquired->greaterThan($periodStart)
                    ? $acquired : $periodStart;
                $periodDays = $eligible && $firstDay->lessThanOrEqualTo($asOf)
                    ? (int) $firstDay->diffInDays($asOf) + 1 : 0;
                $beforeDays = $eligible && $acquired->lessThan($periodStart)
                    ? (int) $acquired->diffInDays($periodStart) : 0;
                $beforePeriodRaw = bcmul($daily, (string) $beforeDays, 8);
                $beforePeriod = bccomp($beforePeriodRaw, $base, 8) > 0 ? $base : $beforePeriodRaw;
                $remaining = bcsub($base, $beforePeriod, 8);
                $periodRaw = bcmul($daily, (string) $periodDays, 8);
                $periodDepreciation = bccomp($periodRaw, $remaining, 8) > 0 ? $remaining : $periodRaw;
                $netBookValue = bcsub($cost, $accumulated, 8);
                if (bccomp($netBookValue, $salvage, 8) < 0) {
                    $netBookValue = $salvage;
                }

                return [
                    'asset_code' => $asset->asset_code,
                    'name' => $asset->name,
                    'category' => $asset->category,
                    'acquisition_date' => $acquired?->toDateString(),
                    'acquisition_cost' => (float) $this->money($cost),
                    'useful_life_years' => (float) $life,
                    'salvage_value' => (float) $this->money($salvage),
                    'annual_depreciation' => (float) $this->money($annual),
                    'accumulated_depreciation' => (float) $this->money($accumulated),
                    'period_depreciation' => (float) $this->money($periodDepreciation),
                    'net_book_value' => (float) $this->money($netBookValue),
                    'not_yet_acquired' => $notYetAcquired,
                    'eligible_for_depreciation' => $eligible,
                ];
            });

        return response()->json([
            'success' => true,
            'as_of' => $asOf->toDateString(),
            'data' => $assets,
        ]);
    }

    private function money(string $value): string
    {
        return bcdiv(bcadd($value, '0.005', 8), '1', 2);
    }
}
