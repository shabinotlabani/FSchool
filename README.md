# FSchool / FC 2 Korriku

ASP.NET Core 9 MVC application for players, teams, football fields, bookings, payments, inventory and financial reporting.

## Local setup

Requires the .NET 9 SDK and PostgreSQL.

1. Copy `appsettings.Example.json` to `appsettings.json` and set the database connection, or configure the `ConnectionStrings__DefaultConnection` environment variable.
2. Run `dotnet restore`.
3. Apply migrations with `dotnet ef database update`.
4. Start the application with `dotnet run`.

Local configuration files and database credentials are excluded from Git. Keep production credentials in environment variables.

## Railway: fresh database

Deploy this repository and add a new PostgreSQL service in the same Railway project. No local players, payments, stock or account balances are imported. Migrations create the tables and built-in setup values (roles, tariff definitions, size groups and three editable football fields).

Set these variables on the application service:

| Variable | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `DATABASE_URL` | `${{Postgres.DATABASE_URL}}` (use the actual database service name) |
| `PORT` | `8080` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` |
| `ADMIN_EMAIL` | Your administrator email |
| `ADMIN_PASSWORD` | A unique strong password with uppercase, lowercase and digits |

Railway reads `railway.json`, builds the Docker image, and runs `dotnet 2Korriku.dll --migrate` before starting the app. This creates/updates the schema and creates the first administrator. An invalid configuration stops deployment. Subsequent deployments retain data and never reset administrator passwords. Remove `ADMIN_PASSWORD` after the first successful deployment.

Production startup also applies any pending migrations before seeding roles or accepting requests. A fresh database therefore works even if Railway's pre-deploy configuration is skipped. Startup does not reset tables or import local data.

Generate a public domain in Settings → Networking with target port 8080. Railway terminates HTTPS; the forwarded-headers variable is intended for this proxy-hosted deployment. Use a single application replica. Container replacement may require users to log in again because authentication keys are not stored on a persistent volume.

References: [Railway pre-deploy commands](https://docs.railway.com/deployments/pre-deploy-command), [config as code](https://docs.railway.com/config-as-code/reference).

## Checks

The integration check projects in `tests/` create temporary PostgreSQL databases and remove them after execution. See each project's README for instructions. The PostgreSQL test user must be allowed to create databases.
