# Duplicate player registration checks

Set `ConnectionStrings__DefaultConnection` to an isolated PostgreSQL test server with permission to create databases. The test creates and removes its own uniquely named database.

```powershell
dotnet build tests/StudentDuplicates.Checks --disable-build-servers
dotnet tests/StudentDuplicates.Checks/bin/Debug/net9.0/StudentDuplicates.Checks.dll .
```

Optional browser checks: append `tests/StudentDuplicates.Checks/browser.cjs` and configure `PLAYWRIGHT_MODULE` and `MANUAL_CHROME` for the installed browser tools.

Checks normalized full names; local and international Kosovo phone formats; player-to-parent phone matching; inactive players; tenant isolation; no matches for blank fields or different countries; family registration confirmation and idempotency; antiforgery; live details; server-side review without JavaScript; stale acknowledgement rejection; and explicit continuation for another person. The browser checks live lookup, acknowledgement reset and mobile table scrolling.

Warnings apply to existing players in the current club only. Shared family numbers are warnings, not uniqueness constraints. No database migration is needed.
