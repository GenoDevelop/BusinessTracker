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

All .NET work runs directly on the host machine with the locally installed .NET 10 SDK. Use local `dotnet` for restore, build, test, run, and EF CLI commands. Docker is used only to host PostgreSQL (the development database and disposable Testcontainers databases). Never build, test, run, or execute EF tooling inside a Docker SDK container, and never pull a .NET SDK image as a fallback for a missing local SDK. If the required local SDK is unavailable, report the prerequisite instead of changing the target framework or moving the toolchain into Docker.

The repository's `global.json` opts `dotnet test` into the .NET 10 Microsoft.Testing.Platform (MTP) runner. Keep this setting: the test executable uses xUnit v3's MTP integration and cannot run through the legacy VSTest mode on .NET 10. With MTP, identify inputs explicitly with `--project` or `--solution`.

- `src/GenoDev.BusinessTracker.Domain`: entities and domain enums. It must not depend on UI, persistence, or application use cases.
- `src/GenoDev.BusinessTracker.ApplicationLogic`: CQRS requests, handlers, DTOs, application abstractions, query helpers, and application services. It depends on Domain, not Infrastructure or WPF.
- `src/GenoDev.BusinessTracker.Infrastructure`: EF Core `BusinessTrackerDbContext`, entity configurations, PostgreSQL setup, design-time context factory, and migrations. It implements application abstractions.
- `src/GenoDev.BusinessTracker.Wpf`: composition root, WPF views, MVVM view models, filters, converters, and reusable controls.
- `tests/GenoDev.BusinessTracker.ApplicationLogic.Tests`: unit/use-case tests for handlers and application services.
- `tests/GenoDev.BusinessTracker.TestsUtilities`: shared test fixture, PostgreSQL Testcontainer support, database reset, clocks, and `Arrange_*` builders.
- `utilities/GenoDev.Utilities.Core`: small reusable, business-independent utilities.

Dependency direction is `Wpf -> ApplicationLogic`, `Wpf -> Infrastructure`, `Infrastructure -> ApplicationLogic + Domain`, and `ApplicationLogic -> Domain`. Do not reference WPF or Infrastructure from Domain/ApplicationLogic merely for convenience.

## NuGet dependencies and licensing

- Reuse the framework, existing packages, and repository code before proposing another dependency. Add a new NuGet package only when it has a concrete, justified benefit and the same result cannot reasonably be achieved with what the project already uses.
- **Never add a new direct NuGet dependency without the user's explicit approval.** Before editing a project file or running a restore that introduces it, tell the user the exact package and proposed version, why it is needed, and ask for permission.
- Before requesting approval, verify the package using its official NuGet metadata and, when needed, its official repository. Report the exact license identifier/name and state plainly whether the package is free to use for this project, including commercial use when that can be established. Mention material obligations or restrictions such as attribution, notice preservation, source disclosure, copyleft, paid/commercial terms, or dual licensing.
- Do not infer that a package is free merely because it can be downloaded from NuGet. If the license, ownership, applicable version, or commercial-use terms are missing or ambiguous, say so and do not add the package until the user makes an informed decision.
- If an update to an existing dependency changes its license or introduces materially different terms, treat it like a new dependency and obtain approval first.

## CQRS and application use cases

