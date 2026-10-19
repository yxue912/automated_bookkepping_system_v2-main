<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('accounts', function (Blueprint $table) {
            $table->id();

            $table->string('account_code', 20)->unique();
            $table->string('account_name', 150);

            $table->enum('account_type', [
                'Asset',
                'Liability',
                'Equity',
                'Revenue',
                'Expense',
            ]);

            $table->string('account_group', 100)->nullable();

            $table->text('description')->nullable();

            $table->boolean('is_active')->default(true);

            $table->timestamps();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('accounts');
    }
};