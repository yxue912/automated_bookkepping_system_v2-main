# Dependencies

## Requirements and detected environment

| Component | Project constraint | Detected on this Gentoo host |
| --- | --- | --- |
| PHP | `^8.3` in `composer.json` | Gentoo PHP 8.3.35 CLI installed; required platform checks passed |
| Composer | 2, lockfile install | User-local Composer 2.10.3 installed in `~/.local/bin` |
| Laravel | `^13.17` | Locked 13.34.0, installed and checked in the Docker PHP runtime |
| Sanctum / Spatie Permission | `^4.3` / `^8.3` | Locked 4.3.3 / 8.3.0, installed in Docker |
| Node.js / npm | Vite 8 lockfile engine `^20.19.0 || >=22.12.0`; one optional locked package asks for `>=22.13.0`, so use Node 22.13+ for the complete tree | Node 26.10.0 / npm 11.19.1 |
| SQLite CLI | Useful for default database | 3.53.4 |
| Git | Source control | 2.55.0 |
| .NET SDK | .NET 10 Windows Forms | User-local .NET SDK 10.0.402 installed; Windows is required to run WinForms |
| Docker | Optional backend verification/deployment | Docker 29.8.1 installed; daemon stopped after host reboot |

Locked versions: Laravel 13.34.0, Sanctum 4.3.3, Spatie Permission 8.3.0, PHPUnit 12.5.38, Laravel Boost 2.10.3, and Vite 8.3.3. These versions came from the checked-in lockfiles, which were not regenerated.

`npm ci` installed 171 lockfile packages and `npm run build` passed with Vite 8.3.3. The npm audit reports nine advisories (two moderate, five high, two critical) in the current locked development tree; `npm audit --omit=dev` reports zero. No forced lockfile upgrade was made.

The repaired Apache/PHP Docker image built successfully before the host reboot. A separate `composer install` with development dependencies installed all 119 locked packages in a Docker PHP runtime, and `composer check-platform-reqs` passed. The production image installed its 80 locked runtime packages. The default SQLite file and directory were verified writable by Apache's `www-data` user. Native Gentoo `composer install` also installed all 119 locked packages, and its platform check passed.

The PHP runtime needs PDO SQLite for the default database, BCMath for exact accounting sums, and the extensions required by the Composer lockfile and Laravel: Ctype, cURL, DOM/XML, Fileinfo, Filter, Hash, Mbstring, OpenSSL, PDO, Tokenizer, and Zip. The Dockerfile enables PDO SQLite, PDO MySQL, BCMath, Mbstring, Exif, PCNTL, Zip, cURL, DOM, and XML. `composer check-platform-reqs` is the final authority for the installed lockfile.

On Gentoo/OpenRC, PHP 8.3 needs `cli bcmath curl ctype fileinfo filter iconv intl pdo phar readline session simplexml sqlite ssl tokenizer unicode xml xmlreader xmlwriter zip zlib` USE flags for this application. The `unicode` flag enables Mbstring in Gentoo's PHP ebuild. The current local Portage tree did not offer a Composer ebuild (`emerge -s composer` found none), so Composer 2 was installed with its checksum-verified [official installer](https://getcomposer.org/download/) into `~/.local/bin`.

On Windows, install the .NET 10 SDK and Windows desktop targeting pack to build `WaterRefilling.App/WaterRefilling.App.vbproj`. The desktop runtime is Windows only. Keep the existing `net10.0-windows` target. Linux cross-compilation passed with `dotnet build WaterRefilling.App/WaterRefilling.App.vbproj -p:EnableWindowsTargeting=true`.

## Verify

```sh
php -v
php -m
composer --version
composer check-platform-reqs
node --version
npm --version
sqlite3 --version
dotnet --info
```

Use `composer install`, not `composer update`, and `npm ci`, not `npm install`, for a fresh checkout. The `laravel/boost` package is already declared as a Composer development dependency. Do not run `composer require` to add it again.

Common failures: `could not find driver` means PDO SQLite is missing; `Call to undefined function bcadd()` means BCMath is missing; Composer platform errors identify a PHP version or extension mismatch; Vite engine errors require a compatible Node version; and WinForms build/runtime errors on Linux require a Windows machine for desktop validation.