- All business operations invoked by the UI go through MediatR.
- Every MediatR request handler runs through `TransactionBehavior`, which wraps the complete handler invocation with `TransactionHelper`. Keep the behavior registered as an open pipeline behavior so commands and queries share the same ambient transaction and nested requests reuse it.
- `ValidationBehavior` runs immediately inside `TransactionBehavior`. It resolves an optional FluentValidation `IValidator<TRequest>`, skips validation when none is registered, and throws `RequestValidationException` with structured source/message errors before invoking an invalid request's handler. Register ApplicationLogic validators by assembly scanning as transient `IValidator<T>` services.
- Every command/query with meaningful input properties has a FluentValidation validator. Validate shape, ranges, enums, pagination, cross-field consistency, uniqueness, and safely queryable existence before the handler. Validation messages exposed to users are Polish. Handlers retain only race-condition guards and mutation-dependent invariants; expected failures from those guards also use `RequestValidationException`, never technical `KeyNotFoundException`/`InvalidOperationException` messages.
- Model reads as `IRequest<T>` queries and writes as commands. Put each request and its handler in the appropriate feature/use-case folder under `ApplicationLogic/UseCases`.
- Keep request contracts and result DTOs independent of WPF. Do not expose EF entities to the UI when a purpose-built DTO is appropriate.
- Handlers depend on `IBusinessTrackerDbContext` and other application abstractions, not the concrete context.
- A query handler builds one server-side `IQueryable`, applies filters and sorting, counts it, pages it, projects it to DTOs, and executes it asynchronously with the supplied cancellation token.
- Read-only queries start with `AsNoTracking()` unless tracking is deliberately required.
- Project to DTOs in SQL with `Select`; avoid loading full graphs and mapping them in memory.
- Commands perform the complete business mutation, call `SaveChangesAsync(cancellationToken)`, and return only the result their caller needs (commonly an ID or no value).
- Preserve business invariants and update every related aggregate/counter consistently. Study adjacent handlers before implementing inventory, production, supply, recipe, or order mutations; these flows affect related totals.
- Stock adjustments are persisted as signed, dated audit entries. Creating an entry applies its signed amount, updating one first reverses its old effect and then applies the replacement, and deleting one reverses its effect. A single create request may contain several categories and must remain atomic. Products use only company stock and whole-number amounts; material variants, packing materials, and fixed assets can target company or private stock.
- Pass cancellation tokens through MediatR, EF async operations, pagination loaders, and services.
- Register application services in `ApplicationLogic/Extensions/DependencyInjectionExtensions.cs`; MediatR discovers handlers from the ApplicationLogic assembly.
- The WPF application does not create a DI scope per MediatR request. Services that hold or depend on the transient `IBusinessTrackerDbContext` (including `IItemsService`) must therefore be transient, not scoped or singleton.

## Database and Entity Framework Core

- PostgreSQL is the only supported relational behavior. Do not replace database-dependent tests with EF's in-memory provider.
- `BusinessTrackerDbContext` uses lazy-loading proxies, snake_case naming, assembly-scanned `IEntityTypeConfiguration<T>` mappings, and multiple PostgreSQL schemas. Put mapping rules in `Infrastructure/Configurations`, not in WPF or handlers.
- New entities require the appropriate `DbSet`, entity configuration, application abstraction exposure when needed, relationships/navigation initialization consistent with neighboring entities, and test arrange support.
- Product images are stored as original binary content in PostgreSQL through `ProductImage`. Keep list queries lightweight by projecting only image metadata and load the selected image content with a separate query. Uploads accept JPEG, PNG, GIF, BMP, and TIFF, with limits of 10 MB per image, 20 images and 50 MB per request. Display images from oldest to newest and select the last newly uploaded image after upload. Reuse `ProductImagesPanel` and `ProductImagesPopup`, which coordinates a native resizable and maximizable `ProductImagesWindow` so gallery sizing and movement work correctly across monitors; keep its percentage zoom controls. Management is available only from the Products view; Production, Recipes, and ordered products in Sales open the same gallery read-only, and entry buttons outside Products are disabled when no images exist.
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

The local development PostgreSQL service is defined in `docker-compose.yml` and listens on port 5434. This database service is Docker's only runtime role in normal project development; the WPF application and all .NET commands remain local.

## Lists, server-side pagination, filtering, and sorting

Use the existing end-to-end list pattern rather than inventing per-view alternatives.

### Query side

