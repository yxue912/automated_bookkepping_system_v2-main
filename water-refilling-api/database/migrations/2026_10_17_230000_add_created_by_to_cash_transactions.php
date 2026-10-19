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
            $table->foreignId('created_by')->nullable()->after('id')
                ->constrained('users')->nullOnDelete();
        });

        Schema::table('cash_disbursements', function (Blueprint $table) {
            $table->foreignId('created_by')->nullable()->after('id')
                ->constrained('users')->nullOnDelete();
        });

        // Preserve existing data and recover ownership from linked journal entries where possible.
        foreach (DB::table('cash_receipts')->select('id')->get() as $record) {
            $entry = DB::table('journal_entries')
                ->where('reference_type', 'CashReceipt')
                ->where('reference_id', $record->id)
                ->whereNotNull('created_by')
                ->orderBy('id')
                ->first();

            if ($entry) {
                DB::table('cash_receipts')->where('id', $record->id)
                    ->update(['created_by' => $entry->created_by]);
            }
        }

        foreach (DB::table('cash_disbursements')->select('id')->get() as $record) {
            $entry = DB::table('journal_entries')
                ->where('reference_type', 'CashDisbursement')
                ->where('reference_id', $record->id)
                ->whereNotNull('created_by')
                ->orderBy('id')
                ->first();

            if ($entry) {
                DB::table('cash_disbursements')->where('id', $record->id)
                    ->update(['created_by' => $entry->created_by]);
            }
        }
    }

    public function down(): void
    {
        Schema::table('cash_receipts', function (Blueprint $table) {
            $table->dropConstrainedForeignId('created_by');
        });

        Schema::table('cash_disbursements', function (Blueprint $table) {
            $table->dropConstrainedForeignId('created_by');
        });
    }
};
