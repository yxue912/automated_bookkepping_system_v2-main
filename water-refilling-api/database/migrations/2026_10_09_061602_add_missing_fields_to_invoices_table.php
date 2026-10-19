<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('invoices', function (Blueprint $table) {
            $table->string('invoice_number')->unique();
            $table->date('invoice_date');
            $table->string('currency', 10)->default('PHP');
            $table->string('customer_name');
            $table->text('customer_address')->nullable();
            $table->string('tax_identification_number')->nullable();
            $table->string('prepared_by')->nullable();
            $table->date('prepared_date')->nullable();
            $table->string('reviewed_by')->nullable();
            $table->date('reviewed_date')->nullable();
            $table->string('approved_by')->nullable();
            $table->date('approved_date')->nullable();
        });

        Schema::create('invoice_items', function (Blueprint $table) {
            $table->id();
            $table->foreignId('invoice_id')
                ->constrained('invoices')
                ->cascadeOnDelete();
            $table->text('nature_of_service');
            $table->decimal('quantity', 15, 2)->default(1);
            $table->decimal('unit_cost_price', 15, 2);
            $table->decimal('amount', 15, 2);
            $table->timestamps();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('invoice_items');

        Schema::table('invoices', function (Blueprint $table) {
            $table->dropUnique(['invoice_number']);
            $table->dropColumn([
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
            ]);
        });
    }
};