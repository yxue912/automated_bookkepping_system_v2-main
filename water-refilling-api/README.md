# Aqua ARC API

This Laravel 13 application serves the Aqua ARC Windows desktop. The desktop authenticates with `POST /api/login`, sends the returned bearer token to protected routes, and revokes it with `POST /api/logout`. Tokens expire after eight hours.

For requirements and setup, see [Dependencies](../DEPENDENCIES.md) and [Local setup](../LOCAL_SETUP.md). The [bug fix report](../BUG_FIX_REPORT.md) records validation and remaining limitations; [route inventory](../ROUTE_BASELINE.md) records the original API contract.

From this directory, install the locked dependencies with `composer install` and `npm ci`. Run `php artisan test` against the disposable in-memory SQLite test database. After configuring a fresh local `.env` and database as described in the setup guide, start a local-only API with `php artisan serve --host=127.0.0.1 --port=8000`.
