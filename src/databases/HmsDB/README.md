# HmsDB — SQL Server migrations

DbUp-based migration runner for **Microsoft SQL Server** (shared-database multi-tenant model).

```
HmsDB/
├── Schema/
│   ├── Tables/           # Initial table scripts (01_, 02_, …)
│   └── Seed/             # Master / seed data
├── Programmability/
│   └── Procedures/       # Stored procedures (sp_*)
├── Migrations/           # Incremental migration scripts
└── Utilities/
```

**Convention:** Use stored procedures (`sp_*`), not inline functions, for all programmability.

Configure connection string in `appsettings.json` before running:

```bash
dotnet run --project src/databases/HmsDB
```