- List queries use zero-based `PageIndex` and a positive `PageSize` and return `PagedList<T>`.
- Apply all filters and ordering before `CountAsync`, `Skip`, and `Take`.
- `TotalCount` is the count after filtering and before paging. `HasNextPage` is derived by `PagedList<T>`.
- Apply deterministic server-side ordering before paging. Store the result of the primary `OrderBy` as an `IOrderedQueryable`, then finish it with an ascending unique-ID tie-breaker using `ThenByStable(x => x.Id)` (or an explicit `ThenBy(x => x.Id)`). This prevents records with equal primary sort values from moving between pages and makes it impossible to call the shared helper before primary ordering.
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
- Keep UI-triggered loading non-blocking from the user's perspective. `PaginationControl.RefreshAsync()` deliberately yields to the WPF dispatcher at `Background` priority before starting query work and coalesces refreshes queued in the same dispatcher turn so only the newest reaches the database. Preserve this behavior.
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
- Form ViewModels catch `RequestValidationException`, apply it through `ViewModelBase`'s `INotifyDataErrorInfo` support, and keep the editor open. Property errors use the shared red error template and render their Polish message below the control; unassigned validation errors use a warning dialog. Clear previous server-side errors before resubmitting.
- Keep the application-level WPF exception handler registered for dispatcher and unobserved task exceptions. Uncaught validation errors are warning dialogs containing only their Polish validation messages; unexpected exceptions are traced for diagnostics but the UI shows only the generic Polish error message without technical details.
- Code-behind is acceptable for view concerns that WPF does not express cleanly: control events, DataGrid sort glyphs, collecting column-header filter state, pagination refresh wiring, and popup/window behavior.
- Resolve ViewModels through DI. Register new ViewModels in `App.xaml.cs` with a lifetime consistent with neighboring screens. The main shell is singleton; feature/form ViewModels are generally transient.
- Parent ViewModels must not construct form/child ViewModels with `new`. Resolve dependency-only children from `IServiceProvider`; use `ActivatorUtilities.CreateInstance` when the child also requires runtime values such as the selected DTO or parent ID. Lightweight row/input helper ViewModels that only wrap their parent and have no service dependencies are exempt.
- Selection changes, popup closing, and other immediate visual state transitions must complete before dependent data loading begins. Set the visible state first, then schedule/await the refresh. For non-pagination loaders started from property-change callbacks or constructors, call `ViewModelBase.YieldToUiAsync()` before query preparation so WPF can process input, bindings, and rendering.
- Closing a popup must never change any active selection in its owning screen, regardless of whether the popup closes after save or cancel. If closing triggers a collection refresh, capture the selected entity by its stable ID, replace the collection, and restore the refreshed instance while suppressing the transient `SelectedItem = null` callbacks emitted by WPF during `ObservableCollection.Clear()`. Those technical callbacks must not reset dependent tables, details, pagination, or form state. A successful delete is the only exception: clear a selection only when the deleted entity is that exact selected entity; deleting another row must preserve the current selection. Keep popup delete targets in explicitly named `*ToDelete`/`*ToRemove` properties rather than reusing active `Selected*` properties.
- After a successful create operation, automatically select the new entity if it is present on the refreshed active page. Creation commands must return the created entity's stable `Guid`; editor ViewModels report it with `EditorCloseResult.Saved(createdEntityId)`, and the owning list keeps it as a pending preferred selection until a non-cancelled page load successfully replaces the collection. Selection resolution order is: the newly created entity when present, otherwise the previous selection when it is still present, otherwise `null` (including when the new entity is on another page and displaced the previous selection from the active page). Never navigate or reset the main list merely to find the created entity. If selecting the created entity changes a master context, reset and refresh its dependent pagination/details exactly as for a deliberate user selection. Edit saves carry no created ID, cancel uses `EditorCloseResult.Cancelled` and does not refresh, and delete uses `EditorCloseResult.Deleted`.
- Rapidly superseded loads (selection changes, live search, dependent lists) must use cancellation or request versioning with latest-request-wins semantics. Pass the token through MediatR and discard stale results. Do not use `Task.Run` with EF/MediatR services merely to move work off the dispatcher, and do not allow fire-and-forget loads to accumulate unobserved work.
- Master-detail and dependent-data views must preserve visual continuity while a new selection loads. Keep the previous detail panel/table visible, load the new selection asynchronously, and replace the displayed data atomically only after the newest request succeeds. Do not set the bound detail object to `null`, collapse the section, show an empty-state placeholder, or otherwise cause layout flicker merely to indicate loading. If stale actions must be prevented during the transition, temporarily disable hit testing or commands without hiding or rebuilding the section. `SuppliesView` and the multi-table sales view are reference implementations of this behavior.
- Reuse existing controls and styles before adding new ones. Search `Wpf/Controls`, `App.xaml`, and nearby views first.
- Whenever new UI behavior can or should be reused, implement it as a reusable component beside the existing controls in the appropriate `Wpf/Controls` subdirectory. Use that component in every applicable current location instead of leaving one-off copies in views. Reuse it for future occurrences so behavior, styling, accessibility, and fixes remain consistent. Add a view-specific implementation only when the behavior is genuinely unique; if duplication emerges, extract it immediately.
- Use the existing button controls (`CreateButton`, `EditButton`, `DeleteButton`, `RefreshButton`, `FilterToggleButton`, arrow/website buttons) and `DraggablePopup` rather than reproducing their visuals or behavior.
- Use existing inputs (`NumericTextBox`, `DateRangePicker`, `EnumMultiSelectComboBox`, `SearchableComboBox`) and existing converters when they match the requirement.
- Multiline description inputs in popup forms use the shared `ResizableTextBox`. Its bottom-right resize grip changes the control's width and height while respecting min/max bounds; horizontal resizing tracks the nearest fixed-width popup content host in both directions without shrinking that host below its declared base width. Forms whose popup should grow with the editor vertically must not wrap the editor in a fixed-height scrolling viewport. Dimensions remain local to that popup control instance and are never written to persistent application settings.
- Display-only multiline descriptions use the shared opaque `ReadOnlyDescriptionBorderStyle`; do not place readable data content on `GlassSurfaceBrush` or recreate description borders in individual detail views.
- Database-backed tables are explicit, read-only `DataGrid`s with `AutoGenerateColumns="False"`, the shared `DataGridColumnHeaderStyle`, explicit bindings/templates, and row action buttons bound back to the DataGrid's ViewModel.
- Table filters belong in the existing grid header controls: `TextFilterColumnHeader`, `NumericFilterColumnHeader`, `DateRangeFilterColumnHeader`, `BooleanFilterColumnHeader`, and `EnumFilterColumnHeader`. Bind `IsFilterVisible` to the DataGrid DataContext and react to the control's `FilterChanged` event.
- New reusable header/filter behavior belongs in `Controls/TableColumns`, not copied into individual views.
- All application tables use `ConfigurableDataGrid` for user-controlled column visibility and ordering. Give every grid a stable, unique `LayoutKey`. A column's stable persistence key is its explicit technical `ColumnKey`, falling back to its stable `SortMemberPath`; columns without a stable sort member must declare `ColumnKey`, and `ColumnTitle` must be supplied when the Polish title cannot be inferred from the header. Keys are persistence contracts and must not be renamed merely because a label changes. Dragging headers and dragging entries in the visibility menu both reorder columns; the menu stays open while visibility or order is changed. These interactions must remain compatible with server-side sorting and existing filter headers. Filter header controls implement `IColumnFilterHeader` so the grid can report whether a visibility change affects an active filter. A hidden column's filter must not constrain the query: react to `ColumnVisibilityChanged`, retain the entered header value, and always rebuild stored criteria using only visible columns. Refresh only that table's pagination when `AffectsActiveFilter` is true and that table's filter panel is enabled; when filters are globally hidden, update criteria silently so a stale hidden-column filter cannot reactivate later. Persisted layouts ignore removed/unknown/duplicate keys, retain defaults for new or ambiguous columns, normalize saved ordering, and restore the declared layout if the saved state is corrupt or would hide every configurable column.
- Preserve the application's existing Polish user-facing language, labels, validation messages, and formatting. Code identifiers and technical documentation remain English.
- Match established layout, resources, colors, spacing, popup patterns, and action placement by inspecting the closest existing screen before designing a new one.
- The application-wide visual palette and implicit control styles live in `Wpf/Themes/ModernTheme.xaml`. Reuse its semantic brushes (surface, text, accent, border, danger, success, and warning) from views and shared controls instead of hard-coding presentation colors; add a new token there only when an existing semantic role does not fit. Supply workflow statuses use their dedicated high-contrast brushes (`SupplyStatusNewBrush`, `SupplyStatusOrderedBrush`, and `SupplyStatusReceivedBrush`) so the neutral, blue, and green states remain easy to distinguish even in small indicators; do not substitute the general teal accent brush for the ordered state. Order statuses reuse those same brushes for new, processing, and delivered respectively, while shipped uses the dedicated magenta `OrderStatusShippedBrush`.
- Scrollbars use the implicit application-wide style from `Wpf/Themes/ModernTheme.xaml`, including scrollbars inside grids, editors, lists, and popups. Keep their track compact and their hover/drag feedback within the scrollbar bounds instead of defining view-local scrollbar chrome.
- Resizable master-detail layouts use the shared `VerticalPanelGridSplitterStyle`: it keeps an 8-pixel transparent hit target while drawing only a 1-pixel divider. Do not render the full hit area as an opaque divider or copy the splitter template into individual views.
- Use `TopNavigationTabControlStyle` for the main application navigation, `SideNavigationTabControlStyle` for module navigation rails, and the default tab styles for detail tabs. Tab surfaces keep a symmetric internal margin and use layout rounding/device-pixel snapping so antialiased rounded corners are never rendered on the template boundary or rasterized asymmetrically. Navigation item and rail dimensions must leave room for the complete icon-and-label header at the supported font size. Reserve translucent glass surfaces for navigation, toolbars, popups, and summary cards; keep data grids and input content opaque and high-contrast. Shared action icons use outline vector geometry with hover and focus effects contained inside the control bounds; never scale the whole control for interaction feedback.
- Keep list screens compact through the shared theme: table headers use a 34-pixel minimum height and automatic height so visible filter controls expand the header downward, rows use a 34-pixel minimum height, and both headers and cells use 2 pixels of base horizontal padding through the shared styles. Shared filter-header title rows add a 5-pixel horizontal content inset; when a table's cell templates use that same readable inset, apply it uniformly to every textual column and matching plain-text header so header and cell text share one axis. Do not mix inset and edge-aligned content within one table. The implicit `DataGrid` style supplies `DataGridColumnHeaderStyle` to every table, including working tables inside editors. That shared header template owns column separators, hover/pressed feedback, resize grippers, and ascending/descending glyphs driven by `DataGridColumn.SortDirection`; sort glyphs overlay the title row and must not consume width from filter editors. The glyph host follows the vertical placement and measured height of the header content, keeping the glyph aligned with the title whether filters are hidden or visible. Do not recreate those visuals per view. Edit, delete, and website actions use the shared `ActionIconButton` dimensions; do not apply smaller local `Width`/`Height` values because that can clip their vector icons.
- `PaginationControl` uses the shared compact page-size ComboBox style in both layouts. Master-list title/action rows that need a contained surface use the compact `MasterListToolbarBorderStyle` together with its scoped icon-button styles; keep that surface scoped to the toolbar above the list rather than wrapping filters, data content, or pagination with it.
- Keep single-line inputs compact and vertically centered through the shared input styles. Every multiline editor (`AcceptsReturn=True`, `RichTextBox`, or `ResizableTextBox`) is top-aligned; `ResizableTextBox` forwards its shared `Padding` and `VerticalContentAlignment` into its inner editor. Search fields created dynamically by `SearchableComboBox` use the dedicated compact popup-search style rather than the full-height form-field padding. Table-header editors use the shared `CompactFilter*` styles; reusable compound inputs such as `DateRangePicker` and `EnumMultiSelectComboBox` expose and use `IsCompact` in filter headers instead of being locally clipped to a smaller height.
- Shared `ComboBox` templates forward the selection-box template and item-template selector so DTO display definitions remain active after selection. `DatePicker` and `DateRangePicker` use the shared modern calendar surface and transparent popup chrome; do not fall back to the platform's default black popup host.
- Avoid introducing a new UI framework, navigation pattern, generic repository, event bus, or parallel design system without explicit approval.

