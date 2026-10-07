# Production deployment checks

From the repository root:

```powershell
dotnet publish -c Release -p:UseAppHost=false -o ./bin/RailwayPublishCheck
dotnet run --project tests/Deployment.Checks --configuration DeploymentCheck -- "$PWD"
```

Creates a temporary PostgreSQL database, runs the published Production migration command, checks empty operational tables and initial administrator setup, verifies safe repeat deployments, then starts the published application and checks its health/login endpoints. Stops only its own child processes and drops only its own database. Never imports or modifies local application data.

The HTTP startup check uses a second empty database without running the pre-deploy command, verifying that normal Production startup creates the schema and first administrator before serving requests. Both temporary databases are removed afterwards.

Set `TEST_BOOTSTRAP_PASSWORD` privately in the test environment to verify that the stored initial Identity hash matches the owner-provided password. The test also verifies that absent or invalid legacy ADMIN_PASSWORD variables cannot block setup.
