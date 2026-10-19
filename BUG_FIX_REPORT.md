# Bug fix report

Branch: `fix/comprehensive-bug-remediation`. Baseline: `main` at `f5957ee`. Work was performed without changing an existing database, secrets, the WinForms designer, form names, control names, or the .NET target framework.

All 40 numbered audit findings were traced and addressed by the changes or explicit environment limitations below; none was dismissed as wholly non-reproducing. Four additional defects found during implementation were corrected: the missing reliable voucher preparer identity for self-approval checks, Docker's unwritable default SQLite path, draft receipt edits/deletes that could ignore a duplicate posted journal, and namespace/underscore aliases that bypassed direct journal source protection. These counts refer to audit finding IDs, several of which share one root cause.

## Dependency map and route compatibility

Desktop login calls `/api/login`, stores the Sanctum bearer token in `ApiSession`, and passes it to dashboard, account, fixed asset, cash receipt, cash disbursement, journal, ledger, trial balance, and lapsing report calls. API routes apply `auth:sanctum` and route specific Spatie permissions before their controllers. Source controllers create documents and journal entries through Eloquent models, then `JournalEntryService` validates and posts journal lines. Reports read only posted journal entries; the desktop parses their existing JSON fields. The full 71-route pre-change method, URI, action, and permission inventory is in [ROUTE_BASELINE.md](ROUTE_BASELINE.md).

| Desktop form | API path | Permission | Controller → service/model → database → response |
| --- | --- | --- | --- |
| LoginForm | `/api/login` | Public, throttled | AuthController → User/Sanctum tokens → user, roles, permissions, bearer token |
| MainForm | `/api/dashboard`, `/api/reports/lapsing-schedule`, `/api/logout` | `dashboard.view`, `financial_reports.view`, auth | DashboardController/LapsingScheduleController/AuthController → journal or asset models → summary/asset rows or token revocation |
| ChartOfAccountsForm | `/api/accounts` | `accounts.*` | AccountController → Account → accounts table → account JSON |
| FixedAssetsForm | `/api/fixed-assets` | `fixed_assets.*` | FixedAssetController → FixedAsset → fixed_assets table → asset JSON |
| CashReceiptsForm | `/api/cash-receipts` | `cash_receipts.*` | CashReceiptController → JournalEntryService on posting → CashReceipt/JournalEntry/lines → receipt JSON |
| CashDisbursementsForm | `/api/cash-disbursements` | `cash_disbursements.*` | CashDisbursementController → JournalEntryService when explicitly authorized → CashDisbursement/JournalEntry/lines → disbursement JSON |
| JournalEntriesForm | `/api/journal-entries` | `journal.view`, `journal_vouchers.post` | JournalEntryController → JournalEntryService on direct voucher posting → JournalEntry/lines → paginated entry JSON |
| GeneralLedgerForm / TrialBalanceForm | `/api/general-ledger`, `/api/trial-balance` | `ledger.view`, `trial_balance.view` | Report controllers → posted JournalEntry/lines and Account → existing balance fields |

All paths in this table except login first pass through `auth:sanctum`. The desktop parses the listed JSON responses in the same form classes; no designer files changed.

Static route comparison: 71 baseline API method/path pairs; 80 final pairs; all 71 retained; no duplicate method/path pairs. Added `POST /api/logout`, `POST /api/cash-disbursements/{cashDisbursement}/post`, four RFP transitions (`submit`, `review`, `approve`, `reject`), and three cash/check voucher transitions (`approve`, `release`, `pay`). All new protected routes remain inside `auth:sanctum`; the login route remains public with throttling.

## Changed file inventory

