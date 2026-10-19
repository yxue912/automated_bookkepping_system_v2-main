<?php

use Illuminate\Support\Facades\Route;
use App\Http\Controllers\Api\AuthController;
use App\Http\Controllers\Api\PendingUserController;
use App\Http\Controllers\Api\DashboardController;
use App\Http\Controllers\Api\CustomerController;
use App\Http\Controllers\Api\VendorController;
use App\Http\Controllers\Api\EmployeeController;
use App\Http\Controllers\Api\DepartmentController;
use App\Http\Controllers\Api\FundController;
use App\Http\Controllers\Api\FixedAssetController;
use App\Http\Controllers\Api\CashReceiptController;
use App\Http\Controllers\Api\CashDisbursementController;
use App\Http\Controllers\Api\RfpController;
use App\Http\Controllers\Api\InvoiceController;
use App\Http\Controllers\Api\CashCheckVoucherController;
use App\Http\Controllers\Api\JournalEntryController;
use App\Http\Controllers\Api\GeneralLedgerController;
use App\Http\Controllers\Api\TrialBalanceController;
use App\Http\Controllers\Api\AccountController;
use App\Http\Controllers\Api\LapsingScheduleController;

/*
|--------------------------------------------------------------------------
| Public Authentication Routes
|--------------------------------------------------------------------------
*/

Route::post('/login', [AuthController::class, 'login'])
    ->middleware('throttle:5,1');

Route::post('/register', [AuthController::class, 'register'])
    ->middleware('throttle:5,1');

/*
|--------------------------------------------------------------------------
| Protected API Routes
|--------------------------------------------------------------------------
*/