### Notes and rich text

- Note content is persisted as an RTF string in `Note.ContentRtf`. Treat it as opaque outside WPF; conversion between `RichTextBox`/`FlowDocument` and RTF belongs to the view layer.
- Keep the paged note list lightweight by projecting only note identity and name. Load the selected note's rich content through a separate details query with latest-request-wins cancellation.
- Creating a note sets only its name and empty content. Editing the rich content is a separate explicit-save operation; formatting changes must never be persisted merely because selection or focus changed.
- Rich-text formatting controls must preserve the editor's focus/selection and reflect the format at the current caret or selection (including active toggles, font size, and text color), so toolbar interaction never obscures what will be changed.
- Changing the active note while its editor is dirty requires a save/discard/cancel decision. Apply this guard to direct row selection and indirect selection changes such as paging, filtering, sorting, or selecting a newly created note; cancel must keep the current editor content and logical selection intact.

## Testing requirements

Every handler change must be covered by unit/use-case tests. This includes new handlers, changed branches, validation/invariant changes, query projections, filters, sort options, pagination behavior, and side effects. A handler change is incomplete until its tests are added or updated and pass.

- Use xUnit v3 and FluentAssertions, following the existing `*_Tests` naming and nearby feature folder structure.
- Derive handler test classes from `BusinessTrackerUnitTestsBase<TSut>`.
- In test code, pass `TestContext.Current.CancellationToken` to every asynchronous handler, service, EF, arrange, or other API that accepts a cancellation token. Do not use `default`, `CancellationToken.None`, or omit an optional token in ordinary tests. A dedicated `CancellationTokenSource` is appropriate only when cancellation behavior itself is under test; dispose it correctly.
- For EF behavior, register the real PostgreSQL test database with `RegisterBusinessTrackingPostgresDatabase(services)`. Tests use Testcontainers and Respawn; Docker must be available.
- Arrange domain data through `Arrange_BusinessTrackerDatabase` and the existing `BusinessTrackerDbContextExtensions.Arrange_*` helpers. Do not manually build large graphs in each test.
- When adding an entity or a commonly needed setup shape, extend the shared `Arrange_*` extensions and maintain both navigation sides consistently.
- Follow Arrange / Act / Assert and FluentAssertions. Cover successful results and all materially distinct failure/edge paths.
- Query handler tests should verify DTO completeness, every supported filter and sort option, zero-based paging, `TotalCount`, and `HasNextPage` where applicable.
- Command handler tests should verify the returned value, persisted database state, related entity/counter changes, and expected behavior when records or stock are missing/insufficient.
- Do not weaken assertions, skip tests, or alter production behavior merely to make a test pass.

