---
status: plan
appliesTo: N/A
lastUpdated: 2026-06-06
---

# Core Storage Abstraction

## Layering

```text
Files.App
  FolderBrowser, FolderView, commands, UI policy
        |
        v
Files.Core.Storage
  IStorable, IFile, IFolder, provider-neutral services, operations
        |
        v
Files.App.Storage
  Windows provider and future provider implementations
        |
        v
Files.App.CsWin32
  Source-generated COM and P/Invoke
```

Core storage contracts use .NET async shapes: `ValueTask`, `Task`,
`IAsyncEnumerable<T>`, and `CancellationToken`.

Core contracts do not expose WinRT storage contracts, raw pointers, PIDLs, COM
interfaces, provider SDK objects, or Shell handles.

`IStorable` remains small. Display names, properties, thumbnails, icons,
previews, clipboard behavior, folder watchers, shortcuts, file associations,
context menus, search, and operations are provider services.

## IStorable

Represents an item that can appear in a folder view.

Examples include files, folders, drives, Shell namespace items, search result
items, Recycle Bin items, SFTP items, ZIP entries, cloud placeholders, and MTP
objects.

```csharp
public interface IStorable
{
    // Provider-local durable item id; opaque to app code and not a file-system path.
    string Id { get; }

    // Provider-local item name.
    string Name { get; }

    // Provider-reported command availability hints.
    StorableCapabilities Capabilities { get; }

    // Data provider that owns this item.
    IStorageDataProvider DataProvider { get; }

    // Checks whether the item still exists in its provider.
    ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default);
}
```

The durable identity of an item is the pair of `IStorageDataProvider.Id` and
`IStorable.Id`.

## IFile

Represents an item that is file-like.

```csharp
public interface IFile : IStorable
{
}
```

`IFile` does not guarantee readable or writable content. Stream access belongs
to provider services.

## IFolder

Represents an item that is folder-like.

```csharp
public interface IFolder : IStorable
{
    // Enumerates lightweight child items.
    IAsyncEnumerable<IStorable> GetItemsAsync(
        FolderEnumerationOptions options,
        CancellationToken cancellationToken = default);

    // Gets the parent folder, or null when no parent can be represented.
    ValueTask<IFolder?> GetParentAsync(
        CancellationToken cancellationToken = default);
}
```

Folder enumeration does not load thumbnails, full property bags, Shell context
menus, large metadata payloads, or provider-specific command sets.

`IFolder` does not guarantee writability, hierarchy, or a file-system path.
Create, paste, delete, and rename behavior belongs to provider services.

## StorableCapabilities

```csharp
[Flags]
public enum StorableCapabilities
{
    None = 0,
    Open = 1 << 0,
    Preview = 1 << 1,
    CopySource = 1 << 2,
    CutSource = 1 << 3,
    PasteDestination = 1 << 4,
    Rename = 1 << 5,
    Delete = 1 << 6,
    CreateFile = 1 << 7,
    CreateFolder = 1 << 8,
    EnumerateChildren = 1 << 9,
    ReadContent = 1 << 10,
    WriteContent = 1 << 11,
    ShowProperties = 1 << 12,
    HasThumbnail = 1 << 13,
    HasProperties = 1 << 14,
    WatchChildren = 1 << 15
}
```

These flags are UI-level hints. Actual operation execution still validates the
operation through provider services.

## FolderEnumerationOptions

```csharp
public sealed class FolderEnumerationOptions
{
    // Include hidden items.
    public bool IncludeHidden { get; init; }

    // Include system items.
    public bool IncludeSystem { get; init; }

    // Enumerate descendants instead of only direct children; defaults to false.
    public bool Recursive { get; init; }

    // Provider-specific property names to prefetch when cheap; providers may ignore this.
    public IReadOnlyList<string> PropertyPrefetchHints { get; init; } = [];
}
```

## Properties

`IPropertyProvider` provides metadata property definitions and values.

