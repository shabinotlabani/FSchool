# Football fields and private booking checks

Also verifies required distinct active head/assistant coaches, rejection of invalid staff, portal-access synchronization on replacement, stale edit protection, staff audit snapshots, and prevention of deactivation or role removal while assigned to a team.

Run from the repository root:

```powershell
dotnet run --project tests/Fields.Checks --configuration FieldsCheck -- "$PWD"
```

Uses `ConnectionStrings__DefaultConnection` or `appsettings.json` to create a uniquely named temporary PostgreSQL database. Applies migrations, tests shared team slots, private booking collisions and concurrent requests, field management, partial payments, duplicate submissions, refunds, cash reporting, cancellation history, and weekly series (dates, duplicate submissions, all-or-nothing conflict handling, and cancelling a single occurrence), plus cash/bank opening balances, receipts, expenses, cross-period cancellations, legacy receipts, and reconciliation of payment-method totals. Drops only that test database in `finally`; no test bookings or payments are written into the application database.

Attendance checks cover role-specific authorization, active name-sorted rosters, explicit marking, shared daily sheets, stale write protection, missing/foreign/duplicate players, future dates, historical snapshots after transfers, and change audits.

Also checks the one-hour attendance edit deadline at its exact boundary for head coach, assistant and admin, verifies edits do not extend it, and checks the fourteen-day history cutoff.