After the mandatory migration step for schema changes, run only the tests relevant to the changed behavior and any additional tests for areas that could reasonably have been affected. Select the appropriate test project, feature, class, or filter based on the impact analysis. Run that selected set in one `dotnet test` invocation and one terminal session, for example `dotnet test --project tests/GenoDev.BusinessTracker.ApplicationLogic.Tests/GenoDev.BusinessTracker.ApplicationLogic.Tests.csproj`. Do not fan tests out into multiple independent or parallel invocations: container startup is expensive and separate sessions hide earlier results from the user.

Do not run unrelated tests merely because some code changed. In particular, changes confined to WPF/MVVM code—views, XAML, code-behind, ViewModels, converters, controls, styles, bindings, or UI-only DI registrations—cannot change handler behavior and therefore do not justify running ApplicationLogic handler tests. Verify such changes with a solution/WPF build and the most appropriate UI-focused inspection or manual check available. Run handler tests only when handlers, application services, application contracts/query semantics, shared handler dependencies, persistence behavior, or test infrastructure that can affect those tests changed. If a change spans both UI and ApplicationLogic, test only the affected ApplicationLogic behavior and build the WPF project/solution.

Do not run the entire solution by default. Use the solution-level command only when justified by broad cross-cutting changes, shared infrastructure changes, project/DI changes with wide impact, or genuine uncertainty about the affected scope:

```powershell
dotnet test --solution GenoDev.BusinessTracker.sln
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
8. Impact-based relevant tests pass in one test session; unrelated handler tests were not run for WPF/MVVM-only changes, a full-solution test run was used only when justified, and the solution builds when UI/contracts/DI changed.
9. Tests pass `TestContext.Current.CancellationToken` through cancellation-aware APIs unless cancellation behavior is explicitly under test.
10. Every newly proposed NuGet dependency was approved by the user after its purpose, exact version, license, and usage terms were disclosed.
11. This `AGENTS.md` was updated if the work introduced a durable architectural decision or convention.
