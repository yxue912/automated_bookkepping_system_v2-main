
<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('rfps', function (Blueprint $table) {
            $table->id();
            $table->string('rfp_number', 30)->unique();
            $table->date('rfp_date');
            $table->string('requested_by', 150);
            $table->string('payee', 150);
            $table->text('purpose');
            $table->decimal('amount', 15, 2);
            $table->string('status', 30)->default('Draft');
            $table->text('remarks')->nullable();
            $table->foreignId('account_id')
                ->nullable()
                ->constrained('accounts')
                ->restrictOnDelete();
            $table->foreignId('user_id')
                ->nullable()
                ->constrained('users')
                ->nullOnDelete();
            $table->timestamps();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('rfps');
    }
};