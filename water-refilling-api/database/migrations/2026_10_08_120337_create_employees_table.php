<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('employees', function (Blueprint $table) {
            $table->id();

            $table->string('employee_code', 20)->unique();
            $table->string('name', 150);
            $table->string('position', 100)->nullable();
            $table->string('contact_number', 30)->nullable();
            $table->string('email', 150)->nullable();
            $table->string('department', 100)->nullable();
            $table->date('hire_date')->nullable();

            $table->boolean('is_active')->default(true);

            $table->timestamps();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('employees');
    }
};