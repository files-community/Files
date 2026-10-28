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

Before changing a server feature, its WinRT contract, or client code that calls it, read [WinRT server and client interoperability](#winrt-server-and-client-interoperability). Its rules cover async completion, WinUI reentrancy, COM ownership, Native AOT exposure, `IIterable<T>`/`IIterator<T>` proxy registration, and ABI value semantics. Apply them to refactors as well as new features.

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

Apply this section to out-of-process WinRT contracts, their server implementations, and their client callers.

### Start with the existing boundary

| Concern | Source to inspect |
| --- | --- |
| Feature entry points and per-client lifetime | [Services](src/Files.App.Server/Services/), [AppService](src/Files.App.Server/AppService.cs), [ServerHost](src/Files.App.Server/ServerHost.cs) |
| Async completion across processes | [ServerAsync](src/Files.App.Server/ServerAsync.cs) |
| Concrete managed types exposed through WinRT | [WinRTExposedExternalTypes](src/Files.App.Server/WinRTExposedExternalTypes.cs) |
| Runtime activation and deployment | [Program](src/Files.App.Server/Program.cs), [Helpers](src/Files.App.Server/Helpers.cs), [package manifest](src/Files.App/Package.appxmanifest) |
| Deferred UI entry points | [DispatcherQueueExtensions](src/Files.App/Extensions/DispatcherQueueExtensions.cs), [BaseLayoutPage](src/Files.App/Views/Layouts/BaseLayoutPage.cs), [InfoPaneViewModel](src/Files.App/ViewModels/UserControls/InfoPaneViewModel.cs) |
| Native calls and apartment ownership | [NativeMethods.txt](src/Files.Storage/NativeMethods.txt), [NativeMethods.json](src/Files.Storage/NativeMethods.json), [STATask](src/Files.Storage/Windows/Managers/STATask.cs), [ThreadWithMessageQueue](src/Files.Storage/Windows/Shell/ThreadWithMessageQueue.cs) |

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
- Keep cancellation scoped to the request; one client's cancellation must not cancel unrelated server-owned work.

Example async shape (substitute the actual implementation and DTO):

```csharp
public static IAsyncOperation<IReadOnlyList<MyInfo>> GetItemsAsync()
    => ServerAsync.Run(token => LoadItemsAsync(token));
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
- Do not invoke inline when a deferred enqueue fails; preserve the no-inline guarantee.
- Avoid mass `Task.Run` additions. They change apartment/thread behavior and do not address the protected
  callback boundary. Keep UI work on its dispatcher and Shell work on the appropriate existing STA helper.
- Avoid synchronous waits for RPC in UI or native callbacks, and never invoke client callbacks while holding
  server locks. Reentrant calls can otherwise deadlock the two sides.

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
- Account for abrupt client disconnection. Server-side resource and subscription lifetimes cannot depend
  solely on the client executing a graceful `Dispose` path.
- Use generated bindings rather than guessed vtables or fabricated interface layouts.

### Exposure, activation and transport registration are different requirements

#### Managed exposure and Native AOT

- Follow existing `public sealed partial` WinRT service/DTO declarations. For managed classes implementing
  WinRT or WinRT-mapped interfaces, make the implementation and all containing types `partial` when required
  by CsWinRT. This includes private nested `IDisposable` implementations (`IClosable`). Fix declarations
  rather than suppressing `CsWinRT1028`.
- Root the concrete closed types that cross the ABI in `WinRTExposedExternalTypes.cs`. An interface return
  type alone is insufficient; so is making an open generic definition partial. A new result type can require
  both a collection registration and a `ServerAsyncOperation<...>` registration.
- Preserve needed `GeneratedWinRTExposedType` and `DynamicWindowsRuntimeCast` annotations on client code.
  Resolve AOT diagnostics using statically known types and source generation, not runtime reflection fallbacks.
- Follow the existing `ReadOnlyCollection<T>` representation for DTO collection results.
- Do not replace a DTO collection with `T[]` just because both implement `IReadOnlyList<T>`. Returning a
  managed array as a WinRT collection can change CCW exposure and runtime-class identity. A direct ABI array
  return is a different contract; supported primitive arrays and value-struct arrays are not prohibited.

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
- Respect the activation/security context. Singleton activation does not imply one server instance shared
  across all elevation or security contexts.

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
- Windows supplies proxies for some system instantiations. Account for the exact IID and supported Windows
  versions instead of adding app registrations blindly for every system type.
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

- Normalize an empty WinRT string to null only when the receiving native API requires null for an omitted
  value. Define the meaning of null and empty inputs in the contract before applying normalization.
- Use WinRT-compatible payload types. Do not pass arbitrary CLR objects, `JsonElement`, or unregistered
  collections as `object`.

### Reference semantics

- [HSTRING and null/empty strings](https://learn.microsoft.com/en-us/windows/win32/winrt/hstring)
- [C#/WinRT 2.3.1 array marshallers](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/src/WinRT.Runtime/Marshalers.cs)
- [C#/WinRT 2.3.1 ABI call generation](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/src/cswinrt/code_writers.h)
- [C#/WinRT Native AOT and trimming guidance](https://github.com/microsoft/CsWinRT/blob/2.3.1.260716.1/docs/aot-trimming.md)

Recheck version-dependent details when upgrading C#/WinRT; do not turn an observation about one projection
or transport into a universal WinRT rule.
