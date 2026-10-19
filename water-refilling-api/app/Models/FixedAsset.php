<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class FixedAsset extends Model
{
    protected $fillable = [
        'asset_code',
        'name',
        'category',
        'acquisition_date',
        'acquisition_cost',
        'useful_life_years',
        'salvage_value',
        'location',
        'account_id',
        'description',
        'is_active',
    ];

    protected $casts = [
        'acquisition_date' => 'date',
        'acquisition_cost' => 'decimal:2',
        'useful_life_years' => 'decimal:2',
        'salvage_value' => 'decimal:2',
        'is_active' => 'boolean',
    ];
}