```csharp
public interface IPropertyProvider
{
    ValueTask<IReadOnlyList<StoragePropertyDefinition>> GetPropertyDefinitionsAsync(
        IFolder folder,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyDictionary<string, object?>> GetPropertiesAsync(
        IStorable storable,
        IReadOnlyList<string> propertyIds,
        CancellationToken cancellationToken = default);
}
```

```csharp
public sealed record StoragePropertyDefinition(
    string Id,
    string Name,
    Type ValueType,
    bool IsVisibleByDefault);
```

## Operations

Operations are first-class objects because Files needs progress UI,
cancellation, taskbar integration, operation history, undo, and structured
errors.

```csharp
public interface IOperationProvider
{
    ValueTask<IStorageOperation> CopyAsync(
        StorageCopyRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<IStorageOperation> MoveAsync(
        StorageMoveRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<IStorageOperation> DeleteAsync(
        StorageDeleteRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<IStorageOperation> RenameAsync(
        StorageRenameRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<IStorageOperation> CreateFileAsync(
        StorageCreateFileRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<IStorageOperation> CreateFolderAsync(
        StorageCreateFolderRequest request,
        CancellationToken cancellationToken = default);
}
```

```csharp
public interface IStorageOperation
{
    StorageOperationId Id { get; }
    StorageOperationKind Kind { get; }
    StorageOperationState State { get; }
    StorageOperationProgress Progress { get; }
    Task<StorageOperationResult> Completion { get; }

    IAsyncEnumerable<StorageOperationEvent> WatchAsync(
        CancellationToken cancellationToken = default);

    ValueTask RequestCancelAsync(CancellationToken cancellationToken = default);
}
```

```csharp
public readonly record struct StorageOperationProgress(
    long? BytesTotal,
    long? BytesCompleted,
    int? ItemsTotal,
    int? ItemsCompleted,
    int? WorkTotal,
    int? WorkCompleted,
    double? Percent,
    string? CurrentItemName,
    StorageOperationPhase Phase,
    bool IsIndeterminate);

public sealed record StorageOperationResult(
    StorageOperationState State,
    IReadOnlyList<StorageOperationItemResult> Items,
    IReadOnlyList<StorageOperationError> Errors,
    bool AnyOperationsAborted);
```

The progress model supports both byte-based providers and Shell work-unit
providers. Windows Shell `IFileOperationProgressSink.UpdateProgress` reports
estimated work units, not bytes.

## Clipboard

`IClipboardProvider` provides provider-aware clipboard data and paste behavior.

```csharp
public interface IClipboardProvider
{
    ValueTask<StorageClipboardData?> GetDataAsync(
        CancellationToken cancellationToken = default);

    ValueTask SetDataAsync(
        ClipboardSetRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<IStorageOperation> PasteAsync(
        ClipboardPasteRequest request,
        CancellationToken cancellationToken = default);
}
```

Clipboard copy, cut, and paste requests are command-layer intent. The provider
service translates the request into provider-native clipboard formats or storage
operations.

The low-level provider service does not show conflict UI, admin retry UI, or
Status Center UI directly.

## Thumbnails, icons, and previews

```csharp
public interface IThumbnailProvider
{
    ValueTask<StorageThumbnail?> GetThumbnailAsync(
        IStorable storable,
        ThumbnailOptions options,
        CancellationToken cancellationToken = default);
}

public interface IIconProvider
{
    ValueTask<StorageIcon?> GetIconAsync(
        IStorable storable,
        IconOptions options,
        CancellationToken cancellationToken = default);
}

public interface IPreviewProvider
{
    ValueTask<StoragePreview?> GetPreviewAsync(
        IStorable storable,
        PreviewOptions options,
        CancellationToken cancellationToken = default);
}
```

Icons are the lightweight fallback path. Thumbnails may be slower and should be
loaded after enumeration. Preview data is separate from thumbnails.

## Folder watchers

