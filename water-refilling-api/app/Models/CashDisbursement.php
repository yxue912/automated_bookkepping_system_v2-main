<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class CashDisbursement extends Model
{
    protected $fillable = [
        'created_by',
        'disbursement_date',
        'voucher_number',
        'payee',
        'account_id',
        'payment_account_id',
        'amount',
        'payment_method',
        'check_number',
        'remarks',
        'status',
    ];

    protected $casts = [
        'disbursement_date' => 'date',
        'amount' => 'decimal:2',
    ];

    public function account(): BelongsTo
    {
        return $this->belongsTo(Account::class);
    }

    public function paymentAccount(): BelongsTo
    {
        return $this->belongsTo(Account::class, 'payment_account_id');
    }
}
