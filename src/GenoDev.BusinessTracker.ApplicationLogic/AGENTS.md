# ApplicationLogic guidance

These rules apply under `GenoDev.BusinessTracker.ApplicationLogic` in addition to the repository guide.

## CQRS, transactions, and validation

- Model reads as `IRequest<T>` queries and writes as commands. Put each request and handler in its feature/use-case folder under `UseCases`.
- `TransactionBehavior` wraps every MediatR handler through `TransactionHelper`; keep it registered as an open behavior so nested requests reuse the ambient transaction.
- `ValidationBehavior` runs immediately inside the transaction behavior. It resolves an optional FluentValidation `IValidator<TRequest>` and throws `RequestValidationException` with structured source/message errors before invalid handlers run.
- Register ApplicationLogic validators by assembly scanning as transient `IValidator<T>` services.
- Every request with meaningful inputs has a validator. Validate shape, ranges, enums, pagination, cross-field consistency, uniqueness, and safely queryable existence. User-facing messages are Polish.
- Handlers retain only race-condition guards and mutation-dependent invariants. Expected guard failures also use `RequestValidationException`, never technical `KeyNotFoundException` or `InvalidOperationException` messages.
- Handlers depend on `IBusinessTrackerDbContext` and other application abstractions, not the concrete context, Infrastructure, or WPF.
- Commands perform the complete mutation, call `SaveChangesAsync(cancellationToken)`, and return only what the caller needs, commonly an ID or no value.

## Queries, lists, and pagination

- List queries use zero-based `PageIndex`, positive `PageSize`, and return `PagedList<T>`.
- Start read-only queries with `AsNoTracking()` unless tracking is deliberate.
- Build one server-side `IQueryable`; apply filters and deterministic ordering before `CountAsync`, `Skip`, and `Take`, then project to DTOs in SQL with `Select` and execute asynchronously.
- `TotalCount` is the filtered count before paging; `PagedList<T>` derives `HasNextPage`.
- Store primary ordering as `IOrderedQueryable`, then add an ascending unique-ID tie-breaker with `ThenByStable(x => x.Id)` or explicit `ThenBy`.
- Feature sorting uses a `*SortBy` enum plus `IsDescending`; enum names align with WPF `SortMemberPath` values.
- Text filters use `WhereContainsAll` or `WhereContainsAllInAny` for escaped PostgreSQL `ILIKE`, AND-separated terms, and quoted phrases. Do not use case-sensitive `Contains` or client-side filtering.
- Numeric filters use `NumericOperator` and `ApplyNumericFilter`. Date ranges, enum selections, and booleans remain nullable so unset filters do not constrain the query.
- Keep filtering, sorting, counting, and paging EF-translatable; never call `AsEnumerable`, `ToList`, or custom in-memory logic first.

## Mutations and services

- Stock adjustments are signed, dated audit entries. Create applies the signed amount; update reverses the old effect before applying the replacement; delete reverses it. Multi-category create requests remain atomic.
- Products use company stock and whole-number amounts. Material variants, packing materials, and fixed assets can target company or private stock.
- Pass cancellation tokens through all asynchronous boundaries and use latest-request-wins semantics where callers can supersede work.
- Register application services in `Extensions/DependencyInjectionExtensions.cs`; MediatR discovers handlers from this assembly.
- The WPF application does not create a DI scope per request. Services holding or depending on transient `IBusinessTrackerDbContext`, including `IItemsService`, must be transient rather than scoped or singleton.

Read the repository's canonical feature guide before changing product images, notes, or mailing behavior.
