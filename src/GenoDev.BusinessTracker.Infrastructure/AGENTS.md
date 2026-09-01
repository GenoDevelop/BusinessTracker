# Infrastructure guidance

These rules apply under `GenoDev.BusinessTracker.Infrastructure` in addition to the repository guide.

## Entity Framework Core and PostgreSQL

- PostgreSQL is the only supported relational behavior.
- `BusinessTrackerDbContext` uses lazy-loading proxies, snake_case naming, assembly-scanned `IEntityTypeConfiguration<T>` mappings, and multiple schemas. Put mapping rules in `Configurations`.
- New entities require the appropriate `DbSet`, entity configuration, application abstraction exposure when needed, relationships/navigation initialization matching neighboring entities, and shared test arrange support.
- When adding a dependent with a non-default client-generated key to an already tracked aggregate, add it explicitly through its `DbSet` as well as the navigation. Navigation-only discovery can classify it as `Modified` and issue an incorrect `UPDATE`. Cover the mixed update-plus-add case in a handler test.
- `SaveChanges` normalizes whitespace-only nullable strings to `null`; do not repeat this normalization in handlers.
- Runtime configuration comes from `infrastructure_settings.json`. Never commit credentials or expose connection strings in output.

## Migrations

For every schema-affecting model/configuration change, create the migration before tests from the repository root:

```powershell
dotnet ef migrations add <DescriptiveMigrationName> --project src/GenoDev.BusinessTracker.Infrastructure
```

Inspect the generated migration and model snapshot and ensure they contain only intended changes. Do not hand-author or rename generated migration/designer/snapshot files, remove existing migrations, or reset the database unless explicitly requested.

Never apply migrations to the user's local database. Do not run `dotnet ef database update`, `Database.Migrate`, `EnsureCreated`, SQL scripts, or equivalent operations against configured development data. Existing automatic migration of disposable Testcontainers databases remains allowed.

Read the repository's canonical product-image or mailing guide before changing their storage, retention, SMTP, attachment, or outbox infrastructure.
