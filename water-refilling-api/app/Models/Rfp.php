<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Illuminate\Database\Eloquent\Relations\HasOne;

class Rfp extends Model
{
    protected $fillable = [
        'rfp_number',
        'rfp_date',
        'requested_by',
        'payee',
        'purpose',
        'amount',
        'status',
        'remarks',
        'account_id',
        'user_id',
        'reference_invoice_no',
        'tax_identification_number',
        'currency',
        'prepared_by',
        'prepared_by_date',
        'reviewed_by',
        'reviewed_by_date',
        'approved_by',
        'approved_by_date',
        'requested_by_date',
        'received_by',
        'received_by_date',
    ];

    protected $casts = [
        'rfp_date' => 'date',
        'amount' => 'decimal:2',
        'prepared_by_date' => 'date',
        'reviewed_by_date' => 'date',
        'approved_by_date' => 'date',
        'requested_by_date' => 'date',
        'received_by_date' => 'date',
    ];

    public function account(): BelongsTo
    {
        return $this->belongsTo(Account::class);
    }

    public function user(): BelongsTo
    {
        return $this->belongsTo(User::class);
    }

    public function details(): HasMany
    {
        return $this->hasMany(RfpDetail::class);
    }

    public function cashCheckVoucher(): HasOne
    {
        return $this->hasOne(CashCheckVoucher::class);
    }
}