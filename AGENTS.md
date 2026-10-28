# Files Development Guidelines

This project is a C#/.NET WinUI 3 desktop app; an alternative to File Explorer.

- Protect context usage. Any command with unknown or potentially large output must be capped. Prefer targeted commands such as `rg`, `Get-Content -TotalCount`, `Select-Object -First`, or focused `git diff -- <path>`; for example, `COMMAND 2>&1 | Select-Object -First 200`. If a line cap is still too noisy, narrow the query instead of dumping full output.
- Always follow `.editorconfig`
- Keep changed text files in CRLF line endings
- Keep comments concise and useful. Do not add comments that restate obvious code.
- Never read entire generated files in `bin` or `obj` unless the generated source is directly needed.
- Prefer targeted search over full file reads.
- Touch only what you must. Clean up only files you created or changed for the task.
- Treat file operations, shell integration, drag/drop, preview handlers, archive actions, settings persistence, and localization as high-risk areas.
- For Win32, COM, Shell, clipboard, hotkey, and file operation interop, prefer `src/Files.Storage`, `NativeMethods.txt`, and existing wrappers/helpers.
- Avoid ad hoc P/Invoke declarations when CsWin32 or existing interop code can cover the API.
- Do not edit generated CsWin32 output directly. Update source declarations, wrappers, or generator inputs instead.
- For UI work, use existing XAML resources, controls, converters, commands, and localization patterns. Avoid one-off styles or hard-coded user-visible strings.
- Start by identifying the smallest relevant project, feature area, and files for the task.
- Read nearby code before adding new abstractions. Prefer existing WinUI, MVVM, service, command, and storage patterns.
- Keep implementation scoped to the requested behavior. Avoid opportunistic refactors, formatting churn, dependency updates, and generated file edits.
- Treat tool output as evidence. When behavior changes, run the focused build that can prove it and report anything left unverified.

## Code Review Rules

### Native interop

- Reject new uses of `DllImport`. Prefer existing interop wrappers or CsWin32, and use `LibraryImport` only when CsWin32 cannot generate or safely express the required API.
- Reject new uses of `ComImport`. Prefer CsWin32-generated COM interfaces or `GeneratedComInterface`, and use a focused AOT-safe vtable wrapper only when source-generated COM cannot represent the interface.
- Add app runtime CsWin32 APIs to `src/Files.Storage/NativeMethods.txt` or the owning app project's `NativeMethods.txt`, and keep test-only APIs in the relevant test project's `NativeMethods.txt`; do not edit generated CsWin32 output.
- When introducing a required native module, add the matching `DirectPInvoke` entry to `src/Files.App/Files.App.csproj`. Do not direct-bind optional modules or entry points that are not guaranteed to exist on every supported Windows version.

### Reflection and Native AOT

- Flag new runtime reflection used for member discovery, invocation, dynamic activation, or runtime code generation unless trimming and AOT analysis can statically prove it safe. Simple type inspection such as `obj.GetType().Name` is out of scope.
- Prefer source generation, static dispatch, or explicitly registered types. When reflection is necessary, use `DynamicallyAccessedMembers`, `DynamicDependency`, or another narrowly justified preservation mechanism as required and keep trimming and AOT diagnostics clean.
- `RequiresDynamicCode` and `RequiresUnreferencedCode` declare incompatibility; they do not make reflection AOT-safe and are not acceptable exemptions.
- Keep suppressions of trimming or AOT diagnostics local, document why the reflected members remain available, and verify the relevant AOT build.

### WinRT server and client boundaries

