<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class RfpDetail extends Model
{
    protected $fillable = [
        'account_id',
        'account_code',
        'account_title',
        'location_project',
        'debit_amount',
        'credit_amount',
    ];

    protected $casts = [
        'debit_amount' => 'decimal:2',
        'credit_amount' => 'decimal:2',
    ];

    public function rfp(): BelongsTo
    {
        return $this->belongsTo(Rfp::class);
    }

    public function account(): BelongsTo
    {
        return $this->belongsTo(Account::class);
    }
}
