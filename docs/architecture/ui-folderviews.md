# UI FolderViews

Status: Draft

`FolderView` is the layout-specific UI hosted by `FolderBrowser`. Each layout has its own view type, for example `DetailsFolderView`, `GridFolderView`, `ListFolderView`, and `ColumnsFolderView`.

`FolderView` should render and interact with the current folder. It should not own browser navigation history.

## Relationship To FolderBrowser

The intended hierarchy is:

Shell surfaces should host `PaneHost`; `PaneHost` should host `FolderBrowser`.
`SidebarView.Content` should not receive a `FolderBrowser` directly.

```text
Shell surface
  -> PaneHost
      -> FolderBrowser
          -> active FolderView
```

For normal layouts:

```text
FolderBrowser
  -> DetailsFolderView
```

or:

```text
FolderBrowser
  -> GridFolderView
```

For columns layout:

```text
FolderBrowser
  -> ColumnsFolderView
      -> ColumnFolderView
      -> ColumnFolderView
      -> ColumnFolderView
```

`FolderBrowser` sees `ColumnsFolderView` as one folder view. The individual columns are internal to `ColumnsFolderView`.

## Minimal Contract

Each folder view can be a `UserControl` and implement a small interface.

```csharp
public interface IFolderView
{
	FrameworkElement Element { get; }

	Task AttachAsync(FolderViewContext context);
	void Detach();

	FolderViewState? CaptureState();
	void RestoreState(FolderViewState state);
}
```

The `Element` is usually the view itself:

```csharp
public sealed partial class DetailsFolderView : UserControl, IFolderView
{
	public FrameworkElement Element => this;
}
```

## FolderViewContext

`FolderViewContext` is passed from the browser to the view when the view is attached.

```csharp
public sealed record FolderViewContext(
	FolderBrowser Browser,
	FolderLocation Location,
	CancellationToken CancellationToken);
```

It can later grow to include item sources, property providers, layout preferences, or services. The context should not become a place where the view stores navigation history.

## View State

View state is optional state that can be restored after navigation or layout switching.

```csharp
public sealed record FolderViewState(
	IReadOnlyList<ItemLocation> SelectedItems,
	ItemLocation? FocusedItem,
	ItemLocation? ScrollTarget,
	double? ScrollOffset);
```

Not every view needs to support every field. A view should ignore state it cannot restore.

## Capability Interfaces

Do not put every possible operation on `IFolderView`. Use capability interfaces.

```csharp
public interface ISelectableFolderView
{
	IReadOnlyList<ItemLocation> SelectedItems { get; }
	void Select(ItemLocation item);
	void Deselect(ItemLocation item);
	void ClearSelection();
}
```

```csharp
public interface IScrollableFolderView
{
	bool ScrollTo(ItemLocation item);
	bool ScrollToTop();
	bool ScrollToBottom();
}
```

```csharp
public interface IRenameFolderView
{
	bool TryStartRename(ItemLocation item);
}
```

```csharp
public interface IDetailsColumnsFolderView
{
	IReadOnlyList<DetailsColumnState> VisibleColumns { get; }
	event EventHandler<DetailsColumnChangedEventArgs> ColumnsChanged;
}
```

For example, `DetailsFolderView` can implement details column behavior, while `GridFolderView` does not need to fake it.

## Normal Layout Views

Normal layouts should be simple:

- `DetailsFolderView`
- `GridFolderView`
- `ListFolderView`
- `TilesFolderView`
- `ContentFolderView`
- `GalleryFolderView`

These views render the folder items with one layout. They may implement selection, scrolling, rename, drag and drop, context menu targeting, and layout-specific keyboard behavior.

They should not create browser history entries directly. Instead, item invocation should be reported to the browser.

```csharp
public event EventHandler<FolderItemInvokedEventArgs>? ItemInvoked;
```

When a folder is invoked, `FolderBrowser` decides whether that is navigation, new tab, new pane, or another action.

## ColumnsFolderView

`ColumnsFolderView` is a compound folder view. It should not recursively contain another `ColumnsFolderView`.

Recommended structure:

```text
ColumnsFolderView
  -> horizontal host
      -> ColumnFolderView
      -> ColumnFolderView
      -> ColumnFolderView
```

`ColumnsFolderView` owns the column collection and active column index.

```csharp
public sealed partial class ColumnsFolderView : UserControl, IFolderView
{
	public ObservableCollection<ColumnEntry> Columns { get; } = [];
	public int ActiveColumnIndex { get; private set; }

	private Task OpenChildColumnAsync(int parentColumnIndex, FolderLocation location);
	private void RemoveColumnsAfter(int columnIndex);
}
```

Each column is represented by state:

```csharp
public sealed class ColumnEntry
{
	public required FolderLocation Location { get; init; }
	public ObservableCollection<FolderItem> Items { get; } = [];
	public ItemLocation? SelectedItem { get; set; }
	public double Width { get; set; }
	public bool IsLoading { get; set; }
}
```

Each visual column is a smaller control:

```csharp
public sealed partial class ColumnFolderView : UserControl
{
	public ColumnEntry Column { get; set; }

	public event EventHandler<ColumnItemInvokedEventArgs>? ItemInvoked;
	public event EventHandler<ColumnSelectionChangedEventArgs>? SelectionChanged;
}
```

`ColumnFolderView` is not a full browser. It is one visual column.

## Column Behavior

When a folder is selected or invoked in column `i`:

```text
Remove columns after i
Add a new column for the selected folder at i + 1
Set ActiveColumnIndex to i + 1
Update FolderBrowser.CurrentLocation as appropriate
```

When a file is selected in column `i`:

```text
Remove columns after i
Set ActiveColumnIndex to i
Update FolderBrowser.Selection
Do not add a child column
```

This keeps branch state centralized in `ColumnsFolderView`.

## Why Not Recursive ColumnsFolderView

Avoid this structure:

```text
ColumnsFolderView
  -> ColumnFolderView
  -> ColumnsFolderView
      -> ColumnFolderView
      -> ColumnsFolderView
```

It creates several problems:

- The active column is hard to define.
- Selection state is split across nested views.
- Removing columns to the right becomes recursive cleanup.
- Command routing has to decide which nested view is the target.
- Horizontal scrolling and width persistence are harder.
- Cancellation and disposal are more likely to leak.
- Restoring view state requires reconstructing a nested tree.

A flat column list is easier to reason about and easier to target from commands.

## Columns And Navigation History

`FolderBrowser` owns committed navigation history. `ColumnsFolderView` owns the visible branch.

Open question: whether single-click expansion in columns should push a browser history entry.

Two possible policies:

- Every folder expansion updates history. This is simpler but may create noisy back stacks.
- Only explicit navigation commits history. This is quieter but requires a clearer definition of explicit navigation.

Regardless of the policy, history entries should remain in `FolderBrowser`, not in individual columns.

## Rules

- Do not make folder views own back and forward stacks.
- Do not store selected item containers as durable state.
- Do not force every layout to implement details columns, rename, or grouping.
- Do not recursively nest `ColumnsFolderView`.
- Do not make `ColumnFolderView` a full browser.
- Do not let view-specific events accumulate in `FolderBrowser` when a capability interface or service would be clearer.

