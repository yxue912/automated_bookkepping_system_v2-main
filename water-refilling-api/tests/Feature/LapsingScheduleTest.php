<?php

namespace Tests\Feature;

use App\Models\FixedAsset;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Facades\Hash;
use Spatie\Permission\Models\Permission;
use Tests\TestCase;

class LapsingScheduleTest extends TestCase
{
    use RefreshDatabase;

    public function test_daily_report_excludes_future_acquisitions_and_tracks_accumulated_depreciation(): void
    {
        FixedAsset::create([
            'asset_code' => 'FA-001',
            'name' => 'Office Equipment',
            'category' => 'Equipment',
            'acquisition_date' => '2026-10-08',
            'acquisition_cost' => 55000,
            'useful_life_years' => 5.5,
            'salvage_value' => 0,
            'is_active' => true,
        ]);

        FixedAsset::create([
            'asset_code' => 'FA-002',
            'name' => 'Future Asset',
            'category' => 'Equipment',
            'acquisition_date' => '2026-10-11',
            'acquisition_cost' => 25000,
            'useful_life_years' => 5,
            'salvage_value' => 0,
            'is_active' => true,
        ]);

        $response = $this->withToken($this->reportToken())
            ->getJson('/api/reports/lapsing-schedule?period=daily&date=2026-10-10');

        $response->assertOk();

        $data = $response->json('data');
        $this->assertCount(2, $data);

        $firstAsset = collect($data)->first(fn (array $asset) => $asset['asset_code'] === 'FA-001');
        $secondAsset = collect($data)->first(fn (array $asset) => $asset['asset_code'] === 'FA-002');

        $this->assertEquals(10000.0, $firstAsset['annual_depreciation']);
        $this->assertEqualsWithDelta(54.76, $firstAsset['accumulated_depreciation'], 0.01);
        $this->assertEqualsWithDelta(54945.24, $firstAsset['net_book_value'], 0.01);
        $this->assertEqualsWithDelta(27.38, $firstAsset['period_depreciation'], 0.01);

        $this->assertTrue($secondAsset['not_yet_acquired']);
        $this->assertFalse($secondAsset['eligible_for_depreciation']);
        $this->assertEquals(0.0, (float) $secondAsset['annual_depreciation']);
        $this->assertEquals(0.0, (float) $secondAsset['accumulated_depreciation']);
        $this->assertEquals(0.0, (float) $secondAsset['period_depreciation']);
        $this->assertEquals(25000.0, (float) $secondAsset['net_book_value']);
    }

    public function test_monthly_report_prorates_depreciation_by_period_length(): void
    {
        FixedAsset::create([
            'asset_code' => 'FA-010',
            'name' => 'Manufacturing Asset',
            'category' => 'Equipment',
            'acquisition_date' => '2026-10-05',
            'acquisition_cost' => 50000,
            'useful_life_years' => 5,
            'salvage_value' => 5000,
            'is_active' => true,
        ]);

        $response = $this->withToken($this->reportToken())
            ->getJson('/api/reports/lapsing-schedule?period=monthly&date=2026-10-31');

        $response->assertOk();

        $asset = collect($response->json('data'))->first();

        $annualDepreciation = 9000.0;
        $this->assertEqualsWithDelta($annualDepreciation, $asset['annual_depreciation'], 0.01);
        $this->assertEqualsWithDelta($annualDepreciation * (27 / 365.2425), $asset['period_depreciation'], 0.5);
        $this->assertEqualsWithDelta($annualDepreciation * (26 / 365.2425), $asset['accumulated_depreciation'], 0.5);
    }

    public function test_invalid_useful_life_values_are_ignored_and_net_book_value_remains_positive(): void
    {
        FixedAsset::create([
            'asset_code' => 'FA-011',
            'name' => 'Invalid Life Asset',
            'category' => 'Equipment',
            'acquisition_date' => '2026-10-01',
            'acquisition_cost' => 12000,
            'useful_life_years' => 0,
            'salvage_value' => 0,
            'is_active' => true,
        ]);

        $response = $this->withToken($this->reportToken())
            ->getJson('/api/reports/lapsing-schedule?period=daily&date=2026-10-10');

        $response->assertOk();

        $asset = collect($response->json('data'))->first();

        $this->assertFalse($asset['eligible_for_depreciation']);
        $this->assertEquals(0.0, (float) $asset['annual_depreciation']);
        $this->assertEquals(0.0, (float) $asset['accumulated_depreciation']);
        $this->assertEquals(12000.0, (float) $asset['net_book_value']);
    }

    private function reportToken(): string
    {
        $user = User::create([
            'name' => 'Report Reader',
            'email' => 'report-reader@example.test',
            'password' => Hash::make('correct-password'),
        ]);

        Permission::firstOrCreate([
            'name' => 'financial_reports.view',
            'guard_name' => 'web',
        ]);
        $user->givePermissionTo('financial_reports.view');

        return $user->createToken('report-test')->plainTextToken;
    }
}
