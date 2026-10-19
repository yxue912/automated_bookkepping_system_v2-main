<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class CashReceipt extends Model
{
    protected $fillable = [
        'created_by',
        'receipt_date',
        'or_number',
        'payor',
        'account_id',
        'deposit_account_id',
        'amount',
        'bank_deposited',
        'remarks',
        'status',
    ];

    protected $casts = [
        'receipt_date' => 'date',
        'amount' => 'decimal:2',
    ];

    public function account(): BelongsTo
    {
        return $this->belongsTo(Account::class);
    }

    public function depositAccount(): BelongsTo
    {
        return $this->belongsTo(Account::class, 'deposit_account_id');
    }
}