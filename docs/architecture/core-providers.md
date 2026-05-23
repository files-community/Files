---
status: plan
appliesTo: N/A
lastUpdated: 2026-06-06
---

# Core Storage Providers

## IStorageDataProvider

`IStorageDataProvider` owns provider identity and exposes optional
provider-specific services.

```csharp
public interface IStorageDataProvider
{
    // Unique provider id.
    string Id { get; }

    // Provider name.
    string Name { get; }

    // Gets a provider-specific service.
    bool TryGetService<TService>(out TService service)
        where TService : class;
}
```

Provider-specific behavior is exposed through
`IStorageDataProvider.TryGetService<TService>()`.

## Provider services

Initial provider services:

```csharp
IStorableResolver
IDisplayNameProvider
IPropertyProvider
IThumbnailProvider
IIconProvider
IPreviewProvider
IOperationProvider
IClipboardProvider
IContextMenuProvider
IFileAssociationProvider
ISearchProvider
IShortcutProvider
IFolderWatcherProvider
```

Provider services are optional. A provider can support a narrow feature set by
exposing only the services it can implement correctly.

## IStorableResolver

Resolves provider-local ids into storables.

```csharp
public interface IStorableResolver
{
    ValueTask<IStorable> ResolveAsync(
        string id,
        CancellationToken cancellationToken = default);
}
```

## IDisplayNameProvider

Provides UI-facing names for storables.

```csharp
public interface IDisplayNameProvider
{
    ValueTask<string> GetDisplayNameAsync(
        IStorable storable,
        CancellationToken cancellationToken = default);
}
```

## IContextMenuProvider

Provides provider-native context menu commands.

```csharp
public interface IContextMenuProvider
{
    ValueTask<IReadOnlyList<StorageCommand>> GetCommandsAsync(
        ContextMenuRequest request,
        CancellationToken cancellationToken = default);

    ValueTask InvokeAsync(
        StorageCommand command,
        ContextMenuInvokeOptions options,
        CancellationToken cancellationToken = default);
}
```

## IFileAssociationProvider

Provides file association and open-with behavior.

```csharp
public interface IFileAssociationProvider
{
    ValueTask<FileAssociation?> GetDefaultAssociationAsync(
        IFile file,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<FileAssociationVerb>> GetVerbsAsync(
        IFile file,
        CancellationToken cancellationToken = default);

    ValueTask InvokeAsync(
        IFile file,
        FileAssociationVerb verb,
        CancellationToken cancellationToken = default);
}
```

## ISearchProvider

Provides provider-native search.

```csharp
public interface ISearchProvider
{
    IAsyncEnumerable<IStorable> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);
}
```

## IShortcutProvider

Provides shortcut and link behavior.

```csharp
public interface IShortcutProvider
{
    ValueTask<ShortcutTarget?> ResolveAsync(
        IStorable shortcut,
        CancellationToken cancellationToken = default);

    ValueTask<IStorable> CreateAsync(
        ShortcutCreateRequest request,
        CancellationToken cancellationToken = default);

    ValueTask UpdateAsync(
        IStorable shortcut,
        ShortcutUpdateRequest request,
        CancellationToken cancellationToken = default);
}
```

## Future provider services

Future provider services can be added after the current contracts settle.

Examples:

- sharing provider;
- permissions provider;
- compression provider;
- cloud sync state provider.

Future providers can include SFTP, ZIP, cloud storage, MTP, and app-owned
virtual collections.
