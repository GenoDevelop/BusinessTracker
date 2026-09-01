# GenoDev.BusinessTracker — repository guidance for AI agents

This is the repository-wide guide. More specific `AGENTS.md` files supplement or override it for their directory trees:

- `src/GenoDev.BusinessTracker.ApplicationLogic/AGENTS.md`
- `src/GenoDev.BusinessTracker.Infrastructure/AGENTS.md`
- `src/GenoDev.BusinessTracker.Wpf/AGENTS.md`
- `src/GenoDev.BusinessTracker.Wpf/Controls/Windows/AGENTS.md`
- `src/GenoDev.BusinessTracker.Wpf/Controls/DataGrids/AGENTS.md`
- `tests/AGENTS.md`

## Keep agent guidance useful and compact

Architecture documentation is part of a change only when the change introduces or intentionally alters a durable convention. Do not update an `AGENTS.md` merely because code changed.

Before adding a rule, all of these must be true:

1. It is expected to remain valid across future work, not just the current task.
2. It applies to a class of changes rather than one file, bug, or implementation episode.
3. It is not already obvious from the code, types, tests, or a more authoritative document.
4. Omitting it has a realistic chance of causing repeated architectural or workflow mistakes.
5. It is placed in the narrowest directory or canonical feature guide that covers its scope.

Do not record chronological notes, completed-task details, transient workarounds, exhaustive implementation choreography, or descriptions of one regression fix. Prefer stating the invariant and the reason in one short rule. Amend or remove an existing rule instead of appending a near-duplicate. Keep one canonical copy of every rule and link to it when a concern crosses directory boundaries.

If a proposed addition needs a long paragraph, several exceptions, or would make a guidance file materially harder to scan, first consider whether the behavior belongs in code comments, tests, an ADR, or a feature guide under `docs/agent-guides`. Split a local `AGENTS.md` before it becomes a repository-wide handbook. Treat roughly 20 KB as a review threshold, not a target.

When code and guidance disagree, inspect the surrounding implementation and tests. Bring the change back to the established pattern or update the guidance only when the divergence is an intentional durable decision.

## Technology and solution structure

The application is a .NET 10 Windows desktop application using WPF, CommunityToolkit.Mvvm, MediatR, Entity Framework Core 10, and PostgreSQL.

Run all .NET restore, build, test, application, and EF CLI work directly on the host with the local .NET 10 SDK. Docker is used only for PostgreSQL development and disposable Testcontainers databases. Never use a Docker SDK image as a fallback; report a missing local SDK instead of changing the target framework or toolchain.

The repository's `global.json` opts `dotnet test` into the .NET 10 Microsoft.Testing.Platform runner used by xUnit v3. Keep it enabled and identify test inputs with `--project` or `--solution`.

- `src/GenoDev.BusinessTracker.Domain`: entities and domain enums; no UI, persistence, or application-use-case dependencies.
- `src/GenoDev.BusinessTracker.ApplicationLogic`: CQRS requests, handlers, DTOs, abstractions, query helpers, and application services; depends only on Domain.
- `src/GenoDev.BusinessTracker.Infrastructure`: EF Core context, mappings, PostgreSQL setup, services, and migrations; implements application abstractions.
- `src/GenoDev.BusinessTracker.Wpf`: composition root, views, ViewModels, filters, converters, and reusable controls.
- `tests/GenoDev.BusinessTracker.ApplicationLogic.Tests`: handler and application-service tests.
- `tests/GenoDev.BusinessTracker.TestsUtilities`: PostgreSQL fixture, database reset, clocks, and arrange builders.
- `utilities/GenoDev.Utilities.Core`: small reusable, business-independent utilities.

Dependency direction is `Wpf -> ApplicationLogic`, `Wpf -> Infrastructure`, `Infrastructure -> ApplicationLogic + Domain`, and `ApplicationLogic -> Domain`. Do not reverse it for convenience.

## NuGet dependencies and licensing

Reuse the framework, existing packages, and repository code first. Never add a new direct NuGet dependency without the user's explicit approval.

Before requesting approval, verify the exact package/version using official metadata and report its license, whether commercial use is permitted, and any material obligations or restrictions. Missing or ambiguous ownership, version, license, or commercial terms blocks adding the package. Treat an existing dependency update that materially changes licensing the same way.

## Cross-cutting application rules

- UI-triggered business operations go through MediatR. Requests and DTOs remain independent of WPF, and handlers depend on application abstractions rather than Infrastructure.
- PostgreSQL is the only supported relational behavior. Do not replace database-dependent tests with EF's in-memory provider.
- Pass cancellation tokens through MediatR, EF async operations, services, pagination, and tests.
- Preserve business invariants and update related aggregates and counters atomically. Study adjacent inventory, production, supply, recipe, and order flows before modifying them.
- Keep Polish user-facing labels, validation messages, and formatting. Code identifiers and technical documentation remain English.
- Reuse established patterns and shared components before introducing new frameworks, repositories, event buses, navigation systems, or parallel design systems.

Read the canonical feature guide whenever a change touches the named concern in any layer:

- Product images: `docs/agent-guides/product-images.md`
- Notes and rich text: `docs/agent-guides/notes.md`
- Order mailing, SMTP, templates, attachments, or outbox delivery: `docs/agent-guides/mailing.md`

## Database schema and migrations

Any model or configuration change that affects the schema requires a generated EF Core migration before tests:

```powershell
dotnet ef migrations add <DescriptiveMigrationName> --project src/GenoDev.BusinessTracker.Infrastructure
```

Inspect the migration and snapshot for unintended changes. Do not hand-author, rename, remove, or reset generated migration artifacts.

Never apply migrations to the user's local/development database. Do not run `dotnet ef database update`, `Database.Migrate`, `EnsureCreated`, or SQL against it. Automatic migration of the isolated Testcontainers database is allowed. The development PostgreSQL service in `docker-compose.yml` listens on port 5434; the application and .NET tooling still run locally.

## Verification

Every handler or application-service behavior change requires focused tests for the affected success, failure, projection, filtering, sorting, pagination, validation, and side-effect paths. Follow `tests/AGENTS.md` whenever production work requires tests, even though the test files live in another subtree.

Use one impact-based `dotnet test` invocation and terminal session. Do not run unrelated handler tests for WPF-only changes and do not run the full solution by default. Build the solution when WPF/XAML, DI registrations, project references, or compile-time contracts change:

```powershell
dotnet build GenoDev.BusinessTracker.sln
```

Handler/application tests require Docker for PostgreSQL Testcontainers. If Docker is unavailable or container startup fails for that reason, stop and ask the user to start Docker. Do not substitute another database, skip required tests, or claim successful verification.

## Change checklist

Before completing a change, verify the applicable items:

1. Layering, CQRS, cancellation, and existing shared patterns remain intact.
2. Schema changes have a generated and reviewed migration, and nothing was applied to the user's database.
3. Focused tests cover changed application behavior; WPF/contracts/DI changes also build successfully.
4. No dependency was added without informed user approval.
5. Agent guidance changed only for a genuinely durable convention and remains in the narrowest canonical location.
