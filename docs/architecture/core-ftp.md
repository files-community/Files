---
status: plan
appliesTo: N/A
lastUpdated: 2026-06-06
---

# Core FTP Provider

## Purpose

The FTP provider maps an FTP server connection into the core storage
abstraction. It should expose remote directories as `IFolder`, remote files as
`IFile`, and FTP commands through provider services.

## Provider identity

`FtpStorageDataProvider.Id` should identify the account and server scope, not a
single item. Item ids should be provider-local remote paths or another stable
remote identifier when the server exposes one.

```text
Provider id: ftp:{host}:{port}:{credentialScope}
Item id: /remote/path/to/item
```

The item id is not a Windows path and must not be interpreted by app code.

## Storables

```csharp
public abstract class FtpStorable : IStorable
{
    public string Id { get; }
    public string Name { get; }
    public StorableCapabilities Capabilities { get; }
    public IStorageDataProvider DataProvider { get; }
}

public sealed class FtpFile : FtpStorable, IFile
{
}

public sealed class FtpFolder : FtpStorable, IFolder
{
}
```

FTP files normally support `Open`, `CopySource`, `ReadContent`, `Rename`, and
`Delete` when credentials and server permissions allow it. FTP folders normally
support `EnumerateChildren`, `PasteDestination`, `CreateFile`, `CreateFolder`,
`Rename`, and `Delete`.

## Provider services

Expected services:

- `IStorableResolver`
- `IDisplayNameProvider`
- `IOperationProvider`
- `IClipboardProvider`
- `IPropertyProvider`
- `IIconProvider`

Optional services:

- `IFolderWatcherProvider` when polling or server-side notifications are added.
- `ISearchProvider` when server-side search or recursive client search is
  intentionally supported.

FTP does not have a native Windows Shell context menu, so it should not expose
Shell-backed `IContextMenuProvider` behavior.

## Operations

FTP operations should be provider-owned and byte-based where possible:

- upload for copy or paste into an FTP folder;
- download for copy from an FTP item to a Windows destination;
- remote rename;
- remote delete;
- remote directory creation;
- remote file creation when the server supports upload streams.

Operations should report `BytesTotal` and `BytesCompleted` when transfer sizes
are known. For directory transfers, report item counts and per-file byte
progress.

## Threading and lifetime

The provider owns FTP client lifetime, connection reuse, reconnect behavior,
credential prompts, cancellation, and timeouts.

Core contracts receive cancellation tokens. UI code receives operation events;
it does not talk to the FTP client directly.

## Limitations

FTP has no portable file ids, file change notifications, recycle bin, rich
property store, or native preview system. The provider should expose these
capabilities only when they can be implemented predictably.
