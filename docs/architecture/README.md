# Architecture

These documents describe the intended target architecture.

They may differ from the current implementation.

Current implementation details belong under `../current`.
Migration plans belong under `../migration`.

## Current Code Maps

- [object-graph.md](object-graph.md): current app/window/tab/pane/view-model
  ownership graph.
- [startup.md](startup.md): current startup sequence.

## Storage Abstraction

- [core-storable.md](core-storable.md): provider-neutral storage contracts, folder browsing, and
  folder views.
- [core-providers.md](core-providers.md): provider identity and optional
  provider services.
- [core-windows.md](core-windows.md): Windows provider architecture.
- [core-ftp.md](core-ftp.md): FTP provider architecture.
- [core-sftp.md](core-sftp.md): SFTP provider architecture.

## UI Architecture

- [ui-folderbrowser.md](ui-folderbrowser.md): `FolderBrowser` as the browsable
  `UserControl` hosted by `PaneHost`, including ownership of current location,
  layout, and navigation history.
- [ui-folderviews.md](ui-folderviews.md): layout-specific folder views,
  capability interfaces, and the flat `ColumnsFolderView` model.
- [ui-command-routing.md](ui-command-routing.md): command metadata, routing,
  hotkey precedence, and execution context.
- [ui-architecture-overview.md](ui-architecture-overview.md): target shell UI architecture
  direction and risk areas.

## Placement

Use `../current` for feature-level implementation notes. Use `../migration` for staged plans that bridge current code to target architecture documents.
