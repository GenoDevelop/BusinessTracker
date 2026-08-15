# GenoDev.BusinessTracker - repository guidance for AI agents

This file is the authoritative, repository-wide guide for AI-assisted work. Read it before changing code. It describes the current architecture and conventions; preserve them unless the user explicitly approves a different direction.

## Keep this guide current

- Treat architecture documentation as part of every change.
- When work introduces or confirms a reusable architectural decision, convention, workflow, shared component, or important exception, update this file in the same change.
- Record durable rules, not task-specific implementation details or a chronological diary.
- If the code and this file disagree, investigate the surrounding implementation and tests. Do not silently choose a new pattern. Either bring the change back to the established pattern or update this guide when the divergence is an intentional new decision.
- More specific `AGENTS.md` files may be added below a directory when that area needs local rules; their instructions supplement or override this root guide for that subtree.

## Technology and solution structure

The application is a .NET 10 Windows desktop application using WPF, CommunityToolkit.Mvvm, MediatR, Entity Framework Core 10, and PostgreSQL.

- `src/GenoDev.BusinessTracker.Domain`: entities and domain enums. It must not depend on UI, persistence, or application use cases.
- `src/GenoDev.BusinessTracker.ApplicationLogic`: CQRS requests, handlers, DTOs, application abstractions, query helpers, and application services. It depends on Domain, not Infrastructure or WPF.
- `src/GenoDev.BusinessTracker.Infrastructure`: EF Core `BusinessTrackerDbContext`, entity configurations, PostgreSQL setup, design-time context factory, and migrations. It implements application abstractions.
- `src/GenoDev.BusinessTracker.Wpf`: composition root, WPF views, MVVM view models, filters, converters, and reusable controls.
- `tests/GenoDev.BusinessTracker.ApplicationLogic.Tests`: unit/use-case tests for handlers and application services.
- `tests/GenoDev.BusinessTracker.TestsUtilities`: shared test fixture, PostgreSQL Testcontainer support, database reset, clocks, and `Arrange_*` builders.
- `utilities/GenoDev.Utilities.Core`: small reusable, business-independent utilities.

Dependency direction is `Wpf -> ApplicationLogic`, `Wpf -> Infrastructure`, `Infrastructure -> ApplicationLogic + Domain`, and `ApplicationLogic -> Domain`. Do not reference WPF or Infrastructure from Domain/ApplicationLogic merely for convenience.

## CQRS and application use cases

- All business operations invoked by the UI go through MediatR.
- Model reads as `IRequest<T>` queries and writes as commands. Put each request and its handler in the appropriate feature/use-case folder under `ApplicationLogic/UseCases`.
- Keep request contracts and result DTOs independent of WPF. Do not expose EF entities to the UI when a purpose-built DTO is appropriate.
- Handlers depend on `IBusinessTrackerDbContext` and other application abstractions, not the concrete context.
- A query handler builds one server-side `IQueryable`, applies filters and sorting, counts it, pages it, projects it to DTOs, and executes it asynchronously with the supplied cancellation token.
- Read-only queries start with `AsNoTracking()` unless tracking is deliberately required.
- Project to DTOs in SQL with `Select`; avoid loading full graphs and mapping them in memory.
- Commands perform the complete business mutation, call `SaveChangesAsync(cancellationToken)`, and return only the result their caller needs (commonly an ID or no value).
- Preserve business invariants and update every related aggregate/counter consistently. Study adjacent handlers before implementing inventory, production, supply, recipe, or order mutations; these flows affect related totals.
- Pass cancellation tokens through MediatR, EF async operations, pagination loaders, and services.
- Register application services in `ApplicationLogic/Extensions/DependencyInjectionExtensions.cs`; MediatR discovers handlers from the ApplicationLogic assembly.

## Database and Entity Framework Core

- PostgreSQL is the only supported relational behavior. Do not replace database-dependent tests with EF's in-memory provider.
- `BusinessTrackerDbContext` uses lazy-loading proxies, snake_case naming, assembly-scanned `IEntityTypeConfiguration<T>` mappings, and multiple PostgreSQL schemas. Put mapping rules in `Infrastructure/Configurations`, not in WPF or handlers.
- New entities require the appropriate `DbSet`, entity configuration, application abstraction exposure when needed, relationships/navigation initialization consistent with neighboring entities, and test arrange support.
- `SaveChanges` normalizes whitespace-only nullable strings to `null`; do not duplicate that normalization across handlers.
- Runtime configuration comes from `Infrastructure/infrastructure_settings.json`. Do not commit new credentials or expose connection strings in output.

