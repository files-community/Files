# Current Implementation

These docs explain the code that exists today. Use them as maps when reading or
changing the current implementation, especially when a feature spans actions,
view models, services, native APIs, and external libraries.

## Docs

- [overview.md](overview.md): major subsystems, startup, and high-level object
  ownership.
- [navigation.md](navigation.md): address bar, sidebar, back/forward stacks,
  path history, and location changes.
- [storage.md](storage.md): current storage item wrappers and UI item models.
- [operations.md](operations.md): copy, move, delete, rename, recycle bin, and
  undo/redo execution paths.
- [commands.md](commands.md): action/command hierarchy, context menus,
  toolbar commands, and keyboard shortcuts.
- [tabs-and-panes.md](tabs-and-panes.md): tabs, panes, shell pages, and
  navigation state.
- [selection.md](selection.md): selected items, multi-selection, and selection
  restoration.
- [search.md](search.md): local search, indexed search, tag search, and
  filtering.
- [thumbnails.md](thumbnails.md): thumbnail loading, icon fallback, caching,
  and delayed retries.
- [properties.md](properties.md): property retrieval, details columns, metadata,
  and properties windows.
- [clipboard.md](clipboard.md): copy, cut, paste, and clipboard formats.
- [drag-drop.md](drag-drop.md): drag sources, drop targets, operation selection,
  and shell data objects.
- [watchers.md](watchers.md): folder update notifications and refresh behavior.
- [shell-integration.md](shell-integration.md): known folders, shell items, file
  operations, context menus, thumbnails, and properties.

## Feature Doc Shape

Current implementation docs in this folder use a feature-oriented shape:

- Purpose
- Entry points
- Code map
- Win32, COM, WinRT, or library dependencies
- Threading and lifetime rules
- Error handling, progress, and logging behavior
- Known limitations
