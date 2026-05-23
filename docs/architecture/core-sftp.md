---
status: plan
appliesTo: N/A
lastUpdated: 2026-06-06
---

# Core SFTP Provider

## Purpose

The SFTP provider maps an SSH file-transfer session into the core storage
abstraction. It should follow the same provider-neutral contracts as FTP while
accounting for SSH authentication, permissions, symlinks, and Unix-like
metadata.

## Provider identity

`SftpStorageDataProvider.Id` should identify the account and server scope, not
a single item. Item ids should be provider-local remote paths or a stable
server-provided identifier when available.

```text
Provider id: sftp:{host}:{port}:{credentialScope}
Item id: /remote/path/to/item
```

The item id is not a Windows path and must not be interpreted by app code.

## Storables

```csharp
public abstract class SftpStorable : IStorable
{
    public string Id { get; }
    public string Name { get; }
    public StorableCapabilities Capabilities { get; }
    public IStorageDataProvider DataProvider { get; }
}

public sealed class SftpFile : SftpStorable, IFile
{
}

public sealed class SftpFolder : SftpStorable, IFolder
{
}
```

SFTP files normally support `Open`, `CopySource`, `ReadContent`, `Rename`, and
`Delete` when credentials and server permissions allow it. SFTP folders
normally support `EnumerateChildren`, `PasteDestination`, `CreateFile`,
`CreateFolder`, `Rename`, and `Delete`.

## Provider services

Expected services:

- `IStorableResolver`
- `IDisplayNameProvider`
- `IOperationProvider`
- `IClipboardProvider`
- `IPropertyProvider`
- `IIconProvider`

Optional services:

- `IShortcutProvider` when symlink resolution and creation are modeled.
- `IFolderWatcherProvider` when polling or server-side notifications are added.
- `ISearchProvider` when recursive client search is intentionally supported.

SFTP does not have a native Windows Shell context menu, so it should not expose
Shell-backed `IContextMenuProvider` behavior.

## Operations

SFTP operations should be provider-owned and byte-based where possible:

- upload for copy or paste into an SFTP folder;
- download for copy from an SFTP item to a Windows destination;
- remote rename;
- remote delete;
- remote directory creation;
- remote file creation when the server supports upload streams;
- symlink operations when modeled by provider services.

Operations should report `BytesTotal` and `BytesCompleted` when transfer sizes
are known. For directory transfers, report item counts and per-file byte
progress.

## Metadata

SFTP can expose Unix-like metadata such as permissions, owner, group, modified
time, file type, and link target. These values should be projected through
`IPropertyProvider` as provider-local property ids.

Windows ACLs, Shell property stores, and Windows file attributes must not leak
into SFTP contracts.

## Threading and lifetime

The provider owns SSH client lifetime, authentication method selection,
connection reuse, reconnect behavior, host key validation, cancellation, and
timeouts.

Core contracts receive cancellation tokens. UI code receives operation events;
it does not talk to the SSH client directly.

## Limitations

SFTP has no portable recycle bin, Windows Shell context menu, Shell thumbnails,
or Windows file association model. The provider should expose these
capabilities only when they can be implemented predictably.