| Files | Purpose | Compatibility impact |
| --- | --- | --- |
| `WaterRefilling.App/ApiSession.vb`, `LoginForm.vb`, `MainForm.vb`, `CashDisbursementsForm.vb`, `JournalEntriesForm.vb` | Shared API address, session handling, effective permissions, explicit posting intent, complete paged journal access | Existing form/control names, navigation layout, and default URL retained; no designer file changed |
| `water-refilling-api/routes/api.php`, `app/Http/Controllers/Api/AuthController.php`, `app/Http/Controllers/Auth/RegisteredUserController.php`, `config/auth.php`, `.env.example` | Authenticated workflow routes, login/logout/throttle, controlled registration | 71 previous API routes retained; nine protected endpoints added; registration defaults closed |
| `app/Http/Controllers/Api/RfpController.php`, `CashCheckVoucherController.php`, `CashDisbursementController.php`, `CashReceiptController.php`, `JournalEntryController.php`, `app/Services/JournalEntryService.php` | Authorized document transitions and atomic journal/source consistency | Unsafe state or reference mutation now returns 422/403; legitimate draft and direct journal paths retained |
| `app/Http/Controllers/Api/AccountController.php`, `InvoiceController.php`, `FixedAssetController.php`, `LapsingScheduleController.php`, `GeneralLedgerController.php`, `TrialBalanceController.php`, `DashboardController.php` | Historical account safety, invoice totals, depreciation, ledger and as-of reporting | Existing JSON fields retained; optional report fields added; historically unsafe edits rejected |
| `app/Models/CashCheckVoucher.php`, `CashDisbursement.php`, `database/migrations/2026_10_14_000001_protect_financial_source_uniqueness.php`, `2026_10_14_000002_add_disbursement_status.php`, `2026_10_14_000003_add_voucher_preparer_identity.php` | Add one-to-one voucher guard, document state, trusted preparer identity | Additive schema only; legacy conflict checks precede constraints |
| `database/seeders/AccountSeeder.php`, `DatabaseSeeder.php`, `Dockerfile` | Idempotent baseline accounts/permissions and working PDO SQLite deployment | Existing customized accounts and grants retained; Apache port/stack retained |
| `tests/Feature/ApiAuthenticationTest.php`, `Auth/RegistrationTest.php`, `RfpWorkflowTest.php`, `AccountingReportsIntegrityTest.php`, `CashReceiptIntegrityTest.php`, `FinancialWorkflowIntegrityTest.php`, `JournalEntryIntegrityTest.php`, `ValidationIntegrityTest.php` | Targeted regression coverage | No production API contract changes from tests |
| `water-refilling-api/README.md`, `DEPENDENCIES.md`, `LOCAL_SETUP.md`, `ROUTE_BASELINE.md`, `BUG_FIX_REPORT.md` | Setup, route inventory, and repair record | Documentation only |
| Tracked files under `WaterRefilling.App/.vs/`, `WaterRefilling.App/bin/`, `WaterRefilling.App/obj/`, `water-refilling-api/ChartOfAccountsForm/obj/` | Remove generated files from Git index | Source, icons, and local user files retained; existing ignore rules apply |

| New route group | Additional permission |
| --- | --- |
| Logout | Authenticated bearer token only |
| Disbursement post | `cash_disbursements.post` and `cash_disbursements.approve` |
| RFP submit / review / approve / reject | `rfp.submit` / `approvals.review` / `rfp.approve` / `rfp.reject` |
| Voucher approve / release / pay | `vouchers.approve` / `vouchers.post` plus disbursement create, approve, post and `rfp.pay` / `rfp.pay` |

## Audit findings

