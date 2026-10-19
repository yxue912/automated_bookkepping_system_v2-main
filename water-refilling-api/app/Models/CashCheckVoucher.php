<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class CashCheckVoucher extends Model
{
    protected $fillable = [
        'voucher_number',
        'rfp_id',
        'voucher_date',
        'payment_method',
        'check_number',
        'check_date',
        'bank_name',
        'payee',
        'amount',
        'currency',
        'status',
        'remarks',
        'prepared_by',
        'prepared_by_user_id',
        'prepared_date',
        'approved_by',
        'approved_date',
        'released_by',
        'released_date',
        'received_by',
        'received_date',
    ];

    protected $casts = [
        'voucher_date' => 'date',
        'check_date' => 'date',
        'amount' => 'decimal:2',
        'prepared_date' => 'date',
        'approved_date' => 'date',
        'released_date' => 'date',
        'received_date' => 'date',
    ];

    public function rfp(): BelongsTo
    {
        return $this->belongsTo(Rfp::class);
    }
}
