# Test guidance

These rules apply to repository tests. Production handler/application changes must also follow them.

- Use xUnit v3 and FluentAssertions with existing `*_Tests` naming and neighboring feature structure.
- Derive handler test classes from `BusinessTrackerUnitTestsBase<TSut>`.
- Pass `TestContext.Current.CancellationToken` to every asynchronous handler, service, EF, arrange, or other API accepting a token. Use a disposed dedicated `CancellationTokenSource` only when cancellation itself is under test.
- Use the real PostgreSQL test database through `RegisterBusinessTrackingPostgresDatabase(services)`. Tests use Testcontainers and Respawn; never substitute EF's in-memory provider.
- Arrange data through `Arrange_BusinessTrackerDatabase` and existing `Arrange_*` extensions. Extend shared arrange helpers for new entities/common shapes and maintain both navigation sides.
- Follow Arrange / Act / Assert. Cover success plus every materially distinct validation, failure, edge, race guard, and side-effect path.
- Query tests verify DTO completeness, every supported filter/sort option, zero-based paging, `TotalCount`, and `HasNextPage` where applicable.
- Command tests verify returned values, persisted state, related aggregate/counter changes, and missing/insufficient-record behavior.
- For a generated-key dependent added to an already tracked aggregate, test the exact mixed update-plus-add scenario.
- Do not weaken assertions, skip tests, or change production behavior merely to make tests pass.

After any required migration, select tests by impact and run them in one `dotnet test` invocation and terminal session, for example:

```powershell
dotnet test --project tests/GenoDev.BusinessTracker.ApplicationLogic.Tests/GenoDev.BusinessTracker.ApplicationLogic.Tests.csproj
```

Keep reruns after fixes in that same session; do not start parallel test sessions. WPF-only changes do not justify ApplicationLogic handler tests. Use solution-wide testing only for broad cross-cutting/shared-infrastructure changes or genuine scope uncertainty:

```powershell
dotnet test --solution GenoDev.BusinessTracker.sln
```

In the final report, state the selected scope and why. If Docker is unavailable for required Testcontainers tests, stop and ask the user to start it rather than substituting, skipping, or claiming partial verification.