### Mandatory migration workflow

Any model/configuration change that affects the database schema must have an EF Core migration. **Create the migration before running tests**, because the PostgreSQL test database is migrated from the checked-in migrations.

From the repository root, use the standard command:

```powershell
dotnet ef migrations add <DescriptiveMigrationName> --project src/GenoDev.BusinessTracker.Infrastructure
```

Then inspect the generated migration and model snapshot to ensure they contain only the intended changes. Do not hand-author or rename generated migration/designer/snapshot files. Do not remove existing migrations or reset the database unless the user explicitly requests it.

**Never apply migrations to the user's local database.** Do not run `dotnet ef database update`, `Database.Migrate`, `EnsureCreated`, SQL scripts, or any other command against the configured local/development database. The user's workflow is to review and apply migrations personally when appropriate. AI work stops after generating and reviewing the migration. Automatic migration of the isolated, disposable PostgreSQL Testcontainer by the existing test infrastructure is allowed and must remain intact.

The local development PostgreSQL service is defined in `docker-compose.yml` and listens on port 5434.

## Lists, server-side pagination, filtering, and sorting

Use the existing end-to-end list pattern rather than inventing per-view alternatives.

### Query side

- List queries use zero-based `PageIndex` and a positive `PageSize` and return `PagedList<T>`.
- Apply all filters and ordering before `CountAsync`, `Skip`, and `Take`.
- `TotalCount` is the count after filtering and before paging. `HasNextPage` is derived by `PagedList<T>`.
- Apply deterministic server-side ordering before paging. Add a stable tie-breaker when the primary column is not unique and page stability matters.
- Sorting is represented by a feature-specific `*SortBy` enum plus `IsDescending`. Keep enum names aligned with DataGrid `SortMemberPath` values so views can parse them directly.
- Text filters should use `QueryableSearchExtensions.WhereContainsAll` or `WhereContainsAllInAny`. They use PostgreSQL `ILIKE`, escape `%`, `_`, and `\`, split unquoted input into terms combined with AND, and treat quoted input as one phrase. Do not replace them with case-sensitive `Contains` or client-side filtering.
- Numeric filters use `NumericOperator` and `ApplyNumericFilter`; date ranges, enum selections, and booleans should remain nullable/optional so an unset filter does not constrain the query.
- Keep the whole expression EF-translatable. Never call `AsEnumerable`, `ToList`, or custom in-memory logic before filtering, sorting, counting, and paging.

### ViewModel and view side

- Expose a `PaginationPageLoader` from the ViewModel. Its loader receives `PaginationState` and a cancellation token, sends the query through `IMediator`, replaces/updates the observable collection, and returns `TotalCount`.
- Store active filters in immutable `*FilterCriteria` records with an `Empty` value. Provide explicit ViewModel methods such as `Set...Filter` and `Set...Sorting`; after state changes, request a pagination refresh.
- Use the shared `PaginationControl`; do not implement page buttons, counts, cancellation, or page-size state separately in a view. Bind its `PageLoader` and call `RefreshAsync()` after data-changing operations, filter changes, sorting changes, or explicit refresh.
- `RefreshAsync()` is the default for tables and must preserve the current page whenever the control can do so. An explicit Refresh button must call `RefreshAsync()` and must not reset pagination.
- `ResetAndRefreshAsync()` exists for exceptional cases only. Use it only when returning to page zero is a deliberate product requirement or the current context makes retaining the page semantically invalid; document or make that reason clear at the call site. Do not reset merely because a filter, sort, or ordinary refresh occurred.
- The control owns zero-based page state, page sizes, cancellation/versioning of overlapping loads, displayed one-based page numbers, total count, and navigation state.
- Views subscribe to ViewModel refresh events on `Loaded`/`DataContextChanged` and unsubscribe on `Unloaded`. This prevents duplicate subscriptions and leaks.
- Sorting is initiated by the DataGrid `Sorting` event: parse `SortMemberPath` into the matching sort enum, set `e.Handled`, clear other sort glyphs, toggle direction, update the ViewModel, and refresh pagination. Do not use WPF collection-view sorting for database-backed lists.

### Views containing multiple tables

`Sales/OrdersView` is the reference implementation for a screen containing several independently filtered and paginated tables. Multi-table screens must not share table state implicitly.

- Give every table its own observable filter-visibility property, immutable filter criteria, sort column/direction, `ObservableCollection`, `PaginationPageLoader`, and `PaginationControl`. For example, Orders uses `IsFilterVisible`, Products uses `IsProductsFilterVisible`, and Packing Materials uses `IsPackingMaterialsFilterVisible`.
- Bind each table's `FilterToggleButton.IsChecked` directly to that table's visibility property with `Mode=TwoWay` and `UpdateSourceTrigger=PropertyChanged`. Never bind a secondary table to the parent/first table's filter state.
- Bind or synchronize every filter header in a table to that same table-specific visibility property. `DataGridColumn`/column-header objects do not reliably inherit the visual-tree DataContext. Where the binding is unreliable, follow `OrdersView.UpdateFilterHeadersVisibility`: explicitly set each header's `IsFilterVisible`, run the synchronization when the DataContext is attached, and repeat it from the ViewModel's `PropertyChanged` event for the relevant visibility properties. Detach both event handlers when replacing the DataContext or unloading the view.
- When filters are hidden, the loader must use that table's `*FilterCriteria.Empty`; hidden values must not keep filtering results. Preserve entered criteria separately so showing filters again can reactivate them consistently.
- Route refresh requests to an explicit table target (such as `OrdersPaginationTarget`) rather than refreshing an arbitrary or first control. A filter, sort, selection, or data mutation must refresh only the affected table(s).
- Refresh the affected table with `RefreshAsync()` when its filter or sort changes. Use `ResetAndRefreshAsync()` only for an explicitly justified exceptional transition, such as one where the previous page has no meaningful relationship to the new context.
- Parent-selection changes must clear dependent collections when there is no selection and independently refresh every dependent table. Reset a dependent table only when retaining its page is intentionally invalid for that particular workflow.

## WPF, MVVM, tables, and reusable UI

- Follow MVVM with CommunityToolkit.Mvvm: ViewModels inherit `ViewModelBase`, use `[ObservableProperty]`, `RelayCommand`/`AsyncRelayCommand`, and MediatR. Keep business and persistence logic out of XAML code-behind.
- Code-behind is acceptable for view concerns that WPF does not express cleanly: control events, DataGrid sort glyphs, collecting column-header filter state, pagination refresh wiring, and popup/window behavior.
- Resolve ViewModels through DI. Register new ViewModels in `App.xaml.cs` with a lifetime consistent with neighboring screens. The main shell is singleton; feature/form ViewModels are generally transient.
- Reuse existing controls and styles before adding new ones. Search `Wpf/Controls`, `App.xaml`, and nearby views first.
- Whenever new UI behavior can or should be reused, implement it as a reusable component beside the existing controls in the appropriate `Wpf/Controls` subdirectory. Use that component in every applicable current location instead of leaving one-off copies in views. Reuse it for future occurrences so behavior, styling, accessibility, and fixes remain consistent. Add a view-specific implementation only when the behavior is genuinely unique; if duplication emerges, extract it immediately.
- Use the existing button controls (`CreateButton`, `EditButton`, `DeleteButton`, `RefreshButton`, `FilterToggleButton`, arrow/website buttons) and `DraggablePopup` rather than reproducing their visuals or behavior.
- Use existing inputs (`NumericTextBox`, `DateRangePicker`, `EnumMultiSelectComboBox`, `SearchableComboBox`) and existing converters when they match the requirement.
- Database-backed tables are explicit, read-only `DataGrid`s with `AutoGenerateColumns="False"`, the shared `DataGridColumnHeaderStyle`, explicit bindings/templates, and row action buttons bound back to the DataGrid's ViewModel.
- Table filters belong in the existing grid header controls: `TextFilterColumnHeader`, `NumericFilterColumnHeader`, `DateRangeFilterColumnHeader`, `BooleanFilterColumnHeader`, and `EnumFilterColumnHeader`. Bind `IsFilterVisible` to the DataGrid DataContext and react to the control's `FilterChanged` event.
- New reusable header/filter behavior belongs in `Controls/TableColumns`, not copied into individual views.
- Preserve the application's existing Polish user-facing language, labels, validation messages, and formatting. Code identifiers and technical documentation remain English.
- Match established layout, resources, colors, spacing, popup patterns, and action placement by inspecting the closest existing screen before designing a new one.
- Avoid introducing a new UI framework, navigation pattern, generic repository, event bus, or parallel design system without explicit approval.

## Testing requirements

Every handler change must be covered by unit/use-case tests. This includes new handlers, changed branches, validation/invariant changes, query projections, filters, sort options, pagination behavior, and side effects. A handler change is incomplete until its tests are added or updated and pass.

- Use xUnit v3 and FluentAssertions, following the existing `*_Tests` naming and nearby feature folder structure.
- Derive handler test classes from `BusinessTrackerUnitTestsBase<TSut>`.
- For EF behavior, register the real PostgreSQL test database with `RegisterBusinessTrackingPostgresDatabase(services)`. Tests use Testcontainers and Respawn; Docker must be available.
- Arrange domain data through `Arrange_BusinessTrackerDatabase` and the existing `BusinessTrackerDbContextExtensions.Arrange_*` helpers. Do not manually build large graphs in each test.
- When adding an entity or a commonly needed setup shape, extend the shared `Arrange_*` extensions and maintain both navigation sides consistently.
- Follow Arrange / Act / Assert and FluentAssertions. Cover successful results and all materially distinct failure/edge paths.
- Query handler tests should verify DTO completeness, every supported filter and sort option, zero-based paging, `TotalCount`, and `HasNextPage` where applicable.
- Command handler tests should verify the returned value, persisted database state, related entity/counter changes, and expected behavior when records or stock are missing/insufficient.
- Do not weaken assertions, skip tests, or alter production behavior merely to make a test pass.

After the mandatory migration step for schema changes, run only the tests relevant to the changed behavior and any additional tests for areas that could reasonably have been affected. Select the appropriate test project, feature, class, or filter based on the impact analysis. Run that selected set in one `dotnet test` invocation and one terminal session. Do not fan tests out into multiple independent or parallel invocations: container startup is expensive and separate sessions hide earlier results from the user.

Do not run the entire solution by default. Use the solution-level command only when justified by broad cross-cutting changes, shared infrastructure changes, project/DI changes with wide impact, or genuine uncertainty about the affected scope:

```powershell
dotnet test GenoDev.BusinessTracker.sln
```

Keep any necessary reruns after a fix in that same terminal session. Do not start a second test session while the first is running. In the final report, state which test scope was selected and why; if the full solution was run, state the justification.

Also build the solution when WPF/XAML, DI registration, project references, or compile-time contracts changed:

```powershell
dotnet build GenoDev.BusinessTracker.sln
```

Handler/application tests require Docker for PostgreSQL Testcontainers. If Docker is unavailable or container startup fails for that reason, stop work immediately and ask the user to start Docker. Do not substitute a different database, skip the affected tests, continue to final handoff, or claim partial verification is sufficient. Resume testing only after the user confirms Docker is available. For a different missing prerequisite, report it explicitly and never claim the tests passed.

## Change checklist

Before completing a change, verify all applicable items:

1. The implementation follows the existing layer and CQRS boundaries.
2. Existing shared WPF controls, grid headers, pagination, converters, and styles were reused.
3. Any newly reusable UI behavior was extracted into a shared component and adopted in all applicable locations.
4. Every table in a multi-table view has independent filter, sorting, pagination, and targeted refresh state.
5. Database-backed filtering, sorting, counting, and pagination remain server-side and cancellation-aware.
6. Every changed handler has corresponding focused tests using the shared test fixture and `Arrange_*` extensions.
7. Every schema change has a generated and reviewed migration created before tests were run, and no migration was applied to the user's local database.
8. Impact-based relevant tests pass in one test session; a full-solution test run was used only when justified, and the solution builds when UI/contracts/DI changed.
9. This `AGENTS.md` was updated if the work introduced a durable architectural decision or convention.