| IDs | Root cause and correction | Main files | Evidence / limit |
| --- | --- | --- | --- |
| 01–05 | RFP and voucher create/edit accepted forged states and actor fields; RFP transitions did not exist. Prohibited client workflow metadata, derived actors and dates from the authenticated user, added locked transitions with rejection reasons and self-approval checks, and protected submitted/approved financial data. | `RfpController`, `CashCheckVoucherController`, `routes/api.php` | `RfpWorkflowTest`, `FinancialWorkflowIntegrityTest` passed |
| 06 | Disbursement create posted its journal with only create permission. It now creates Draft by default; explicit `post_now` or the new post route requires approval and post permissions. Draft edits and deletes update or remove the Draft journal atomically. Desktop requests one-step posting only when those permissions are returned by the server. | `CashDisbursementController`, `CashDisbursementsForm.vb`, new status migration | `FinancialWorkflowIntegrityTest` passed |
| 07–08 | Direct journal requests could forge system source references and source posting could be repeated. Direct source references, including namespace/underscore aliases, and source journal post requests are rejected; source controllers lock their documents, validate exactly one Draft journal for edit, delete, and post, and post atomically. Voucher release creates a posted disbursement in the same transaction. | `JournalEntryController`, `JournalEntryService`, cash source controllers, voucher controller | `JournalEntryIntegrityTest`, `FinancialWorkflowIntegrityTest`, `CashReceiptIntegrityTest`; alias assertion passed in the native full suite; custom free-form journal references still need a domain policy |
| 09 | Account type could be changed after posting, reinterpreting history. Locked updates reject type changes with posted lines, while allowing metadata edits and deactivation. | `AccountController`, `JournalEntryService` | `ValidationIntegrityTest` passed |
| 10–15 | Voucher amount/currency and duplicate linkage were not fully enforced, RFP detail totals were unchecked, invoice line totals and approval fields were client supplied. Added one-to-one voucher constraint with legacy preflight, exact amount/currency checks, balanced RFP detail submission, fixed precision invoice calculation, and immutability for existing approved invoices. Voucher release posts the cash disbursement before state advances. | voucher/RFP/invoice controllers, uniqueness migration | `FinancialWorkflowIntegrityTest`, `ValidationIntegrityTest`; invoices remain documents with no automatic journal posting |
| 16–17 | Ledger omitted opening entries and sorted by line ID. It now includes all prior posted lines and runs in date, journal ID, line ID order with debit/credit normal balances. | `GeneralLedgerController` | `AccountingReportsIntegrityTest` passed |
| 18–22 | Ledger and trial balance hid inactive history; trial balance lacked as-of filtering; dashboard used account names and float accumulation. Historical posted lines remain included, as-of filtering is additive, dashboard uses established account codes and BCMath sums. | ledger, trial balance, dashboard controllers | `AccountingReportsIntegrityTest`, existing `DashboardTest` passed |
| 23–24 | Asset salvage could exceed cost and inactive assets vanished from historical lapsing reports. Added bounds checks and daily/monthly effective-date fields, future-acquisition flags, accumulated/period depreciation caps, and inactive history inclusion. Existing `as_of` response remains. | `FixedAssetController`, `LapsingScheduleController` | existing `LapsingScheduleTest`, `ValidationIntegrityTest` passed |
| 25–26 | Check details were not required for Check disbursements and PHP float sums affected accounting totals. Payment validation is method-specific; journal, ledger, trial balance, dashboard, voucher, receipt, invoice, and depreciation calculations use BCMath before two-decimal serialization. | cash controllers, journal service, report controllers, invoice controller | 55 PHP tests passed |
| 27–31 | Workflow routes, logout, one consistent login response, token expiry, and login throttling were incomplete. Added routes, kept token type Bearer, eight-hour token expiry, current-token revocation, and a five-attempt minute throttle. | `routes/api.php`, `AuthController` | `RfpWorkflowTest`, `ApiAuthenticationTest` passed |
| 32 | Public web registration was open. The existing GET/POST routes now return 404 by default and can be explicitly enabled for controlled onboarding; initial admin seeding remains available. | `config/auth.php`, `RegisteredUserController`, `.env.example` | `RegistrationTest` passed |
| 33–36 | Docker lacked PDO SQLite; desktop API addresses differed; menu access used roles; journal grid read one page. Added SQLite extension, one environment-based base URL, server-returned effective permissions, and bounded-size page requests through the final journal page. | `Dockerfile`, `ApiSession.vb`, `LoginForm.vb`, `MainForm.vb`, `JournalEntriesForm.vb` | Docker build passed; Windows checks unavailable |
| 37–40 | Host dependencies were incomplete, baseline account seeding overwrote customizations, and tracked build output polluted the tree. Documented host requirements, invoked idempotent account seeding, and removed tracked generated files from Git's index while preserving files locally. | seeders, `.gitignore`-covered build paths, setup docs | Native PHP 8.3.35, Composer 2.10.3, and .NET 10.0.402 installed; `composer install`, platform checks, `npm ci`, Vite build, seeder test, and Windows-target cross-build passed |

