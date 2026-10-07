# FSchool / FC 2 Korriku

ASP.NET Core 9 MVC application for players, teams, football fields, bookings, payments, inventory and financial reporting.

## Local setup

Requires the .NET 9 SDK and PostgreSQL.

1. Copy `appsettings.Example.json` to `appsettings.json` and set the database connection, or configure the `ConnectionStrings__DefaultConnection` environment variable.
2. Run `dotnet restore`.
3. Apply migrations with `dotnet ef database update`.
4. Start the application with `dotnet run`.

Local configuration files and database credentials are excluded from Git. Keep production credentials in environment variables.

## Checks

The integration check projects in `tests/` create temporary PostgreSQL databases and remove them after execution. See each project's README for instructions. The PostgreSQL test user must be allowed to create databases.
