# ConfigurableDataGrid guidance

- Every grid has a stable unique `LayoutKey`.
- A column's persistence key is explicit `ColumnKey`, falling back to stable `SortMemberPath`. Declare `ColumnKey` when no stable sort member exists and `ColumnTitle` when the Polish title cannot be inferred. Keys are contracts and do not change with labels.
- Dragging headers or visibility-menu entries reorders columns; the menu remains open during visibility/order edits. Preserve compatibility with server-side sorting and filter headers.
- Filter headers implement `IColumnFilterHeader`. A hidden column retains its entered value but never filters; report whether visibility changed an active filter so callers refresh only the affected enabled table.
- Persisted layouts ignore unknown/removed/duplicate keys, merge new columns at their declared default positions, normalize ordering, and restore defaults when corrupt or when every configurable column would be hidden.
- Every grid declares the row object's technical ID column first. It uses key `Id`, is not server-sortable, is collapsed by default, and remains user-revealable/reorderable.
