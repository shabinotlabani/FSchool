# Treasury checks

Run from the repository root:

```powershell
dotnet run --project tests/Treasury.Checks --configuration TreasuryCheck -- "$PWD"
```

Creates a uniquely named temporary PostgreSQL database using the configured local connection. Applies migrations and verifies opening balances, account-specific withdrawals, concurrent and duplicate submissions, historical balance protection, expense integration, cancellation audit, replacement openings, date-filtered history and reconciliation of report totals. Drops only its own test database in `finally`.

Initial funds are dated additions to existing recorded movements, not a reset of history. Reports carry earlier funds in the opening balance and show funds registered or reversed within the selected period separately from receipts. Owner withdrawals are separate from operating expenses and are included in outgoing balances. Opening setup and owner withdrawals require the Admin role.
