<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('cash_check_vouchers', function (Blueprint $table) {
            $table->foreignId('prepared_by_user_id')->nullable()->constrained('users')->nullOnDelete();
        });
    }

    public function down(): void
    {
        Schema::table('cash_check_vouchers', fn (Blueprint $table) => $table->dropConstrainedForeignId('prepared_by_user_id'));
    }
};
