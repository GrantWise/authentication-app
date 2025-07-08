# SQLite Migration Fix Instructions

## Problem
The existing migration was created for SQL Server and contains SQL Server-specific syntax that won't work with SQLite.

## Solution 1: Recreate Migrations (Recommended)

```bash
cd /home/grant/authentication/backend/AuthenticationApi

# Remove existing migrations
rm -rf Migrations

# Create new SQLite-compatible migrations
dotnet ef migrations add InitialCreate

# Build and run
dotnet build
dotnet run
```

## Solution 2: Manual Migration File Fix

If you can't recreate migrations, replace the migration content with SQLite-compatible syntax:

### Issues in current migration:
- `"uniqueidentifier"` → should be `"TEXT"` for SQLite
- `"nvarchar(255)"` → should be `"TEXT"` for SQLite  
- `"bit"` → should be `"INTEGER"` for SQLite
- `"datetime2"` → should be `"TEXT"` for SQLite
- `"NEWID()"` → not available in SQLite (remove defaultValueSql)
- `"GETUTCDATE()"` → not available in SQLite (remove defaultValueSql)

## After Fix

The application should:
1. Build successfully with `dotnet build`
2. Run successfully with `dotnet run`
3. Create an SQLite database file `AuthenticationDb.db`
4. Be accessible at `https://localhost:7157`
5. Show Swagger UI at `https://localhost:7157/swagger`

## Test Users (Development)
- **testuser** / TestPassword123! (User role)
- **admin** / AdminPassword123! (Admin role)
- **flowcreator** / FlowPassword123! (FlowCreator role)