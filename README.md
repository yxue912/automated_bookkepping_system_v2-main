# Aqua ARC Automated Bookkeeping System

Aqua ARC consists of a .NET 10 Windows Forms desktop application and a Laravel 13 API. The desktop contains the bookkeeping forms; the Laravel web page is not a replacement for that interface.

The desktop runs on Windows. The API can run on Windows or Gentoo Linux. The desktop uses `http://127.0.0.1:8000/api/` by default and reads `AQUA_ARC_API_BASE_URL` when the API is elsewhere. Use HTTPS when connecting across machines with real credentials.

## Repository

- `WaterRefilling.App/` — existing Windows desktop application.
- `water-refilling-api/` — Laravel API, database migrations, seeders, and tests.

## Windows download

Get the current desktop executable and complete Windows package from [Releases](https://github.com/angemarcos04/automated_bookkeeping_system/releases/latest). The standalone `WaterRefilling.App.exe` runs on Windows x64. The `AquaARC-win-x64.zip` package contains the desktop executable and its supporting files; extract the ZIP before running it. The desktop needs a running Laravel API; follow [Local setup](LOCAL_SETUP.md) to configure it.

## Start here

- [Dependencies](DEPENDENCIES.md)
- [Local setup](LOCAL_SETUP.md)
- [API details](water-refilling-api/README.md)
- [Bug fix report](BUG_FIX_REPORT.md)
- [Baseline route inventory](ROUTE_BASELINE.md)

The backend test suite uses disposable in-memory SQLite. Run `php artisan test` from `water-refilling-api/`. Build the desktop on Windows with `dotnet build WaterRefilling.App/WaterRefilling.App.vbproj`; Linux can cross-build with `-p:EnableWindowsTargeting=true` but cannot run Windows Forms natively.