```csharp
public interface IFolderWatcherProvider
{
    ValueTask<IFolderWatcher?> TryCreateWatcherAsync(
        IFolder folder,
        FolderWatcherOptions options,
        CancellationToken cancellationToken = default);
}

public interface IFolderWatcher : IAsyncDisposable
{
    IFolder Folder { get; }

    IAsyncEnumerable<FolderWatcherEvent> WatchAsync(
        CancellationToken cancellationToken = default);
}

public abstract record FolderWatcherEvent(string? ItemId);
```

`FolderBrowser` owns watcher subscriptions and translates provider changes into
refresh or incremental collection updates. `FolderView` does not subscribe
directly to storage watchers.

## FolderBrowser

`FolderBrowser` is the storage-aware coordinator for a browsing surface.

A split pane owns multiple `FolderBrowser` instances. Columns mode is one
`FolderBrowser` hosting multiple sibling `FolderView` instances.

```csharp
public interface IFolderBrowser
{
    StorageId CurrentFolderId { get; }
    IFolder? CurrentFolder { get; }
    FolderBrowserState State { get; }
    IReadOnlyList<IFolderView> Views { get; }
    IFolderView? ActiveView { get; }

    ValueTask NavigateAsync(
        StorageId folderId,
        FolderNavigationOptions options,
        CancellationToken cancellationToken = default);

    ValueTask RefreshAsync(
        FolderRefreshOptions options,
        CancellationToken cancellationToken = default);

    ValueTask<IStorageOperation> ExecuteAsync(
        StorageCommandRequest request,
        CancellationToken cancellationToken = default);
}
```

Every navigation and refresh has an internal generation id. Late enumeration,
thumbnail, metadata, or watcher results from an older generation are ignored.

## Folder views

`FolderView` is a XAML control that renders a folder projection and reports user
intent. It does not call storage providers, Shell APIs, Win32 APIs, or operation
providers directly.

Details, grid, list, tiles, gallery, and columns are folder view types.

```csharp
public sealed record FolderItemModel(
    StorageId Id,
    string Name,
    string DisplayName,
    FolderItemTraits Traits,
    StorageItemVisualState VisualState);
```

The model keeps provider identity. It does not replace identity with a display
path.

```csharp
public interface IFolderView
{
    IReadOnlyList<FolderItemModel> Items { get; }
    IReadOnlyList<StorageId> SelectedItemIds { get; }

    event EventHandler<FolderViewItemInvokedEventArgs> ItemInvoked;
    event EventHandler<FolderViewSelectionChangedEventArgs> SelectionChanged;
    event EventHandler<FolderViewCommandRequestedEventArgs> CommandRequested;

    ValueTask SetItemsAsync(
        IReadOnlyList<FolderItemModel> items,
        CancellationToken cancellationToken = default);

    ValueTask ScrollToAsync(
        StorageId itemId,
        CancellationToken cancellationToken = default);

    ValueTask FocusSelectedItemsAsync(
        CancellationToken cancellationToken = default);

    ValueTask StartRenameAsync(
        StorageId itemId,
        CancellationToken cancellationToken = default);
}
```

Details view owns table layout mechanics such as selection visuals, column
widths, sorting gestures, grouping presentation, focus, scrolling, and inline
rename state.

Grid view owns item sizing, selection visuals, focus, scrolling, grouping
presentation, and inline rename state.

View-specific data providers adapt core provider services into UI-ready data:

- Details data comes from `IPropertyProvider`, `IDisplayNameProvider`,
  `IIconProvider`, and folder browser projection state.
- Grid visual data comes from `IThumbnailProvider`, `IIconProvider`,
  `IDisplayNameProvider`, and folder browser projection state.

## Migration boundary

Current WinRT-shaped storage types are migration sources, not target contracts.

`HomeStorable` is not part of the target model. Home is app composition over
providers, pinned locations, recent items, and commands.

Legacy storage implementations should be deleted once callers move to the new
contracts.
