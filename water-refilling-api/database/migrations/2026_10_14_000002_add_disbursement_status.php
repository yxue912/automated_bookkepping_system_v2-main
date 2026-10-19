<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        $invalid = DB::table('cash_disbursements as disbursement')
            ->leftJoin('journal_entries as journal', function ($join) {
                $join->on('journal.reference_id', '=', 'disbursement.id')
                    ->where('journal.reference_type', '=', 'CashDisbursement');
            })
            ->select('disbursement.id')
            ->groupBy('disbursement.id')
            ->havingRaw('COUNT(journal.id) <> 1 OR SUM(CASE WHEN journal.status IN (?, ?) THEN 0 ELSE 1 END) > 0', ['Draft', 'Posted'])
            ->first();
        if ($invalid) {
            throw new RuntimeException("Disbursement {$invalid->id} requires manual journal reconciliation before adding status. No records were changed.");
        }
        Schema::table('cash_disbursements', function (Blueprint $table) {
            $table->string('status', 20)->default('Draft');
        });
        DB::table('cash_disbursements')->whereIn('id', DB::table('journal_entries')
            ->where('reference_type', 'CashDisbursement')->where('status', 'Posted')
            ->select('reference_id'))->update(['status' => 'Posted']);
    }

    public function down(): void
    {
        Schema::table('cash_disbursements', fn (Blueprint $table) => $table->dropColumn('status'));
    }
};
