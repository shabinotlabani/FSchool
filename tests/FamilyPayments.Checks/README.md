# Family payment checks

Run from the repository root:

```powershell
dotnet run --project tests/FamilyPayments.Checks --configuration FamilyBilling -- "$PWD"
```

The runner uses `ConnectionStrings__DefaultConnection`, or the connection in
`appsettings.json`, to create a uniquely named temporary PostgreSQL database.
The database account needs permission to create databases. The runner applies
the migrations, exercises the payment service and controller routes, and drops
only its own test database in `finally`. It never inserts test payments into
the application database.

Checks cover family grouping, full-payment enforcement, stale forms, concurrent
double submission, shared receipts, atomic cancellation, available-funds checks,
cash totals, and current versus future family membership.
