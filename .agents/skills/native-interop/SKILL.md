---
name: native-interop
description: Native interop guidance for the Files WinUI repo. Use when Codex works on Win32, COM, Shell, clipboard, file operations, hotkeys, NativeMethods.txt, Windows.Win32.PInvoke, AOT compatibility, or migration from Vanara/System.Windows.Forms APIs to CsWin32/source-generated interop.
---

# Files Native Interop

## Core Rule

Prefer the repo's `Files.App.CsWin32` project and source-generated interop over new Vanara, `System.Windows.Forms`, reflection-heavy, or runtime-marshaled native API usage. Keep native interop explicit, AOT-compatible, and close to existing Files patterns.

## Workflow

1. Locate the native API surface with targeted search:
   - `rg -n "Vanara|System\.Windows\.Forms|Windows\.Forms|Windows\.Win32|PInvoke|NativeMethods" src tests --glob '!**/bin/**' --glob '!**/obj/**'`
   - Ignore `.resw` `System.Windows.Forms` resource metadata unless the task is about resource generation.
2. Identify the owning project and whether it already references `src/Files.App.CsWin32/Files.App.CsWin32.csproj`.
3. For new Win32 symbols, add the minimal API/type names to `src/Files.App.CsWin32/NativeMethods.txt`. Do not edit generated files as source.
4. Consume generated APIs through `Windows.Win32`, `Windows.Win32.Foundation`, and domain namespaces such as `Windows.Win32.UI.Shell`.
5. If CsWin32 cannot generate a required shape, add a narrowly scoped manual definition in `Files.App.CsWin32` following `Extras.cs`, `ManualGuid.cs`, and the custom COM interface files.
6. Replace Vanara/WinForms usage with typed CsWin32 handles, COM pointers, `HRESULT`, and explicit memory ownership.
7. Build the affected project first with explicit platform/configuration, then widen only if needed:
   - `msbuild -restore src/Files.App.CsWin32/Files.App.CsWin32.csproj -p:Configuration=Debug -p:Platform=x64`
   - `msbuild -restore src/Files.App/Files.App.csproj -p:Configuration=Debug -p:Platform=x64`

## Reference Selection

- Read `references/cswin32-generation.md` when adding APIs to `NativeMethods.txt`, consuming `PInvoke`, handling generated COM types, or adding manual interop.
- Read `references/migration-patterns.md` when replacing Vanara or `System.Windows.Forms`, reviewing AOT risk, or deciding how to model ownership and marshaling.

## Files-Specific Guardrails

- Keep shared native declarations in `src/Files.App.CsWin32`; do not scatter duplicate P/Invoke declarations through app code.
- Prefer `LibraryImport` for new manual P/Invoke unless an existing workaround requires `DllImport`.
- Preserve `NativeMethods.json` settings unless there is a clear AOT/interoperability reason to change them.
- Use `ComPtr<T>` for COM interface lifetime and `ComHeapPtr<T>` for memory returned from COM task allocation.
- Treat `NativeMethods.txt` as the durable generator input. Read generated `obj`/`Generated` output only for focused debugging.
- Keep changes small: add only the native symbols and helper wrappers needed for the feature or migration.
