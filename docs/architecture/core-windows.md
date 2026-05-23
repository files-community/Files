---
status: plan
appliesTo: N/A
lastUpdated: 2026-06-06
---

# Core Windows Provider

## WindowsStorageDataProvider

Windows provider implementation for Shell namespace items and file-system
items.

```csharp
public sealed class WindowsStorageDataProvider : IStorageDataProvider
{
    public string Id { get; }
    public string Name { get; }

    public bool TryGetService<TService>(out TService service)
        where TService : class;
}
```

For Windows Shell items, identity may be based on a parsing name, persisted
PIDL bytes, file id, known folder id, Shell item id list, or another Shell
identity mechanism. Raw PIDL pointer values must not be exposed by storage
contracts.

## WindowsStorable

Base class for Windows provider-owned storables.

```csharp
public abstract class WindowsStorable : IStorable
{
    protected WindowsStorable(
        WindowsStorageDataProvider dataProvider,
        string id,
        string name,
        StorableCapabilities capabilities)
    {
        DataProvider = dataProvider;
        WindowsDataProvider = dataProvider;
        Id = id;
        Name = name;
        Capabilities = capabilities;
    }

    public string Id { get; }
    public string Name { get; }
    public StorableCapabilities Capabilities { get; }
    public IStorageDataProvider DataProvider { get; }
    public WindowsStorageDataProvider WindowsDataProvider { get; }

    public ValueTask<bool> ExistsAsync(
        CancellationToken cancellationToken = default)
    {
        return WindowsDataProvider.ExistsAsync(
            this,
            cancellationToken);
    }
}
```

## WindowsFile

Windows file-like storable.

```csharp
public sealed class WindowsFile : WindowsStorable, IFile
{
    public WindowsFile(
        WindowsStorageDataProvider dataProvider,
        string id,
        string name,
        StorableCapabilities capabilities)
        : base(dataProvider, id, name, capabilities)
    {
    }
}
```

## WindowsFolder

Windows folder-like storable.

```csharp
public sealed class WindowsFolder : WindowsStorable, IFolder
{
    public WindowsFolder(
        WindowsStorageDataProvider dataProvider,
        string id,
        string name,
        StorableCapabilities capabilities)
        : base(dataProvider, id, name, capabilities)
    {
    }

    public IAsyncEnumerable<IStorable> GetItemsAsync(
        FolderEnumerationOptions options,
        CancellationToken cancellationToken = default)
    {
        return WindowsDataProvider.EnumerateItemsAsync(
            this,
            options,
            cancellationToken);
    }

    public ValueTask<IFolder?> GetParentAsync(
        CancellationToken cancellationToken = default)
    {
        return WindowsDataProvider.GetParentAsync(
            this,
            cancellationToken);
    }
}
```

## Properties

`WindowsPropertyProvider` provides Shell property definitions and values through
`IPropertyProvider`.

```csharp
public sealed class WindowsPropertyProvider : IPropertyProvider
{
    public ValueTask<IReadOnlyList<StoragePropertyDefinition>> GetPropertyDefinitionsAsync(
        IFolder folder,
        CancellationToken cancellationToken = default);

    public ValueTask<IReadOnlyDictionary<string, object?>> GetPropertiesAsync(
        IStorable storable,
        IReadOnlyList<string> propertyIds,
        CancellationToken cancellationToken = default);
}
```

Windows property ids map to Shell property keys. The Windows provider keeps
Shell property stores, COM interfaces, and native memory inside the Windows
implementation.

Details view columns consume `StoragePropertyDefinition` and projected values,
not Shell property store objects.

## Operations

`WindowsOperationProvider` starts Windows Shell operations through
`IOperationProvider`.

```csharp
public sealed class WindowsOperationProvider : IOperationProvider
{
    public ValueTask<IStorageOperation> CopyAsync(
        StorageCopyRequest request,
        CancellationToken cancellationToken = default);

    public ValueTask<IStorageOperation> MoveAsync(
        StorageMoveRequest request,
        CancellationToken cancellationToken = default);

    public ValueTask<IStorageOperation> DeleteAsync(
        StorageDeleteRequest request,
        CancellationToken cancellationToken = default);

    public ValueTask<IStorageOperation> RenameAsync(
        StorageRenameRequest request,
        CancellationToken cancellationToken = default);

    public ValueTask<IStorageOperation> CreateFileAsync(
        StorageCreateFileRequest request,
        CancellationToken cancellationToken = default);

    public ValueTask<IStorageOperation> CreateFolderAsync(
        StorageCreateFolderRequest request,
        CancellationToken cancellationToken = default);
}
```

Windows copy, move, rename, delete, and create-folder use Shell
`IFileOperation` where possible.

Source-generated COM wrappers are preferred. Raw pointers, manual vtables, PIDL
ownership, and Shell COM interfaces do not cross into core storage contracts.

Dialogs, admin retry, history registration, and Status Center registration are
UI policy. Provider code reports operation events and structured results.

Windows Shell progress may report work units instead of byte totals.
`StorageOperationProgress.WorkTotal` and `WorkCompleted` are used for Shell
`IFileOperationProgressSink.UpdateProgress`.
