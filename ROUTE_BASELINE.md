# API route baseline at main f5957ee

The 71 pre-existing API routes are recorded here before the remediation changes. All routes except login are inside `auth:sanctum`; the listed permission is an additional route middleware requirement.

| Method | URI | Controller / action | Permission |
| --- | --- | --- | --- |
| POST | /api/login | closure | public |
| POST | /api/login', function (Request $request) {
    $request->validate([
        'email' => ['required', 'email'],
        'password' => ['required'],
    ]);

    $user = User::where('email', $request->email)->first();

    if (! $user || ! Hash::check($request->password, $user->password)) {
        return response()->json([
            'success' => false,
            'message' => 'Invalid email or password.',
        ], 401);
    }

    $token = $user->createToken('vbnet-desktop')->plainTextToken;

    return response()->json([
        'success' => true,
        'message' => 'Login successful.',
        'token' => $token,
        'token_type' => 'Bearer',
        'user' => [
            'id' => $user->id,
            'name' => $user->name,
            'email' => $user->email,
            'roles' => $user->getRoleNames(),
        ],
    ]);
});

/*
|--------------------------------------------------------------------------
| Protected API Routes
|--------------------------------------------------------------------------
*/

Route::middleware('auth:sanctum')->group(function () {

    /*
    |--------------------------------------------------------------------------
    | Dashboard
    |--------------------------------------------------------------------------
    */


Route::get('/dashboard | DashboardController@index | permission:dashboard.view |
| GET | /api/customers | CustomerController@index | permission:customers.view |
| POST | /api/customers | CustomerController@store | permission:customers.create |
| GET | /api/customers/{customer} | CustomerController@show | permission:customers.view |
| PUT | /api/customers/{customer} | CustomerController@update | permission:customers.edit |
| DELETE | /api/customers/{customer} | CustomerController@destroy | permission:customers.delete |
| GET | /api/vendors | VendorController@index | permission:vendors.view |
| POST | /api/vendors | VendorController@store | permission:vendors.create |
| GET | /api/vendors/{vendor} | VendorController@show | permission:vendors.view |
| PUT | /api/vendors/{vendor} | VendorController@update | permission:vendors.edit |
| DELETE | /api/vendors/{vendor} | VendorController@destroy | permission:vendors.delete |
| GET | /api/employees | EmployeeController@index | permission:employees.view |
| POST | /api/employees | EmployeeController@store | permission:employees.create |
| GET | /api/employees/{employee} | EmployeeController@show | permission:employees.view |
| PUT | /api/employees/{employee} | EmployeeController@update | permission:employees.edit |
| DELETE | /api/employees/{employee} | EmployeeController@destroy | permission:employees.delete |
| GET | /api/departments | DepartmentController@index | permission:departments.view |
| POST | /api/departments | DepartmentController@store | permission:departments.create |
| GET | /api/departments/{department} | DepartmentController@show | permission:departments.view |
| PUT | /api/departments/{department} | DepartmentController@update | permission:departments.edit |
| DELETE | /api/departments/{department} | DepartmentController@destroy | permission:departments.delete |
| GET | /api/funds | FundController@index | permission:funds.view |
| POST | /api/funds | FundController@store | permission:funds.create |
| GET | /api/funds/{fund} | FundController@show | permission:funds.view |
| PUT | /api/funds/{fund} | FundController@update | permission:funds.edit |
| DELETE | /api/funds/{fund} | FundController@destroy | permission:funds.delete |
| GET | /api/fixed-assets | FixedAssetController@index | permission:fixed_assets.view |
| POST | /api/fixed-assets | FixedAssetController@store | permission:fixed_assets.create |
| GET | /api/fixed-assets/{fixedAsset} | FixedAssetController@show | permission:fixed_assets.view |
| PUT | /api/fixed-assets/{fixedAsset} | FixedAssetController@update | permission:fixed_assets.edit |
| DELETE | /api/fixed-assets/{fixedAsset} | FixedAssetController@destroy | permission:fixed_assets.delete |
| GET | /api/cash-receipts | CashReceiptController@index | permission:cash_receipts.view |
| POST | /api/cash-receipts | CashReceiptController@store | permission:cash_receipts.create |
| POST | /api/cash-receipts/{cashReceipt}/post | CashReceiptController@post | permission:cash_receipts.post |
| GET | /api/cash-receipts/{cashReceipt} | CashReceiptController@show | permission:cash_receipts.view |
| PUT | /api/cash-receipts/{cashReceipt} | CashReceiptController@update | permission:cash_receipts.edit |
| DELETE | /api/cash-receipts/{cashReceipt} | CashReceiptController@destroy | permission:cash_receipts.delete |
| GET | /api/cash-disbursements | CashDisbursementController@index | permission:cash_disbursements.view |
| POST | /api/cash-disbursements | CashDisbursementController@store | permission:cash_disbursements.create |
| GET | /api/cash-disbursements/{cashDisbursement} | CashDisbursementController@show | permission:cash_disbursements.view |
| PUT | /api/cash-disbursements/{cashDisbursement} | CashDisbursementController@update | permission:cash_disbursements.edit |
| DELETE | /api/cash-disbursements/{cashDisbursement} | CashDisbursementController@destroy | permission:cash_disbursements.delete |
| GET | /api/rfps | RfpController@index | permission:rfp.view |
| POST | /api/rfps | RfpController@store | permission:rfp.create |
| GET | /api/rfps/{rfp} | RfpController@show | permission:rfp.view |
| PUT | /api/rfps/{rfp} | RfpController@update | permission:rfp.edit |
| DELETE | /api/rfps/{rfp} | RfpController@destroy | permission:rfp.delete |
| GET | /api/invoices | InvoiceController@index | permission:invoices.view |
| POST | /api/invoices | InvoiceController@store | permission:invoices.create |
| GET | /api/invoices/{invoice} | InvoiceController@show | permission:invoices.view |
| PUT | /api/invoices/{invoice} | InvoiceController@update | permission:invoices.edit |
| DELETE | /api/invoices/{invoice} | InvoiceController@destroy | permission:invoices.delete |
| GET | /api/cash-check-vouchers | CashCheckVoucherController@index | permission:vouchers.view |
| POST | /api/cash-check-vouchers | CashCheckVoucherController@store | permission:vouchers.create |
| GET | /api/cash-check-vouchers/{cashCheckVoucher} | CashCheckVoucherController@show | permission:vouchers.view |
| PUT | /api/cash-check-vouchers/{cashCheckVoucher} | CashCheckVoucherController@update | permission:vouchers.edit |
| DELETE | /api/cash-check-vouchers/{cashCheckVoucher} | CashCheckVoucherController@destroy | permission:vouchers.delete |
| GET | /api/journal-entries | JournalEntryController@index | permission:journal.view |
| GET | /api/journal-entries/{journalEntry} | JournalEntryController@show | permission:journal.view |
| POST | /api/journal-entries | JournalEntryController@store | permission:journal_vouchers.create |
| POST | /api/journal-entries/{journalEntry}/post | JournalEntryController@post | permission:journal_vouchers.post |
| GET | /api/general-ledger | GeneralLedgerController@index | permission:ledger.view |
| GET | /api/trial-balance | TrialBalanceController@index | permission:trial_balance.view |
| GET | /api/accounts | AccountController@index | permission:accounts.view |
| POST | /api/accounts | AccountController@store | permission:accounts.create |
| GET | /api/accounts/{account} | AccountController@show | permission:accounts.view |
| PUT | /api/accounts/{account} | AccountController@update | permission:accounts.edit |
| PATCH | /api/accounts/{account} | AccountController@update | permission:accounts.edit |
| DELETE | /api/accounts/{account} | AccountController@destroy | permission:accounts.delete |
| GET | /api/reports/lapsing-schedule | LapsingScheduleController@index | permission:financial_reports.view |

Desktop calls traced: login, logout, dashboard, accounts, fixed-assets, cash-receipts, cash-disbursements, journal-entries, general-ledger, trial-balance, and reports/lapsing-schedule. RFP, voucher, and invoice routes currently have no corresponding WinForms screen in this checkout.
