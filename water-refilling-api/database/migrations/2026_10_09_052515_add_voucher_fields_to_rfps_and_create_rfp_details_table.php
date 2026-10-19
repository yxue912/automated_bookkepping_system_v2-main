
<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('rfps', function (Blueprint $table) {
            $table->string('reference_invoice_no', 100)->nullable();
            $table->string('tax_identification_number', 30)->nullable();
            $table->string('currency', 10)->default('PHP');

            $table->string('prepared_by', 150)->nullable();
            $table->date('prepared_by_date')->nullable();

            $table->string('reviewed_by', 150)->nullable();
            $table->date('reviewed_by_date')->nullable();

            $table->string('approved_by', 150)->nullable();
            $table->date('approved_by_date')->nullable();

            $table->date('requested_by_date')->nullable();

            $table->string('received_by', 150)->nullable();
            $table->date('received_by_date')->nullable();
        });

        Schema::create('rfp_details', function (Blueprint $table) {
            $table->id();

            $table->foreignId('rfp_id')
                ->constrained('rfps')
                ->cascadeOnDelete();

            $table->foreignId('account_id')
                ->nullable()
                ->constrained('accounts')
                ->restrictOnDelete();

            $table->string('account_code', 30)->nullable();
            $table->string('account_title', 150)->nullable();
            $table->string('location_project', 150)->nullable();

            $table->decimal('debit_amount', 15, 2)->default(0);
            $table->decimal('credit_amount', 15, 2)->default(0);

            $table->timestamps();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('rfp_details');

        Schema::table('rfps', function (Blueprint $table) {
            $table->dropColumn([
                'reference_invoice_no',
                'tax_identification_number',
                'currency',
                'prepared_by',
                'prepared_by_date',
                'reviewed_by',
                'reviewed_by_date',
                'approved_by',
                'approved_by_date',
                'requested_by_date',
                'received_by',
                'received_by_date',
            ]);
        });
    }
};