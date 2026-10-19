<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('cash_receipts', function (Blueprint $table) {
            $table->string('status', 20)->nullable()->after('remarks');
        });

        $receipts = DB::table('cash_receipts')->get();

        foreach ($receipts as $receipt) {
            $hasPostedEntry = DB::table('journal_entries')
                ->where('reference_type', 'CashReceipt')
                ->where('reference_id', $receipt->id)
                ->where('status', 'Posted')
                ->exists();

            DB::table('cash_receipts')
                ->where('id', $receipt->id)
                ->update([
                    'status' => $hasPostedEntry ? 'Posted' : 'Draft',
                ]);
        }

        Schema::table('cash_receipts', function (Blueprint $table) {
            $table->string('status', 20)->default('Draft')->nullable(false)->change();
        });
    }

    public function down(): void
    {
        Schema::table('cash_receipts', function (Blueprint $table) {
            $table->dropColumn('status');
        });
    }
};