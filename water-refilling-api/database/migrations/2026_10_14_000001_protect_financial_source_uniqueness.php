<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        $duplicateVoucher = DB::table('cash_check_vouchers')->select('rfp_id')
            ->groupBy('rfp_id')->havingRaw('COUNT(*) > 1')->first();
        if ($duplicateVoucher) {
            throw new RuntimeException('Duplicate vouchers for one RFP require manual reconciliation before this migration. No history was changed.');
        }
        Schema::table('cash_check_vouchers', function (Blueprint $table) {
            $table->unique('rfp_id', 'cash_check_vouchers_rfp_id_unique');
        });
    }

    public function down(): void
    {
        Schema::table('cash_check_vouchers', fn (Blueprint $table) => $table->dropUnique('cash_check_vouchers_rfp_id_unique'));
    }
};