## Additional findings

The disbursement model had no document status even though journals did. The additive status migration checks existing disbursement/journal pairs first, then records Draft or Posted without rewriting journal history. A separate additive voucher preparer identity field makes self-approval checks reliable for new vouchers without changing legacy display names. Voucher release requires an active RFP debit account and a distinct active Asset payment account. Account seeding now preserves custom names and classifications for existing account codes; repeat role seeding adds missing baseline permissions without removing custom grants.

Voucher release posts in PHP only because the disbursement book has no currency column or exchange-rate contract. Non-PHP vouchers can remain Draft or Approved for document handling but cannot be released through the accounting route until a reviewed multi-currency posting design is added. Docker now creates an empty default SQLite file and gives Apache's `www-data` user write access to it and its directory.

## Validation performed

- `git diff --check`: passed.
- Static route method/path comparison: passed, 71 retained, nine added, no duplicates.
- `npm ci`: passed, 171 packages installed from `package-lock.json`.
- `npm run build`: passed, Vite 8.3.3.
- `npm audit`: nine existing advisories in locked development dependencies; `npm audit --omit=dev` found zero; no forced upgrade.
- Docker Apache/PHP image build, locked `composer install` (119 packages), `composer check-platform-reqs`, PHP syntax checks, and Laravel `route:list`: passed before the host reboot. Native Gentoo `composer install` and platform checks also passed with PHP 8.3.35 and Composer 2.10.3.
- Native full PHPUnit suite: 57 passed, 252 assertions, zero failures, on isolated in-memory SQLite. This includes the receipt integrity fix and the final journal alias assertion.
- Fresh disposable SQLite migration and two consecutive full seed runs: passed; no persistent database touched.
- Docker image build and direct SQLite write as Apache's `www-data`: passed before the final receipt/migration source edits. A final image rebuild was interrupted by the host reboot.
- .NET 10 desktop cross-compilation on Gentoo: passed with zero warnings and zero errors. Windows runtime tests remain unavailable on this host.

## Remaining concerns and readiness

**READY** for a native Gentoo API local-server trial: native dependencies, the full suite, and the frontend build pass. A local-only server was started on request at `127.0.0.1:8000`; `/` returned 200, protected `/api/dashboard` returned 401 without a token, and login, authorized dashboard, and logout returned 200. Docker stopped after the host reboot and the account cannot restart it without an administrator password, so the final Docker image snapshot is unverified. The desktop cross-build passed, but Windows runtime behavior remains unverified because Windows is unavailable here. The old root-owned generated `vendor.root-owned-backup` directory still needs `sudo rm -rf` after confirming the new user-owned `vendor` works. Repeated posting was tested, but simultaneous requests against a production SQL server were not stress tested; transaction locks and uniqueness constraints enforce the intended invariants. Legacy RFPs without an identified `user_id` cannot be approved until ownership is reconciled; the repair does not guess an approver or rewrite history. Historical asset deactivation has no effective-date column, so an inactive asset remains visible in historical reports without a precise deactivation cutoff. Depreciation uses an eight-decimal daily rate and two-decimal output; an approved organizational depreciation convention is still needed before using report figures as posted entries. The journal API permits paired free-form references for legitimate direct vouchers; only known system-generated source types are reserved. The desktop journal grid retrieves small API pages but retains all loaded rows in memory. The current npm lockfile has nine audit advisories and needs a separately reviewed dependency update.