Before changing a server feature, its WinRT contract, or client code that calls it, read [WinRT server and client interoperability](#winrt-server-and-client-interoperability). Its rules cover async completion, WinUI reentrancy, COM ownership, Native AOT exposure, `IIterable<T>`/`IIterator<T>` proxy registration, and shared server state. Apply them to refactors as well as new features.

## Codebase Structure

```text
/src
├── Files.App                    Main WinUI app
├── Files.App.Controls           Shared app controls
├── Files.Storage                Storage, implementations, and shared app Win32 interop
├── Files.App.BackgroundTasks    Background task project
├── Files.App.Server             App service/server project
├── Files.Core.SourceGenerator   Roslyn source generators and analyzers
└── Files.Shared                 Shared attributes, extensions, and common code
```

```text
/tests
├── Files.App.UITests
├── Files.App.UnitTests
└── Files.InteractionTests
```

## Build

Prefer explicit platform/configuration builds.
Unless the task is specifically about resolving or inspecting warnings, add `-v:quiet -clp:ErrorsOnly` to `msbuild` commands so the log proves success or shows only actionable errors.

```powershell
msbuild -restore Files.slnx -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
```

If `msbuild` isn't available in the current shell, run it from Visual Studio Developer PowerShell. Match `-arch`, `-host_arch`, and `-p:Platform` to the platform you're verifying; use `x64` for x64 work and `arm64` for ARM64 work.

```powershell
pwsh.exe -NoProfile -Command "& {
  Import-Module 'C:\Program Files\Microsoft Visual Studio\18\Professional\Common7\Tools\Microsoft.VisualStudio.DevShell.dll'
  Enter-VsDevShell 1ba2cc4e -SkipAutomaticLocation -DevCmdArguments '-arch=x64 -host_arch=x64'
  msbuild -restore src/Files.App/Files.App.csproj -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
}"
```

For focused C# work, build the affected project first.
Do not run build commands in parallel.

```powershell
msbuild -restore src/Files.Shared/Files.Shared.csproj -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
msbuild -restore src/Files.Core.SourceGenerator/Files.Core.SourceGenerator.csproj -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
msbuild -restore src/Files.App/Files.App.csproj -p:Configuration=Debug -p:Platform=x64 -v:quiet -clp:ErrorsOnly
```

## Test

We currently don't have a suitable set of tests for AI agents. Just make sure that the builds succeed.

## Commit & Push

When asked to commit, run these commands beforehand:

```powershell
git status --short
git diff --check
```

Do not revert unrelated user changes. Stage only files that belong to the requested change.

Use concise commit messages that describe the behavior change, for example:

```text
Add source-generated settings storage
```

## Open a PR

When asked to open a PR, use a short PR title that names the behavior, not the implementation mechanics only, and prepend the PR type:

- "Fix": use this prefix when the linked issue is a bug
- "Feature": use this prefix when the linked issue is a feature request
- "Code Quality": anything else

The repository maintainers draft release notes based on these PR types: only fixes and feature requests are listed.

Good examples:

```text
Fix: Fixed an issue where thumbnails wouldn't refresh when a file was updated
Feature: Add support for previewing AVI files in the Preview Pane
Code Quality: Add source-generated settings serialization
```

For the PR body, follow `./.github/PULL_REQUEST_TEMPLATE.md`.

## WinRT server and client interoperability

Apply this section when changing a server feature, its WinRT API, native marshalling, or the client code
that consumes it. These rules record invariants behind failures encountered during the server migration;
preserve the reasons for these patterns when refactoring.

### Start with the existing boundary

| Concern | Source to inspect |
| --- | --- |
| Feature entry points and per-client lifetime | [Services](src/Files.App.Server/Services/), [AppService](src/Files.App.Server/AppService.cs), [ServerHost](src/Files.App.Server/ServerHost.cs) |
| Async completion across processes | [ServerAsync](src/Files.App.Server/ServerAsync.cs) |
| Concrete managed types exposed through WinRT | [WinRTExposedExternalTypes](src/Files.App.Server/WinRTExposedExternalTypes.cs) |
| Runtime activation and deployment | [Program](src/Files.App.Server/Program.cs), [Helpers](src/Files.App.Server/Helpers.cs), [package manifest](src/Files.App/Package.appxmanifest) |
| Deferred UI entry points | [DispatcherQueueExtensions](src/Files.App/Extensions/DispatcherQueueExtensions.cs), [BaseLayoutPage](src/Files.App/Views/Layouts/BaseLayoutPage.cs), [InfoPaneViewModel](src/Files.App/ViewModels/UserControls/InfoPaneViewModel.cs) |
| Cache, startup, notifications and persistence | [CachedTask](src/Files.App.Server/Caching/CachedTask.cs), [StartupServices](src/Files.App.Server/Initialization/StartupServices.cs), [ServerEvents](src/Files.App.Server/ServerEvents.cs), [SettingsStore](src/Files.App.Server/Settings/SettingsStore.cs) |
| Native calls and apartment ownership | [NativeMethods.txt](src/Files.Storage/NativeMethods.txt), [NativeMethods.json](src/Files.Storage/NativeMethods.json), [STATask](src/Files.Storage/Windows/Managers/STATask.cs), [ThreadWithMessageQueue](src/Files.Storage/Windows/Shell/ThreadWithMessageQueue.cs) |

Use the feature service that owns an operation. Keep client registration/lifetime in `AppService` and shared
configuration in the server. Do not recreate per-instance configuration or connection-selection helpers.
Prefer typed WinRT contracts, enums, structs, buffers and collections. JSON is appropriate for persisted
settings or import/export; do not add serialization merely to avoid implementing the WinRT boundary correctly.

### ServerAsync protects completion delivery, not UI reentrancy

- Return server-owned async operations through `ServerAsync.Run`, including operations exposed by session
  objects. Do not replace these wrappers with direct `AsyncInfo.Run`, `AsAsyncOperation`, or `AsAsyncAction`
  returns without preserving their completion-delivery behavior.
- A client may exit while work is pending. Calling its completion delegate can then throw an RPC disconnect
  exception on the server's completion thread. The wrappers catch the specific HRESULTs in `IsDisconnected`
  only while delivering that callback. Do not broaden this to all exceptions or apply it to the operation body.
- Preserve `Id`, `Status`, `ErrorCode`, `GetResults`, `Cancel` and `Close` forwarding. Work failures must still
  reach the caller through the operation. The completion delegate receives the returned wrapper (`owner`),
  not the underlying task adapter. Keep the wrapper and delegate alive through delivery.
- Preserve each operation's completion contract; do not replace awaited work with fire-and-forget as part
  of a move to the server.
- `ServerAsync.Run` does not guarantee that the provider starts later, runs on a worker, or uses an STA.
  It does not make the client-side COM call nonblocking or safe inside a WinUI render callback.
- Keep cancellation scoped to the request. A caller's `WaitAsync(token)` can cancel its wait for a shared
  cached load; do not let one client cancel work needed by all other clients.

Existing async shape (substitute the actual catalog and DTO):

```csharp
public static IAsyncOperation<IReadOnlyList<MyInfo>> GetItemsAsync()
    => ServerAsync.Run(token => MyCatalog.Items.GetAsync().WaitAsync(token));
```

### WinUI callers must leave protected callbacks before calling COM

A method returning `IAsyncOperation<T>` still has a synchronous COM/RPC entry call. Activation, interface
queries, property reads, and that entry call can pump messages before the operation is returned. Starting
one from `ContainerContentChanging`, layout, binding, or related realization callbacks can re-enter WinUI's
protected render loop and terminate the process. An `await` at the call site does not prevent this.

- Preserve the deferred entry boundaries in `EnqueueItemLoad`, `EnqueueDeferredAsync`, and
  `BasePropertiesPage.Properties_Loaded`. Queue the start of the operation, not just its final UI assignment.
- Do not replace unconditional enqueueing with `HasThreadAccess` followed by direct execution. Do not use
  `EnqueueOrInvokeAsync` as a substitute where deferral is required: it can execute without leaving the caller.
- Do not invoke inline when a deferred enqueue fails. The current preview helper discards work when the
  dispatcher is unavailable; changing that behavior must preserve the no-inline guarantee.
- Check that the view/item/request is still current inside the queued callback. Preserve container identity,
  request-version, cancellation and window-close checks so a recycled row cannot receive another item's data.
- Queue priority affects responsiveness, not apartment safety. Changing Low to Normal may be a valid measured
  adjustment; restoring a synchronous start is not an equivalent optimization.
- Avoid mass `Task.Run` additions. They change apartment/thread behavior and do not address the protected
  callback boundary. Keep UI work on its dispatcher and Shell work on the appropriate existing STA helper.
- Do not synchronously wait for server RPC in UI or native Shell callbacks. Preserve deliberate local paths
  such as tag bookkeeping in file-operation callbacks when waiting on another apartment could deadlock.
- Deferring a load must not serialize unrelated work. Folder lists and Details metadata should not wait for
  icons or recursive size calculations. Observe independent tasks and finalize loading-state flags in
  `finally`; logging an exception does not clear a loading indicator.

### COM ownership and apartments

- The server initializes WinRT as MTA; its dispatcher and STA workers have separate purposes. A Shell object
  created on an STA does not become free-threaded because a WinRT service returns its data. Keep native
  operations and releases on the owning apartment, or use supported agile references/marshalling.
- Do not switch STA/MTA/ASTA initialization or add apartment attributes to hide reentrancy failures. ASTA is
  not interchangeable with `[STAThread]`, and changing the apartment model needs its own supported design.
- Prefer CsWin32/source-generated COM and existing wrappers. A raw `nint`, `ThisPtr`, or vtable address does
  not keep its managed owner alive. Retain the `IObjectReference`, COM wrapper or `SafeHandle` through the last
  native use, including exceptional paths. Preserve generated lifetime guards such as `GC.KeepAlive`.
- A borrowed pointer is not an owned COM reference. If native code retains it beyond a call, obtain and later
  release the required reference. Balance ownership exactly once. `GC.KeepAlive` neither adds a COM reference
  nor prevents another thread from disposing an owner; use the appropriate ownership/handle mechanism.
- Do not pass raw pointers between apartments or across `await` without a valid lifetime and marshalling
  strategy. A `fixed` block pins managed memory; it does not establish COM ownership or agility.
- Keep native cleanup on its owning worker. Do not add another finalizer to a session when the native menu
  object already owns finalization; the menu/session pair previously needed a single cleanup owner.
- Use generated bindings for probes as well as production code. Do not call guessed vtables or proxy/stub
  factories with fabricated interface layouts to investigate an ABI failure.

### Exposure, activation and transport registration are different requirements

#### Managed exposure and Native AOT

- Follow existing `public sealed partial` WinRT service/DTO declarations. For managed classes implementing
  WinRT or WinRT-mapped interfaces, make the implementation and all containing types `partial` when required
  by CsWinRT. This includes private nested `IDisposable` implementations (`IClosable`), as demonstrated by
  `PinnedFoldersManager.SyncSuspensionScope`. Do not silence `CsWinRT1028` instead of fixing the declarations.
- Root the concrete closed types that cross the ABI in `WinRTExposedExternalTypes.cs`. An interface return
  type alone is insufficient; so is making an open generic definition partial. A new result type can require
  both a collection registration and a `ServerAsyncOperation<...>` registration.
- Preserve needed `GeneratedWinRTExposedType` and `DynamicWindowsRuntimeCast` annotations on client code.
  Resolve AOT diagnostics using statically known types and source generation, not runtime reflection fallbacks.
- For cached DTO lists, follow the existing `ReadOnlyCollection<T>` pattern (`ToList().AsReadOnly()`) and
  publish a stable backing collection. Do not mutate a published collection during client enumeration.
- Do not replace a DTO collection with `T[]` just because both implement `IReadOnlyList<T>`. Returning a
  managed array as a WinRT collection previously caused CCW/runtime-class-name failures. A direct ABI array
  return is a different contract; supported primitive arrays and value-struct array APIs are not prohibited.

For a new DTO list, the matching closed registrations typically include:

```csharp
[assembly: GeneratedWinRTExposedExternalType(typeof(ReadOnlyCollection<MyInfo>))]
[assembly: GeneratedWinRTExposedExternalType(typeof(ServerAsyncOperation<IReadOnlyList<MyInfo>>))]
```

Use the actual type and existing imports; these example types are placeholders.

#### Activation and metadata identity

- For a new activatable service, keep `Program.GetActivatableTypes()`, the generated activation factory,
  and the manifest's out-of-process `ActivatableClass` entries consistent. `Helpers.GetActivationFactory`
  delegates to the generated `Module`; do not introduce reflection-based factory discovery. Returned DTOs
  do not need activation entries solely because they are public.
- Package the matching server executable and `Files.App.Server.winmd`. Verify the project references and
  package payload when changing service APIs; a successful source build alone does not prove correct deployment.

#### `IIterable<T>` and `IIterator<T>` proxy registration

**For each new custom DTO collection crossing the process boundary, register both `IIterable<T>` and
`IIterator<T>` in [Files.App/Package.appxmanifest](src/Files.App/Package.appxmanifest).** Register the closed
interfaces for the actual DTO type, not only the open generic definitions or the collection's return interface.
This is separate from the managed exposure attributes and the service's activation entry.

- A C# `IReadOnlyList<T>` projects to `IVectorView<T>`. Enumeration also queries `IIterable<T>` and uses
  `IIterator<T>`. Successful `Count`/indexer access does not prove that enumeration works across processes.
- Add the two `<Interface>` entries to the existing `windows.activatableClass.proxyStub` extension, under
  the metadata marshaler's `ProxyStub` with class ID `00000355-0000-0000-C000-000000000046` and
  `<Path>Files.App.Server.winmd</Path>`. Do not add another copy of the whole extension for every DTO.

Existing `LibraryInfo` registrations illustrate the complete manifest shape:

```xml
<Extension Category="windows.activatableClass.proxyStub">
    <ProxyStub ClassId="00000355-0000-0000-C000-000000000046">
        <Path>Files.App.Server.winmd</Path>
        <Interface
            Name="Windows.Foundation.Collections.IIterable`1&lt;Files.App.Server.LibraryInfo&gt;"
            InterfaceId="1e6b2a4a-8e22-5e39-96d8-532fe4a50b60" />
        <Interface
            Name="Windows.Foundation.Collections.IIterator`1&lt;Files.App.Server.LibraryInfo&gt;"
            InterfaceId="7cd1cab2-0275-5a0d-aa21-8c9da450165b" />
    </ProxyStub>
</Extension>
```

These names and IIDs belong to `LibraryInfo`. **Do not reuse its IIDs for a new DTO.** Use the canonical WinRT
type name (including the backtick and generic arity), escape angle brackets in XML, and derive each closed
interface's IID from that DTO's actual WinRT identity. CLR names such as `IEnumerable<T>` are not manifest names.

- Derive interface IIDs from the actual generated WinRT metadata/type signatures using CsWinRT/SDK tooling.
  Do not guess GUIDs, copy them to unrelated types, or derive them from a display name alone. Namespace and
  default-interface identity matter; `typeof(...).GUID` on a CLR generic interface is not a substitute for a
  WinRT parameterized IID. Moving a source file without changing its type identity is different from renaming it.
- Keep the matching `ReadOnlyCollection<T>` and `ServerAsyncOperation<IReadOnlyList<T>>` managed exposure
  registrations described above. Managed CCW exposure does not replace proxy registration, and proxy
  registration does not root a missing managed CCW. The object's runtime class name and interfaces must also match.
- Account for interfaces reached indirectly through enumeration, `QueryInterface`, and callbacks, not just
  the public method signature. Apply the same rule to other custom closed generic interfaces; map iteration,
  for example, introduces `IKeyValuePair<K, V>` elements and their iterable/iterator instantiations.
- Do not register every system instantiation blindly. Windows already supplies some proxies, including the
  `EventHandler<string>` IID checked during this migration. Investigate the exact failing IID and supported
  Windows versions before concluding that a missing app-manifest entry is the cause.
- If `IReadOnlyListImpl<T>.Make_IEnumerableObjRef` or CCW creation fails, fix the exposure/identity/registration
  layer. Do not hide it with Count/indexer copying, JSON transport, a fabricated runtime class name, or blanket
  exception swallowing. Normal `foreach`/LINQ enumeration must remain supported across the boundary.

### Preserve the data contract, including null and empty values

| Boundary | Rule |
| --- | --- |
| WinRT `string` / HSTRING | Null HSTRING represents the empty string. A nullable C# annotation does not preserve a null-string sentinel. |
| Direct WinRT ABI arrays (`T[]`) | Do not assume that all null arrays become empty. In C#/WinRT 2.3.1, the runtime marshallers encode null as `(length: 0, data: 0)` and decode a zero pointer as null. A non-null zero-length buffer decodes as an empty array. |
| Collection/runtime-class interfaces | A nullable interface reference is a different ABI shape from an array's length/buffer pair. Do not infer its null behavior from array handling. |
| Unknown timestamps | Use a supported nullable value, such as `DateTimeOffset?`, rather than sending `DateTime.MinValue`. Time-zone conversion at the WinRT boundary can underflow a sentinel. Convert to a UI sentinel only after receipt if necessary. |

These are marshaller-level semantics, not a guarantee about every cross-process projection. Where the
null/empty distinction is meaningful, represent presence explicitly instead of depending on buffer allocation.

- Normalize an empty WinRT string back to null only when the underlying API's contract means "omitted".
  `ContextMenuSession.InvokeItemAsync` does this for the Shell working directory; passing an empty string
  instead broke folder context-menu commands. Skip overlay lookup for a missing/empty path as appropriate.
- Do not apply this normalization to all strings: an intentionally empty setting is not a missing setting.
  Likewise, define whether null/empty input arrays mean no items, clear, or invalid input before normalizing.
- Use WinRT-compatible payload types. Do not pass arbitrary CLR objects, `JsonElement`, or unregistered
  collections as `object`. Settings use `SettingsValue`/`SettingsValueCodec` for this reason.
- Preserve enums, item identity, ordering, failure fallbacks and incremental events when moving data into a
  catalog. A snapshot transport is not a reason to reset a client's entire observable collection on each event.
- Do not fetch expensive Shell metadata merely because a DTO has a field for it. Populate fields when a
  consumer needs them, reuse metadata already read, and cache/invalidate at the server boundary.

### Shared state, notifications and client lifetime

- Server caches/configuration are shared by clients in that server's activation context. Elevated and
  non-elevated servers can be separate processes accessing the same persisted settings; singleton activation
  is not proof that only one process can write a file. Keep tray/window ownership within the correct context.
- Preserve `CachedTask` invalidation, refresh-after-in-flight-invalidation, and retry behavior for faulted
  loads. Do not cache a failed operation forever or let a caller's cancellation poison the shared cache.
- Keep shared initialization in the server's startup/catalog infrastructure. Do not make the first window
  or a feature's UI wait for unrelated catalog warmups; load required data asynchronously and update the view.
- Keep notification batching bounded. Repeated events must not keep restarting the flush deadline forever.
  The timer's scheduled state is separate from the pending set because paused Quick Access events may remain
  pending after a flush. Never invoke client callbacks while holding server cache/registration locks.
- Preserve per-client/nested Quick Access batches and release them on client exit. Batch notifications, not
  the reads needed by the reorder operation itself. Keep the client's synchronization/reorder behavior.
- Update authoritative settings in memory promptly; coalesce persistence outside synchronous/UI setters.
  Preserve the file lease, atomic write, merge with other writers, and pending-write version rules. A `.lock`
  file's existence is not ownership: the open exclusive handle holds the lock.
- Flush required persistence before reporting import success or starting package installation. Store/package
  updates can terminate running server processes; background writes alone are not a durability guarantee.
- Server notifications must refresh client caches and raise the appropriate UI changes. Transient settings
  pages must unsubscribe when unloaded/disposed and refresh when activated again, without retaining old views.
- Retain process-exit client tracking even when the server stays alive for caching. It controls session cleanup,
  pending batches and Quit. Determine session-restoration eligibility atomically at client registration; a new
  window must not restore another running instance's tabs.
- Honor `ServerHost`'s keep-running, Quit and update policy. Do not unconditionally exit on the last client
  closing or tie shared-cache lifetime to disposal of an individual client.
- "Keep Files running in the background" refers to the server. Main-window teardown must close owned auxiliary
  windows or retain their client session until they close. Do not revive semaphore-based cached UI instances.
- When combining server features, compose `StartupServices`, `CatalogWatchers`, service APIs and client
  registration/cleanup once. Preserve every initialization, invalidation, flush and disposal hook; do not
  duplicate whole shared implementations or drop another feature's hooks during conflict resolution.

### Reference semantics

- [HSTRING and null/empty strings](https://learn.microsoft.com/en-us/windows/win32/winrt/hstring)
- [C#/WinRT 2.3.1 array marshallers](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/src/WinRT.Runtime/Marshalers.cs)
- [C#/WinRT 2.3.1 ABI call generation](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/src/cswinrt/code_writers.h)
- [C#/WinRT Native AOT and trimming guidance](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/docs/aot-trimming.md)

Recheck version-dependent details when upgrading C#/WinRT; do not turn an observation about one projection
or transport into a universal WinRT rule.
