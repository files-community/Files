# Repository Instructions

This repository contains the Files Windows desktop app, a WinUI-based file manager for Windows. The codebase includes the main app, reusable controls, storage layers, Win32/CsWin32 interop, packaging support, background/server components, and UI/interaction tests.

## Codebase Overview

```text
/src
├── Files.App                  // Main WinUI desktop app: startup, DI, views, view models, actions, services, dialogs, styles, assets, strings, and app helpers.
├── Files.App.CsWin32          // CsWin32 source-generated Win32 interop. Add APIs to NativeMethods.txt here.
├── Files.App.Controls         // Reusable WinUI controls shared by the app.
├── Files.App.Storage          // App-facing storage abstractions and storage implementation pieces.
├── Files.App.BackgroundTasks  // Background task project.
├── Files.App.Server           // App service/server behavior.
├── Files.App.Launcher         // Launch-related entry points.
├── Files.App.OpenDialog       // File open dialog-specific app project/folder.
├── Files.App.SaveDialog       // File save dialog-specific app project/folder.
├── Files.App (Package)        // Packaging-related app project assets.
├── Files.Core.Storage         // Lower-level storage primitives that should not depend on the main WinUI app.
├── Files.Core.SourceGenerator // Roslyn source generators used by the solution.
└── Files.Shared               // Shared models, helpers, and code used by multiple projects.
```

```text
/tests
├── Files.App.UITests      // UI test assets and views.
├── Files.InteractionTests // Interaction tests used by CI automation.
└── Files.App.UnitTests    // Placeholder/stale in this checkout; verify project files before assuming unit tests exist here.
```

## When Dealing With Interop Code

When the user asks to convert marshaled interop code into unmarshaled interop, or asks to remove trim-unsafe manual P/Invoke definitions, see [docs/interop-unmarshaled-conversion.md](docs/interop-unmarshaled-conversion.md).

Prefer adding APIs and related generated types to `src/Files.App.CsWin32/NativeMethods.txt`, then update the callees to use CsWin32-generated `Windows.Win32.PInvoke` APIs directly. Do not leave manual `DllImport` definitions in place or replace them with local `LibraryImport` declarations when CsWin32 can generate the API.

## When Building the App

Use `.github/workflows/ci.yml` as the source of truth for building.
For normal local verification, build with MSBuild restore and explicit configuration/platform; packaging is not required.

```powershell
msbuild -restore src\Files.App\Files.App.csproj /p:Configuration=Debug /p:Platform=x64
```

## When Packaging the App

Use `.github/workflows/ci.yml` as the source of truth for packaging. Adjust `Configuration`, `Platform`, and `AppxBundlePlatforms` as needed.
