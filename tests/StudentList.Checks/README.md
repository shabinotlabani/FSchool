# Player list pagination checks

Use an isolated PostgreSQL server with database creation permission in `ConnectionStrings__DefaultConnection`.

```powershell
dotnet build tests/StudentList.Checks --disable-build-servers
dotnet tests/StudentList.Checks/bin/Debug/net9.0/StudentList.Checks.dll .
```

Creates and removes a temporary database with 5,001 players. Checks latest-first ordering, ten-row pages, distinct consecutive pages, search across older records, telephone search, literal wildcard handling, safe page bounds, tenant isolation and rendered pagination links retaining the search.

The controller applies SQL `Skip`/`Take` and selects only player IDs, names and phone numbers. Financial details are loaded through the player's account, not the list. No schema change is required.
