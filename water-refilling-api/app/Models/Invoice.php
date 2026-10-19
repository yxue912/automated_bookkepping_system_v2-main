<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;

class Invoice extends Model
{
    protected $fillable = [
        'invoice_number',
        'invoice_date',
        'currency',
        'customer_name',
        'customer_address',
        'tax_identification_number',
        'prepared_by',
        'prepared_date',
        'reviewed_by',
        'reviewed_date',
        'approved_by',
        'approved_date',
    ];

    protected function casts(): array
    {
        return [
            'invoice_date' => 'date',
            'prepared_date' => 'date',
            'reviewed_date' => 'date',
            'approved_date' => 'date',
        ];
    }

    public function items(): HasMany
    {
        return $this->hasMany(InvoiceItem::class);
    }
}