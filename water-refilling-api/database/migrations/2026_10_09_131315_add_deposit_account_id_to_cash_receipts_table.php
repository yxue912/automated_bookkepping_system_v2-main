<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('cash_receipts', function (Blueprint $table) {
            $table->foreignId('deposit_account_id')
                ->nullable()
                ->after('account_id')
                ->constrained('accounts')
                ->restrictOnDelete();
        });
    }

    public function down(): void
    {
        Schema::table('cash_receipts', function (Blueprint $table) {
            $table->dropForeign(['deposit_account_id']);
            $table->dropColumn('deposit_account_id');
        });
    }
};