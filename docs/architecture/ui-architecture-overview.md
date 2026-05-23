# UI Architecture Direction

Status: Draft

This document captures the intended direction for the next UI architecture iteration. It is deliberately critical: the main goal is to avoid moving the current complexity into new type names.

## Scope

The scope is the shell UI that hosts browsable file content, including:

- Tabs and panes
- The active folder browser concept
- Folder layout views
- Navigation stacks
- Command routing and hotkeys
- Shared UI surfaces such as the toolbar, address bar, status bar, preview pane, details pane, and sidebar

This document does not define storage provider APIs or low-level file operation behavior.

## Current Pressure Points

The current UI has several responsibilities that are hard to isolate:

- `ShellPanesPage` determines the active pane and also participates in tab content behavior.
- `BaseShellPage` owns frame navigation, shell view model state, toolbar state, and loading state.
- Layout pages own selection, focus, rectangle selection, context menu behavior, and view-specific gestures.
- Commands are globally available through `ICommandManager`, but individual actions read mutable context services to decide whether they can execute.
- Column view changes what "active browser" means because the active command target can be an inner column rather than the outer pane.

The next architecture should reduce these overlaps while still allowing `FolderBrowser`
to be the top-level browsable `UserControl`. The key is that layout-specific state and
command-specific state should not all be pushed into that control.

## Design Goals

- Let `FolderBrowser` own one browsable surface, including current location, layout,
  selection identity, and navigation history.
- Use `PaneHost` as the shell integration point that owns browser lifetime and pane
  activation.
- Keep layout-specific UI details inside folder views, not in `FolderBrowser`.
- Make the active browsing target explicit and observable.
- Keep command metadata global, stable, and generated.
- Route command execution through the currently active scope.
- Split folder layout contracts by capability instead of forcing every layout into one large interface.
- Support Home, Search, Tags, Recycle Bin, archive, FTP, cloud providers, and virtual folders without assuming a path string is enough.
- Make layout switching preserve user state where practical.
- Keep migration possible from the current `ShellPanesPage`, `BaseShellPage`, and layout page structure.

## Proposed Concepts

### FolderBrowser

`FolderBrowser` is the browsable `UserControl` hosted by `PaneHost`. Shell surfaces
should host a pane host rather than inserting `FolderBrowser` directly.

It owns current folder location, current layout, navigation history, selection identity,
load lifecycle, cancellation, and folder capabilities.

It should host the active folder view but avoid owning layout-specific behavior such as
details columns, column widths, rectangle selection visuals, or inline rename controls.

### FolderView

A folder view renders the current folder in one layout. Details, grid, list, cards,
gallery, and columns are layout implementations.

Layouts should implement small capability interfaces such as selection, rename, scroll, grouping, sorting, or details columns only when they support those behaviors.

`ColumnsFolderView` is a compound folder view. It should own a flat collection of
columns rather than recursively nesting another `ColumnsFolderView`.

### CommandRegistry

`CommandRegistry` contains stable command codes, labels, glyphs, default hotkeys, categories, and grouping metadata. It is a good fit for source generation.

It should not own per-tab or per-pane state.

### CommandRouter

`CommandRouter` resolves commands against the active scope. It handles hotkey precedence, focus-sensitive routing, and execution context creation.

It should receive or construct a command context at execution time instead of letting command instances permanently subscribe to arbitrary UI services.

### Context Projections

Context projections expose derived state for specific UI surfaces. Examples include content page context, display context, sidebar context, multitasking context, and window context.

They should derive from the active `FolderBrowser` and focused surface where possible,
not compete as separate sources of truth.

## Architectural Rules

1. `FolderBrowser` is the durable owner of one browsable surface.
2. `FolderView` must not own browser navigation history.
3. The active browser must be explicit. Do not infer it by walking the visual tree except as a migration bridge.
4. Command executability must be synchronous from the UI perspective. Expensive checks can update cached state asynchronously.
5. A command should re-check the current context when executed. A stale enabled button must not be enough to perform a destructive operation.
6. A layout view should expose only capabilities it actually supports.
7. Navigation entries must preserve provider identity and query state, not only a display path or parsable path.
8. Column view must be designed as a first-class active-scope case, not patched after the pane model is finished.

## Migration Shape

The likely migration path is incremental:

1. Introduce `PaneHost` as the shell-level host for browsable panes.
2. Introduce `FolderBrowser` as the browsable `UserControl` owned by `PaneHost`.
3. Move current location, layout mode, selection identity, and navigation history into `FolderBrowser`.
4. Host one active `FolderView` inside `FolderBrowser`.
5. Wrap layout behavior behind capability interfaces.
6. Add an active-browser service and route a small set of commands through it.
7. Replace visual-tree active target discovery with explicit focus and activation events.
8. Retire compatibility context projections only after all commands use the router model.

## Main Risks

- `FolderBrowser` becomes a larger version of `BaseShellPage`.
- A per-browser command manager duplicates command instances and leaks event subscriptions as tabs and panes are opened.
- A flags-based command context cannot express real UI state and slowly accumulates exceptions.
- A single `IFolderView` interface becomes too broad and makes Home, Gallery, and Columns implement fake behavior.
- Path-only navigation fails for virtual folders, provider-specific folders, and search results.