Route::middleware('auth:sanctum')->group(function () {

    // Authentication
    Route::post('/logout', [AuthController::class, 'logout']);

    /*
    |--------------------------------------------------------------------------
    | Pending Account Approval — Owner Only
    |--------------------------------------------------------------------------
    */

    Route::middleware(['role:Owner', 'permission:users.manage'])->group(function () {
        Route::get('/pending-users', [PendingUserController::class, 'index']);

        Route::post(
            '/pending-users/{user}/approve',
            [PendingUserController::class, 'approve']
        );
    });

    /*
    |--------------------------------------------------------------------------
    | Dashboard
    |--------------------------------------------------------------------------
    */

    Route::get('/dashboard', [DashboardController::class, 'index'])
        ->middleware('permission:dashboard.view');

    /*
    |--------------------------------------------------------------------------
    | Customers
    |--------------------------------------------------------------------------
    */

    Route::get('/customers', [CustomerController::class, 'index'])
        ->middleware('permission:customers.view');

    Route::post('/customers', [CustomerController::class, 'store'])
        ->middleware('permission:customers.create');

    Route::get('/customers/{customer}', [CustomerController::class, 'show'])
        ->middleware('permission:customers.view');

    Route::put('/customers/{customer}', [CustomerController::class, 'update'])
        ->middleware('permission:customers.edit');

    Route::delete('/customers/{customer}', [CustomerController::class, 'destroy'])
        ->middleware('permission:customers.delete');

    /*
    |--------------------------------------------------------------------------
    | Vendors / Payees
    |--------------------------------------------------------------------------
    */

    Route::get('/vendors', [VendorController::class, 'index'])
        ->middleware('permission:vendors.view');

    Route::post('/vendors', [VendorController::class, 'store'])
        ->middleware('permission:vendors.create');

    Route::get('/vendors/{vendor}', [VendorController::class, 'show'])
        ->middleware('permission:vendors.view');

    Route::put('/vendors/{vendor}', [VendorController::class, 'update'])
        ->middleware('permission:vendors.edit');

    Route::delete('/vendors/{vendor}', [VendorController::class, 'destroy'])
        ->middleware('permission:vendors.delete');

    /*
    |--------------------------------------------------------------------------
    | Employees
    |--------------------------------------------------------------------------
    */

    Route::get('/employees', [EmployeeController::class, 'index'])
        ->middleware('permission:employees.view');

    Route::post('/employees', [EmployeeController::class, 'store'])
        ->middleware('permission:employees.create');

    Route::get('/employees/{employee}', [EmployeeController::class, 'show'])
        ->middleware('permission:employees.view');

    Route::put('/employees/{employee}', [EmployeeController::class, 'update'])
        ->middleware('permission:employees.edit');

    Route::delete('/employees/{employee}', [EmployeeController::class, 'destroy'])
        ->middleware('permission:employees.delete');

    /*
    |--------------------------------------------------------------------------
    | Departments / Programs
    |--------------------------------------------------------------------------
    */

    Route::get('/departments', [DepartmentController::class, 'index'])
        ->middleware('permission:departments.view');

    Route::post('/departments', [DepartmentController::class, 'store'])
        ->middleware('permission:departments.create');

    Route::get('/departments/{department}', [DepartmentController::class, 'show'])
        ->middleware('permission:departments.view');

    Route::put('/departments/{department}', [DepartmentController::class, 'update'])
        ->middleware('permission:departments.edit');

    Route::delete('/departments/{department}', [DepartmentController::class, 'destroy'])
        ->middleware('permission:departments.delete');

    /*
    |--------------------------------------------------------------------------
    | Funds
    |--------------------------------------------------------------------------
    */

    Route::get('/funds', [FundController::class, 'index'])
        ->middleware('permission:funds.view');

    Route::post('/funds', [FundController::class, 'store'])
        ->middleware('permission:funds.create');

    Route::get('/funds/{fund}', [FundController::class, 'show'])
        ->middleware('permission:funds.view');

    Route::put('/funds/{fund}', [FundController::class, 'update'])
        ->middleware('permission:funds.edit');

    Route::delete('/funds/{fund}', [FundController::class, 'destroy'])
        ->middleware('permission:funds.delete');

    /*
    |--------------------------------------------------------------------------
    | Fixed Assets
    |--------------------------------------------------------------------------
    */

    Route::get('/fixed-assets', [FixedAssetController::class, 'index'])
        ->middleware('permission:fixed_assets.view');

    Route::post('/fixed-assets', [FixedAssetController::class, 'store'])
        ->middleware('permission:fixed_assets.create');

    Route::get('/fixed-assets/{fixedAsset}', [FixedAssetController::class, 'show'])
        ->middleware('permission:fixed_assets.view');

    Route::put('/fixed-assets/{fixedAsset}', [FixedAssetController::class, 'update'])
        ->middleware('permission:fixed_assets.edit');

    Route::delete('/fixed-assets/{fixedAsset}', [FixedAssetController::class, 'destroy'])
        ->middleware('permission:fixed_assets.delete');

    /*
    |--------------------------------------------------------------------------
    | Cash Receipts Book
    |--------------------------------------------------------------------------
    */

    Route::get('/cash-receipts', [CashReceiptController::class, 'index'])
        ->middleware('permission:cash_receipts.view');

    Route::post('/cash-receipts', [CashReceiptController::class, 'store'])
        ->middleware('permission:cash_receipts.create');

    Route::post('/cash-receipts/{cashReceipt}/post', [CashReceiptController::class, 'post'])
        ->middleware('permission:cash_receipts.post');

    Route::get('/cash-receipts/{cashReceipt}', [CashReceiptController::class, 'show'])
        ->middleware('permission:cash_receipts.view');

    Route::put('/cash-receipts/{cashReceipt}', [CashReceiptController::class, 'update'])
        ->middleware('permission:cash_receipts.edit');

    Route::delete('/cash-receipts/{cashReceipt}', [CashReceiptController::class, 'destroy'])
        ->middleware('permission:cash_receipts.delete');

    /*
    |--------------------------------------------------------------------------
    | Cash Disbursements Book
    |--------------------------------------------------------------------------
    */

    Route::get('/cash-disbursements', [CashDisbursementController::class, 'index'])
        ->middleware('permission:cash_disbursements.view');

    Route::post('/cash-disbursements', [CashDisbursementController::class, 'store'])
        ->middleware('permission:cash_disbursements.create');

    Route::post('/cash-disbursements/{cashDisbursement}/post', [CashDisbursementController::class, 'post'])
        ->middleware('permission:cash_disbursements.post');

    Route::get('/cash-disbursements/{cashDisbursement}', [CashDisbursementController::class, 'show'])
        ->middleware('permission:cash_disbursements.view');

    Route::put('/cash-disbursements/{cashDisbursement}', [CashDisbursementController::class, 'update'])
        ->middleware('permission:cash_disbursements.edit');

    Route::delete('/cash-disbursements/{cashDisbursement}', [CashDisbursementController::class, 'destroy'])
        ->middleware('permission:cash_disbursements.delete');

    /*
    |--------------------------------------------------------------------------
    | Request for Payment (RFP)
    |--------------------------------------------------------------------------
    */

    Route::get('/rfps', [RfpController::class, 'index'])
        ->middleware('permission:rfp.view');

    Route::post('/rfps', [RfpController::class, 'store'])
        ->middleware('permission:rfp.create');

    Route::post('/rfps/{rfp}/submit', [RfpController::class, 'submit'])
        ->middleware('permission:rfp.submit');

    Route::post('/rfps/{rfp}/review', [RfpController::class, 'review'])
        ->middleware('permission:approvals.review');

    Route::post('/rfps/{rfp}/approve', [RfpController::class, 'approve'])
        ->middleware('permission:rfp.approve');

    Route::post('/rfps/{rfp}/reject', [RfpController::class, 'reject'])
        ->middleware('permission:rfp.reject');

    Route::get('/rfps/{rfp}', [RfpController::class, 'show'])
        ->middleware('permission:rfp.view');

    Route::put('/rfps/{rfp}', [RfpController::class, 'update'])
        ->middleware('permission:rfp.edit');

    Route::delete('/rfps/{rfp}', [RfpController::class, 'destroy'])
        ->middleware('permission:rfp.delete');

    /*
    |--------------------------------------------------------------------------
    | Invoices
    |--------------------------------------------------------------------------
    */

    Route::get('/invoices', [InvoiceController::class, 'index'])
        ->middleware('permission:invoices.view');

    Route::post('/invoices', [InvoiceController::class, 'store'])
        ->middleware('permission:invoices.create');

    Route::get('/invoices/{invoice}', [InvoiceController::class, 'show'])
        ->middleware('permission:invoices.view');

    Route::put('/invoices/{invoice}', [InvoiceController::class, 'update'])
        ->middleware('permission:invoices.edit');

    Route::delete('/invoices/{invoice}', [InvoiceController::class, 'destroy'])
        ->middleware('permission:invoices.delete');

    /*
    |--------------------------------------------------------------------------
    | Cash / Check Vouchers
    |--------------------------------------------------------------------------
    */

    Route::get('/cash-check-vouchers', [CashCheckVoucherController::class, 'index'])
        ->middleware('permission:vouchers.view');

    Route::post('/cash-check-vouchers', [CashCheckVoucherController::class, 'store'])
        ->middleware('permission:vouchers.create');

    Route::post('/cash-check-vouchers/{cashCheckVoucher}/approve', [CashCheckVoucherController::class, 'approve'])
        ->middleware('permission:vouchers.approve');

    Route::post('/cash-check-vouchers/{cashCheckVoucher}/release', [CashCheckVoucherController::class, 'release'])
        ->middleware('permission:vouchers.post');

    Route::post('/cash-check-vouchers/{cashCheckVoucher}/pay', [CashCheckVoucherController::class, 'pay'])
        ->middleware('permission:rfp.pay');

    Route::get('/cash-check-vouchers/{cashCheckVoucher}', [CashCheckVoucherController::class, 'show'])
        ->middleware('permission:vouchers.view');

    Route::put('/cash-check-vouchers/{cashCheckVoucher}', [CashCheckVoucherController::class, 'update'])
        ->middleware('permission:vouchers.edit');

    Route::delete('/cash-check-vouchers/{cashCheckVoucher}', [CashCheckVoucherController::class, 'destroy'])
        ->middleware('permission:vouchers.delete');

    /*
    |--------------------------------------------------------------------------
    | Journal Entries
    |--------------------------------------------------------------------------
    */

    Route::get('/journal-entries', [JournalEntryController::class, 'index'])
        ->middleware('permission:journal.view');

    Route::get('/journal-entries/{journalEntry}', [JournalEntryController::class, 'show'])
        ->middleware('permission:journal.view');

    Route::post('/journal-entries', [JournalEntryController::class, 'store'])
        ->middleware('permission:journal_vouchers.create');

    Route::post('/journal-entries/{journalEntry}/post', [JournalEntryController::class, 'post'])
        ->middleware('permission:journal_vouchers.post');

    /*
    |--------------------------------------------------------------------------
    | Accounting Reports
    |--------------------------------------------------------------------------
    */

    Route::get('/general-ledger', [GeneralLedgerController::class, 'index'])
        ->middleware('permission:ledger.view');

    Route::get('/trial-balance', [TrialBalanceController::class, 'index'])
        ->middleware('permission:trial_balance.view');

    /*
    |--------------------------------------------------------------------------
    | Chart of Accounts
    |--------------------------------------------------------------------------
    */

    Route::get('/accounts', [AccountController::class, 'index'])
        ->middleware('permission:accounts.view');

    Route::post('/accounts', [AccountController::class, 'store'])
        ->middleware('permission:accounts.create');

    Route::get('/accounts/{account}', [AccountController::class, 'show'])
        ->middleware('permission:accounts.view');

    Route::put('/accounts/{account}', [AccountController::class, 'update'])
        ->middleware('permission:accounts.edit');

    Route::patch('/accounts/{account}', [AccountController::class, 'update'])
        ->middleware('permission:accounts.edit');

    Route::delete('/accounts/{account}', [AccountController::class, 'destroy'])
        ->middleware('permission:accounts.delete');

    /*
    |--------------------------------------------------------------------------
    | Lapsing Schedule Report
    |--------------------------------------------------------------------------
    */

    Route::get('/reports/lapsing-schedule', [LapsingScheduleController::class, 'index'])
        ->middleware('permission:financial_reports.view');
});