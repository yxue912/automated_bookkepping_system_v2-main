<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('cash_check_vouchers', function (Blueprint $table) {
            $table->id();

            $table->string('voucher_number', 50)->unique();

            $table->foreignId('rfp_id')
                ->constrained('rfps')
                ->restrictOnDelete();

            $table->date('voucher_date');
            $table->string('payment_method', 20);
            $table->string('check_number', 100)->nullable();
            $table->date('check_date')->nullable();
            $table->string('bank_name', 150)->nullable();

            $table->string('payee', 150);
            $table->decimal('amount', 15, 2);
            $table->string('currency', 10)->default('PHP');

            $table->string('status', 30)->default('Draft');
            $table->text('remarks')->nullable();

            $table->string('prepared_by', 150)->nullable();
            $table->date('prepared_date')->nullable();
            $table->string('approved_by', 150)->nullable();
            $table->date('approved_date')->nullable();
            $table->string('released_by', 150)->nullable();
            $table->date('released_date')->nullable();
            $table->string('received_by', 150)->nullable();
            $table->date('received_date')->nullable();

            $table->timestamps();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('cash_check_vouchers');
    }
};