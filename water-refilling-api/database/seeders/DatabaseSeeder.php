<?php

namespace Database\Seeders;

use App\Models\User;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\Hash;
use Spatie\Permission\Models\Role;
use Spatie\Permission\Models\Permission;

class DatabaseSeeder extends Seeder
{
    public function run(): void
    {
        $this->call(AccountSeeder::class);
        /*
        |--------------------------------------------------------------------------
        | ROLES
        |--------------------------------------------------------------------------
        */

        $adminRole = Role::firstOrCreate([
            'name' => 'Admin',
            'guard_name' => 'web',
        ]);

        $ownerRole = Role::firstOrCreate([
            'name' => 'Owner',
            'guard_name' => 'web',
        ]);

        $staffRole = Role::firstOrCreate([
            'name' => 'Staff',
            'guard_name' => 'web',
        ]);


        /*
        |--------------------------------------------------------------------------
        | ADMIN USER
        |--------------------------------------------------------------------------
        */

        $adminEmail = env('AQUA_ARC_ADMIN_EMAIL');
        $adminPassword = env('AQUA_ARC_ADMIN_PASSWORD');
        $admin = null;

        if (filled($adminEmail) && filled($adminPassword)) {
            if (strlen($adminPassword) < 12) {
                throw new \InvalidArgumentException(
                    'AQUA_ARC_ADMIN_PASSWORD must be at least 12 characters.'
                );
            }

            $admin = User::firstOrCreate(
                ['email' => $adminEmail],
                [
                    'name' => env('AQUA_ARC_ADMIN_NAME', 'Administrator'),
                    'password' => Hash::make($adminPassword),
                ]
            );

            $admin->assignRole($adminRole);
        }


        /*
        |--------------------------------------------------------------------------
        | PERMISSIONS
        |--------------------------------------------------------------------------
        */

        $permissions = [

            // Dashboard
            'dashboard.view',

            // Users
            'users.view',
            'users.create',
            'users.edit',
            'users.delete',
            'users.manage',

            // Roles
            'roles.view',
            'roles.create',
            'roles.edit',
            'roles.delete',
            'roles.manage',

            // Permissions
            'permissions.view',
            'permissions.create',
            'permissions.edit',
            'permissions.delete',
            'permissions.manage',

            // Password
            'password.change',
            'password.reset',

            // Audit Logs
            'audit_logs.view',
            'audit_logs.export',
            'audit_logs.manage',

            // Organization
            'organization.view',
            'organization.edit',

            // Fiscal Year
            'fiscal_year.view',
            'fiscal_year.create',
            'fiscal_year.edit',
            'fiscal_year.close',

            // Accounting Periods
            'accounting_periods.view',
            'accounting_periods.create',
            'accounting_periods.edit',
            'accounting_periods.close',
            'accounting_periods.reopen',

            // Currency
            'currency.view',
            'currency.edit',

            // System Settings
            'system_settings.view',
            'system_settings.edit',

            // Chart of Accounts
            'accounts.view',
            'accounts.create',
            'accounts.edit',
            'accounts.delete',
            'accounts.manage',

            // Customers
            'customers.view',
            'customers.create',
            'customers.edit',
            'customers.delete',
            'customers.manage',

            // Vendors / Payees
            'vendors.view',
            'vendors.create',
            'vendors.edit',
            'vendors.delete',
            'vendors.manage',

            // Employees
            'employees.view',
            'employees.create',
            'employees.edit',
            'employees.delete',
            'employees.manage',

            // Departments / Programs
            'departments.view',
            'departments.create',
            'departments.edit',
            'departments.delete',
            'departments.manage',

            // Funds
            'funds.view',
            'funds.create',
            'funds.edit',
            'funds.delete',
            'funds.manage',

            // Fixed Assets
            'fixed_assets.view',
            'fixed_assets.create',
            'fixed_assets.edit',
            'fixed_assets.delete',
            'fixed_assets.depreciate',
            'fixed_assets.dispose',

            // Cash Receipts Book
            'cash_receipts.view',
            'cash_receipts.create',
            'cash_receipts.edit',
            'cash_receipts.delete',
            'cash_receipts.submit',
            'cash_receipts.approve',
            'cash_receipts.post',

            // Cash Disbursements Book
            'cash_disbursements.view',
            'cash_disbursements.create',
            'cash_disbursements.edit',
            'cash_disbursements.delete',
            'cash_disbursements.submit',
            'cash_disbursements.approve',
            'cash_disbursements.post',

            // RFP
            'rfp.view',
            'rfp.create',
            'rfp.edit',
            'rfp.delete',
            'rfp.submit',
            'rfp.approve',
            'rfp.reject',
            'rfp.pay',
            'rfp.post',

            // Cash / Check Vouchers
            'vouchers.view',
            'vouchers.create',
            'vouchers.edit',
            'vouchers.delete',
            'vouchers.submit',
            'vouchers.approve',
            'vouchers.post',

            // Invoices
            'invoices.view',
            'invoices.create',
            'invoices.edit',
            'invoices.delete',
            'invoices.submit',
            'invoices.approve',
            'invoices.post',

            // Journal Vouchers
            'journal_vouchers.view',
            'journal_vouchers.create',
            'journal_vouchers.edit',
            'journal_vouchers.delete',
            'journal_vouchers.submit',
            'journal_vouchers.approve',
            'journal_vouchers.post',

            // Payroll
            'payroll.view',
            'payroll.create',
            'payroll.edit',
            'payroll.delete',
            'payroll.submit',
            'payroll.approve',
            'payroll.post',

            // Approval Workflow
            'approvals.view',
            'approvals.review',
            'approvals.approve',
            'approvals.reject',
            'approvals.pay',

            // Accounting Books
            'journal.view',
            'journal.export',
            'ledger.view',
            'ledger.export',
            'trial_balance.view',
            'trial_balance.export',

            // Financial Reports
            'financial_reports.view',
            'financial_reports.export',
            'financial_reports.print',

            // Documents
            'documents.view',
            'documents.upload',
            'documents.download',
            'documents.delete',
            'documents.manage',

            // Reports Center
            'reports.view',
            'reports.search',
            'reports.export',
            'reports.print',

            // Database Backup
            'database_backup.view',
            'database_backup.create',
            'database_backup.restore',
            'database_backup.manage',

            // System Maintenance
            'system_maintenance.view',
            'system_maintenance.manage',
        ];


        /*
        |--------------------------------------------------------------------------
        | CREATE PERMISSIONS
        |--------------------------------------------------------------------------
        */

        foreach ($permissions as $permission) {
            Permission::firstOrCreate([
                'name' => $permission,
                'guard_name' => 'web',
            ]);
        }


        /*
        |--------------------------------------------------------------------------
        | ADMIN PERMISSIONS
        |--------------------------------------------------------------------------
        */

        if ($admin) {
    $admin->syncPermissions([]);
}

/*
|--------------------------------------------------------------------------
| OWNER PERMISSIONS
|--------------------------------------------------------------------------
*/

$ownerPermissions = Permission::whereNotIn('name', [

    // Role administration
    'roles.view',
    'roles.create',
    'roles.edit',
    'roles.delete',
    'roles.manage',

    // Permission administration
    'permissions.view',
    'permissions.create',
    'permissions.edit',
    'permissions.delete',
    'permissions.manage',

    // Security administration
    'audit_logs.manage',

    // Database administration
    'database_backup.restore',
    'database_backup.manage',

    // System administration
    'system_maintenance.manage',

])->where('guard_name', 'web')->get();

$ownerRole->syncPermissions($ownerPermissions);



        /*
        |--------------------------------------------------------------------------
        | STAFF PERMISSIONS
        |--------------------------------------------------------------------------
        */

        $staffPermissions = Permission::whereIn('name', [

            // Dashboard
            'dashboard.view',

            // Chart of Accounts — allow Staff to select accounts in transaction forms
            'accounts.view',

            // Customers
            'customers.view',
            'customers.create',
            'customers.edit',

            // Vendors
            'vendors.view',

            // Employees
            'employees.view',

            // Cash Receipts
            'cash_receipts.view',
            'cash_receipts.create',
            'cash_receipts.edit',
            'cash_receipts.submit',
            'cash_receipts.post',

            // Cash Disbursements
            'cash_disbursements.view',
            'cash_disbursements.create',
            'cash_disbursements.edit',
            'cash_disbursements.submit',
            'cash_disbursements.post',

            // RFP
            'rfp.view',
            'rfp.create',
            'rfp.edit',
            'rfp.submit',

            // Vouchers
            'vouchers.view',
            'vouchers.create',
            'vouchers.edit',
            'vouchers.submit',

            // Invoices
            'invoices.view',
            'invoices.create',
            'invoices.edit',

            // Journal Vouchers
            'journal_vouchers.view',
            'journal_vouchers.create',
            'journal_vouchers.edit',

            // Documents
            'documents.view',
            'documents.upload',
            'documents.download',

            // Reports
            'reports.view',
            'reports.search',

            // Password
            'password.change',

        ])->where('guard_name', 'web')->get();

        $staffRole->syncPermissions($staffPermissions);

    }
}
