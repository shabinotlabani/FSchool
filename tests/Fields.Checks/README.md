# Football fields and private booking checks

Run from the repository root:

```powershell
dotnet run --project tests/Fields.Checks --configuration FieldsCheck -- "$PWD"
```

Uses `ConnectionStrings__DefaultConnection` or `appsettings.json` to create a uniquely named temporary PostgreSQL database. Applies migrations, tests shared team slots, private booking collisions and concurrent requests, field management, partial payments, duplicate submissions, refunds, cash reporting, cancellation history, and weekly series (dates, duplicate submissions, all-or-nothing conflict handling, and cancelling a single occurrence), plus cash/bank opening balances, receipts, expenses, cross-period cancellations, legacy receipts, and reconciliation of payment-method totals. Drops only that test database in `finally`; no test bookings or payments are written into the application database.
