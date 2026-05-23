# Migration Patterns

## Migration Goal

Move native interop away from Vanara and `System.Windows.Forms` when the code path affects app runtime behavior, AOT compatibility, trimming, or WinUI-native integration. Use CsWin32 and explicit COM/PInvoke wrappers so the code stays source-generated and predictable.

## Finding Candidates

Use targeted searches:

```powershell
rg -n "Vanara|System\.Windows\.Forms|Windows\.Forms" src --glob '!**/bin/**' --glob '!**/obj/**' --glob '!**/Strings/**'
rg -n "using Vanara|using Forms = System\.Windows\.Forms|System\.Windows\.Forms\." src --glob '!**/bin/**' --glob '!**/obj/**'
```

Known app areas with historical Vanara/WinForms usage include shell context menus, file operations, network drives, libraries, cloud detector shell items, hotkeys, clipboard/data objects, and layout shell ID lists.

## Replacement Strategy

1. Identify what the current wrapper actually provides: Win32 function, COM interface, Shell helper, clipboard format, file dialog, or convenience object.
2. Search `NativeMethods.txt` for the corresponding API/type.
3. Add missing symbols to `NativeMethods.txt` before writing manual declarations.
4. Replace managed wrapper objects with explicit generated types and helper methods.
5. Encapsulate unsafe code in the narrowest practical helper; keep call sites readable.
6. Remove Vanara/WinForms package references only after all usages in that project are gone.

## Common Patterns

### Vanara PInvoke To CsWin32

- Replace `Vanara.PInvoke.*` static calls with `Windows.Win32.PInvoke`.
- Replace Vanara handle structs with generated CsWin32 handle/value types.
- Replace `Vanara.PInvoke.HRESULT` handling with `Windows.Win32.Foundation.HRESULT`.
- Preserve `SetLastError` semantics by checking generated return values and `WIN32_ERROR` patterns used nearby.

### Vanara Shell Objects To COM

- Prefer generated Shell COM interfaces such as `IShellItem`, `IShellItem2`, `IShellItemArray`, `IContextMenu`, and `IFileOperation`.
- Create Shell items with generated APIs such as `SHCreateItemFromParsingName`, `SHCreateItemFromIDList`, or `SHCreateShellItemArrayFromIDLists`.
- Store returned COM interfaces in `ComPtr<T>` and dispose them deterministically.
- Free PIDLs, strings, and COM-allocated memory with the correct owner (`CoTaskMemFree`, `ComHeapPtr<T>`, or API-specific cleanup).

### WinForms Clipboard And DataObject

- Do not introduce new `System.Windows.Forms.Clipboard`, `DataObject`, or `DataFormats` usage in app runtime code.
- For shell clipboard formats, prefer Win32/OLE clipboard APIs and explicit formats such as `CF_HDROP`, preferred drop effect, and Shell ID list formats.
- Keep format registration and memory ownership explicit. Clipboard data typically requires global memory ownership transfer; verify the API contract before freeing memory.

### WinForms Dialogs Or Window Wrappers

- Avoid WinForms dialog/window adapters in WinUI code paths.
- Use WinUI/Windows App SDK dialogs where possible.
- For native dialogs that require an owner HWND, use existing Win32 helper patterns and generated HWND APIs.

## AOT And Marshaling Checklist

- Avoid APIs that depend on runtime COM marshaling or reflection-discovered interop.
- Avoid new Vanara abstractions on runtime paths; they may hide marshaling, reflection, or unsupported trimming behavior.
- Keep strings and buffers explicit: `PCWSTR`, `PWSTR`, pinned buffers, stackalloc, or fixed blocks as appropriate.
- Keep COM method signatures `PreserveSig`-style and handle `HRESULT` explicitly.
- Confirm who owns every returned pointer or handle.
- Match x86/x64-sensitive APIs carefully. Follow the `SetWindowLongPtr` workaround in `Extras.cs` when architecture-specific entry points are involved.

## Build Verification

Build narrowly first:

```powershell
msbuild -restore src/Files.App.CsWin32/Files.App.CsWin32.csproj -p:Configuration=Debug -p:Platform=x64
msbuild -restore src/Files.App/Files.App.csproj -p:Configuration=Debug -p:Platform=x64
```

If the migrated usage is in another project, build that project first. Do not rely on tests for this repo; current guidance is to make sure relevant builds succeed.
