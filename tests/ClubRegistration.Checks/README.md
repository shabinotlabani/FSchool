# Club registration checks

Set `ConnectionStrings__DefaultConnection` to an isolated PostgreSQL test server with database creation permissions. The checks create and remove their own temporary database; never point them at production.

```powershell
dotnet build tests/ClubRegistration.Checks --disable-build-servers
dotnet tests/ClubRegistration.Checks/bin/Debug/net9.0/ClubRegistration.Checks.dll .
```

Checks public registration, validation, antiforgery, pending persistence, ignored forged status, tenant privacy, owner review, HTML encoding, migration consistency, and absence of automatic club/account creation. Optional second argument `tests/ClubRegistration.Checks/browser.cjs` checks desktop/mobile rendering; set `PLAYWRIGHT_MODULE` and `MANUAL_CHROME` for the installed browser tooling.

The `ClubRegistrationRequests` migration creates a separate global request table. Public users submit four fields at `/Clubs/Register`. Only the platform owner can read `/Clubs/Requests` and mark requests reviewed. The owner creates clubs/accounts and sends credentials separately. Submissions are limited to five attempts per IP per hour.
