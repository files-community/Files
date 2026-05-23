# UI Command Routing

Status: Draft

The command architecture should separate stable command metadata from active UI state. Commands should be discoverable and bindable, but their execution target should be resolved at the time they run.

## Problem Statement

A command system based on a command manager per UI area is attractive, but it does not model the real execution constraints of Files.

The command target can depend on:

- The focused pane, tab, or column
- Whether a text box, dialog, flyout, rename box, or list view has focus
- The current folder type
- Selection count and selected item types
- Recycle Bin, Home, Search, Tags, archive, or cloud-drive state
- Current permissions and provider capabilities
- Whether a load, rename, drag, or file operation is already in progress

An enum such as `Browser`, `Sidebar`, or `Tabs` is not expressive enough for this. It can be useful as metadata, but it should not be the main executability model.

## Proposed Layers

### Command Metadata

Command metadata is stable and can be generated:

- Command code
- Label
- Extended label
- Description
- Glyph
- Default hotkeys
- Category
- Toolbar and menu grouping
- Whether the command is intended to appear in global customization UI

This metadata should be cheap, immutable, and independent of the active tab or pane.

### Command Handler

A command handler performs the operation. It receives a request object that contains the current command context.

Handlers should avoid long-lived subscriptions to UI services. If a handler needs state, it should read it from the request or from scoped services exposed by the request.

Example shape:

```csharp
public sealed record CommandRequest(
	CommandCode Code,
	ICommandContext Context,
	object? Parameter,
	CancellationToken CancellationToken);

public interface ICommandHandler
{
	bool CanExecute(CommandRequest request);
	Task ExecuteAsync(CommandRequest request);
}
```

The exact type names are open. The important rule is that the context is passed in, not permanently captured by every command instance.

### Command Router

The router resolves a command against the active scope. It is responsible for:

- Finding the active command context
- Applying hotkey precedence
- Ignoring hotkeys that belong to text input or system controls
- Re-checking executability before execution
- Emitting property changes for UI-bound command states
- Preventing duplicate execution where commands are not reentrant

The router is the right place to bridge existing `ICommand` bindings during migration.

### Context Projections

Context projections are read models derived from active UI state. Examples:

- Active `FolderBrowser`
- Active layout capabilities
- Selected items
- Current folder identity
- Sidebar target
- Home widget target
- Window state
- Multitasking state

These projections should be derived from explicit active-scope state. They should not become independent mutable sources of truth.

## Hotkey Precedence

Hotkeys need deterministic precedence. A suggested order:

1. Modal dialog or teaching tip with focus
2. Text input and rename controls
3. Active flyout or context menu
4. Active layout view
5. Active `FolderBrowser`
6. Sidebar or Home widget, when focused
7. Active pane
8. Active tab
9. Window-global commands

This order should be encoded in one router, not scattered across pages and controls.

## Executability

WinUI command binding expects `CanExecute` to be synchronous. Avoid making the command binding wait on asynchronous checks.

Recommended behavior:

- Use synchronous cached state for `CanExecute`.
- Update cached state when the active context changes.
- Perform expensive checks in the background and notify commands when results change.
- Re-check inside `ExecuteAsync` using the freshest context.
- Treat destructive commands as invalid if context changed after the UI was enabled.

This prevents UI hangs and reduces races where a command remains enabled after the selection or folder changes.

## Lifetime

Use one command registry for the app lifetime.

Avoid creating one full command manager per browsing surface. Per-surface command instances multiply event subscriptions by tab and pane count, making leaks and stale state more likely.

If per-surface state is required, keep it in `FolderBrowser` or command context, not inside a separate copy of every command.

## Migration From Current Actions

The current action model can be migrated gradually:

1. Keep generated command codes and command metadata.
2. Add a router-backed `IRichCommand` adapter for existing XAML bindings.
3. Convert a small set of navigation commands to request-based handlers.
4. Replace direct reads from global context services with request context reads.
5. Move action-specific `PropertyChanged` subscriptions into context projection invalidation.
6. Retire direct singleton action instances after command state is router-driven.

## Anti-Patterns

- Do not let command instances own the current tab or pane.
- Do not use command context flags as a substitute for real state.
- Do not allow command handlers to execute based only on stale UI enabled state.
- Do not duplicate all command instances for every pane.
- Do not require layout controls to know about toolbar or command palette behavior.

