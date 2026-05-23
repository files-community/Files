# UI FolderBrowser

Status: Draft

`FolderBrowser` is the browsable UI surface hosted by `PaneHost`. It is a `UserControl`, but shell surfaces should not place it directly. They should host a `PaneHost`, and `PaneHost` should own the browser instance.

This document intentionally treats `FolderBrowser` as a UI control. The current target is not a separate non-UI browsing session object. If we later extract a session object, it should be an implementation detail, not a prerequisite for the initial architecture.

## Responsibilities

`FolderBrowser` owns the state of one browsable surface:

- Current folder location
- Current layout mode
- Current folder view
- Navigation history
- Selection identity
- View state capture and restore
- Folder capabilities exposed to commands and shared UI
- Load and navigation cancellation

It should not own layout-specific UI details such as details columns, grid item containers, column widths, rectangle selection visuals, or inline rename controls.

## Hosting

`FolderBrowser` is intended to be inserted by `PaneHost`:

```xml
<pane:PaneHost>
	<local:FolderBrowser x:Name="Browser" />
</pane:PaneHost>
```

Shell surfaces should host `PaneHost`, not `FolderBrowser` directly:

```xml
<local:ShellSurface>
	<pane:PaneHost />
</local:ShellSurface>
```

The browser remains an implementation detail of the pane host. This gives the app one place to manage pane activation, pane chrome, browser lifetime, and future split-pane behavior.

Conceptually, the active `FolderView` is placed into the browser content area.

If `FolderBrowser` needs no chrome of its own, its `Content` can be the active view. If it needs loading, error, overlay, or transition UI, use an internal `ContentControl` such as `ViewHost` and keep external code from replacing the root content directly.

```xml
<UserControl ...>
	<Grid>
		<ContentControl x:Name="ViewHost" />
	</Grid>
</UserControl>
```

## PaneHost Relationship

`PaneHost` is the shell integration point. It owns the browser instance and any pane-level behavior around it.

`PaneHost` should own:

- Browser lifetime
- Pane activation and focus state
- Pane chrome, if any
- Split-pane composition
- The active browser reference exposed to commands and shared UI

`FolderBrowser` should not need to know whether it is hosted in a tab, split pane, sidebar area, or another shell surface.

## Shape

The initial shape can be straightforward:

```csharp
public sealed partial class FolderBrowser : UserControl
{
	private readonly NavigationHistory history = new();
	private readonly IFolderViewFactory viewFactory;
	private CancellationTokenSource? navigationCts;

	public FolderLocation CurrentLocation { get; private set; }
	public FolderLayoutMode LayoutMode { get; private set; }
	public IFolderView? CurrentView { get; private set; }
	public SelectionState Selection { get; } = new();
	public FolderCapabilities Capabilities { get; private set; }

	public bool CanNavigateBack => history.CanGoBack;
	public bool CanNavigateForward => history.CanGoForward;

	public Task NavigateAsync(FolderLocation location, NavigationOptions options);
	public Task NavigateBackAsync();
	public Task NavigateForwardAsync();
	public Task NavigateUpAsync();
	public Task ChangeLayoutAsync(FolderLayoutMode layoutMode);
}
```

The important point is that `FolderBrowser` owns browser state, while `FolderView` owns only the active layout UI.

## Navigation History

Navigation history belongs to `FolderBrowser`, not to individual folder views.

History entries should not store UI instances. They should store enough information to restore a location and, optionally, view state.

```csharp
public sealed record NavigationEntry(
	FolderLocation Location,
	FolderLayoutMode LayoutMode,
	FolderViewState? ViewState);
```

The minimal first version can omit `ViewState`:

```csharp
public sealed record NavigationEntry(
	FolderLocation Location,
	FolderLayoutMode LayoutMode);
```

This keeps the model simple while leaving room for selection and scroll restoration later.

## Folder Location

`FolderLocation` should not be only a path string. It needs to represent normal paths and non-path locations.

Examples:

- File system path
- Home
- Search results
- Tags
- Recycle Bin
- Archive folder
- Cloud provider folder
- FTP or SFTP folder
- Other virtual folders

Suggested shape:

```csharp
public sealed record FolderLocation(
	string ProviderId,
	string Id,
	string? DisplayPath,
	string? DisplayName);
```

The exact fields can change, but command routing and history should not depend on path strings alone.

## Navigation Flow

A normal navigation should follow this flow:

1. Capture state from the current view.
2. Push the current location into history when requested.
3. Cancel any pending navigation or load operation.
4. Resolve the target folder.
5. Select a folder view for the target folder and layout mode.
6. Attach the view with a `FolderViewContext`.
7. Place the view into the browser content area.
8. Restore view state when available.
9. Notify shared UI and commands that browser state changed.

Example:

```csharp
public async Task NavigateAsync(FolderLocation location, NavigationOptions options)
{
	var previousState = CurrentView?.CaptureState();

	if (options.CommitToHistory && CurrentLocation is not null)
		history.Push(new NavigationEntry(CurrentLocation, LayoutMode, previousState));

	navigationCts?.Cancel();
	navigationCts = new CancellationTokenSource();

	var nextView = viewFactory.Create(location, LayoutMode);
	await nextView.AttachAsync(new FolderViewContext(this, location, navigationCts.Token));

	CurrentView?.Detach();
	CurrentLocation = location;
	CurrentView = nextView;

	ViewHost.Content = nextView.Element;

	if (options.RestoreState is not null)
		nextView.RestoreState(options.RestoreState);
}
```

## Selection

`FolderBrowser` should own selection identity, not selected item containers.

The current view may own visual selection, but it should report selection changes back to the browser. When the layout changes, the browser can pass selected item identities to the next view.

```csharp
public sealed class SelectionState
{
	public IReadOnlyList<ItemLocation> Items { get; }

	public void SetItems(IReadOnlyList<ItemLocation> items);
	public void Clear();
}
```

This avoids losing selection only because a layout view was replaced.

## Commands

Commands should target the active `FolderBrowser`.

Examples:

- Back and forward use `FolderBrowser.NavigationHistory`.
- Rename uses `FolderBrowser.Selection` and the current view's rename capability.
- Delete uses `FolderBrowser.Selection` and folder capabilities.
- Layout commands call `FolderBrowser.ChangeLayoutAsync`.

This means command scope can start with the active browser and then inspect the current view only for view-specific capabilities.

## Open Questions

- Whether column single-click expansion should push entries into browser history.
- How much view state should be captured in the first version.
- Whether browser state should later be extracted into a non-UI object for tests.
- How Home and Search should expose selection when they are composed from multiple groups.

## Rules

- Do not store `FolderView` instances in navigation history.
- Do not make `FolderView` own back and forward stacks.
- Do not let external code replace the browser view without going through navigation or layout APIs.
- Do not require path strings for every browsable location.
- Do not make command handlers depend on a concrete view type when a capability interface is enough.

