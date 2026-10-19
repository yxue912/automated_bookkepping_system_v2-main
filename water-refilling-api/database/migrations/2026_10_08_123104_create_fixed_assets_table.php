<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('fixed_assets', function (Blueprint $table) {
            $table->id();

            $table->string('asset_code', 20)->unique();
            $table->string('name', 150);
            $table->string('category', 100)->nullable();

            $table->date('acquisition_date')->nullable();

            $table->decimal('acquisition_cost', 15, 2)->default(0);
            $table->decimal('useful_life_years', 8, 2)->nullable();
            $table->decimal('salvage_value', 15, 2)->default(0);

            $table->string('location', 150)->nullable();

            $table->foreignId('account_id')
                ->nullable()
                ->constrained('accounts')
                ->nullOnDelete();

            $table->text('description')->nullable();

            $table->boolean('is_active')->default(true);

            $table->timestamps();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('fixed_assets');
    }
};