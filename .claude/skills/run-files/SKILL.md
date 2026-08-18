---
name: run-files
description: Build, deploy, launch, screenshot and click the Files WinUI 3 desktop app on Windows. Use when asked to run Files, start the app, deploy it, take a screenshot of it, drive its UI, or verify a change in the real running app rather than only building it.
---

# Run the Files app

Files is a **packaged (MSIX) WinUI 3 desktop app** targeting `net10.0-windows10.0.26100.0`.
It cannot be run by launching `Files.exe` — it needs package identity, the Windows App SDK
runtime, and a repaired loose layout. Everything is wrapped in one driver:

```
.claude/skills/run-files/driver.ps1
```

All paths below are relative to the repo root. All commands were run on Windows 11 with
PowerShell 7 (`pwsh`). The driver prints `KEY=VALUE` result lines so you can assert on
output instead of reading prose.

## Prerequisites

Check first — it names exactly what is missing:

```bash
pwsh -NoProfile -File .claude/skills/run-files/driver.ps1 prereqs
```

Expected when healthy:

```
MSVC=14.51.36231
VS=C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools
PREREQS=OK
```

If it reports `MISSING=...`, install the named item. These are the ones that actually bite:

| Missing | Fix |
|---|---|
| `dotnet-sdk-10` | `winget install --id Microsoft.DotNet.SDK.10 --architecture x64 --scope machine` — `global.json` pins `10.0.102`; .NET 8 cannot build this repo. |
| `vs2026-buildtools` | `winget install --id Microsoft.VisualStudio.BuildTools --override "--quiet --wait --norestart --add Microsoft.VisualStudio.Workload.VCTools --add Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools --add Microsoft.VisualStudio.Component.Windows11SDK.26100 --includeRecommended"` |
| `atl` | See **ATL** below — `--includeRecommended` does **not** include it. |
| `developer-mode` | Enable Developer Mode in Windows Settings (required to register an unsigned loose package). |

**VS 2022 (17.x) cannot build this repo at all** — two independent blockers: .NET SDK 10
requires MSBuild 18, and the `.vcxproj` files pin `PlatformToolset = v145` (MSVC 14.5x),
which ships only with VS 2026. `prereqs` deliberately looks for `[18.0,19.0)` and ignores
any 17.x install still on the machine.

### ATL

Four C++ projects `#include <atlbase.h>`. ATL is not part of `--includeRecommended`, and
the VS installer **must be elevated** — with `--quiet` and no elevation it exits `5007`
having done nothing. `--wait` is a bootstrapper flag and is rejected by `vs_installer.exe`
(exit `87`). This is the invocation that worked, run from an elevated PowerShell:

```bash
& 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vs_installer.exe' modify --installPath 'C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools' --add Microsoft.VisualStudio.Component.VC.ATL --add Microsoft.VisualStudio.Component.VC.ATLMFC --quiet --norestart
```

It returns immediately; poll until `VC\Tools\MSVC\<ver>\atlmfc\include\atlbase.h` exists.

## Build

```bash
pwsh -NoProfile -File .claude/skills/run-files/driver.ps1 build
```

```
RESTORE_EXIT=0
BUILD_EXIT=0
```

This runs two passes, both required:

1. `-t:restore -p:RestorePackagesConfig=true` — `Files.App.Launcher.vcxproj` still uses
   legacy `packages.config` (CppWinRT + WIL), which a plain `-restore` silently skips.
2. `-restore Files.slnx -p:Configuration=Debug -p:Platform=x64`

**Always build `Files.slnx`, never `Files.App.csproj` alone.** The C++ projects and
`Files.App.Server` are solution-level `BuildDependency` entries, not `ProjectReference`s —
a project-scoped build *removes* `Files.App.Server.exe`/`.dll` from the output folder and
produces an app that will not start.

The build is `-m:1` on purpose; parallel builds race on the shared native output directory.

## Run — agent path

```bash
pwsh -NoProfile -File .claude/skills/run-files/driver.ps1 deploy
pwsh -NoProfile -File .claude/skills/run-files/driver.ps1 launch
pwsh -NoProfile -File .claude/skills/run-files/driver.ps1 shot
```

Or the whole chain — `prereqs → build → deploy → launch → shot`:

```bash
pwsh -NoProfile -File .claude/skills/run-files/driver.ps1 all
```

`deploy` output on a healthy run:

```
LAYOUT=D:\Github\Files\src\Files.App\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64
WINAPPSDK_NEED=2.4.0.0 HAVE=2.4.0.0
LOGO_ICO=True
SERVER_EXE=True
REGISTER=OK
AUMID=FilesDev_ykqwq8d6ps0ag!App
```

`launch` waits for the window title to become `<Page> - Files` (twice in a row); the splash
title is exactly `Files`. Debug builds are not ReadyToRun, so the very first launch after a
build can take ~20-40s; warm launches settle in under 10s.

```
LAUNCH=OK title='Downloads - Files' after=6s
```

The page name varies — see the session-restore gotcha below.

### Driving the UI

```bash
# screenshot the window (PNG lands in .claude/skills/run-files/_out/)
pwsh -NoProfile -File .claude/skills/run-files/driver.ps1 shot

# click at window-relative coordinates, then auto-screenshot
pwsh -NoProfile -File .claude/skills/run-files/driver.ps1 click 177 334

# app's own structured log / process state
pwsh -NoProfile -File .claude/skills/run-files/driver.ps1 log 40
pwsh -NoProfile -File .claude/skills/run-files/driver.ps1 status
pwsh -NoProfile -File .claude/skills/run-files/driver.ps1 stop
```

A successful click — note the title changing, which is your assertion:

```
FOREGROUND=True
CLICK=(177,334) screen=(717,574) title='Downloads - Files'
SHOT=D:\Github\Files\.claude\skills\run-files\_out\click.png
WINDOW=2880x1541 title='Downloads - Files'
```

Screenshots use `PrintWindow` with `PW_RENDERFULLCONTENT` (flag `2`). This matters twice:
plain `CopyFromScreen` captures whatever is on top (you get the wrong window), and WinUI 3
windows are DWM-composited so flag `0` returns blank.

**Reading coordinates off the PNG:** the capture is the window rect at *real pixel size* —
here `2880x1541`. If your image viewer scales it down (e.g. shows it at 2000px wide),
multiply your measured coordinates by the scale factor before passing them to `click`.
Getting this wrong is silent: the click lands somewhere harmless and the title never changes.

The driver re-reads the window rect immediately before clicking, because restoring/focusing
the window moves it — in the sample above the same window-relative `(177,334)` mapped to a
different screen point than on the previous run.

Verify the click worked by reading the `title=` in the output — it tracks the current page
(`Settings - Files` → `Downloads - Files`). **Look at the PNG.** A dark frame showing only
`Files Dev` is the splash, i.e. a failed start, not a loaded app.

## Run — human path

Double-click the app from the Start menu after `deploy` (it registers as **Files Dev**).
There is no `dotnet run` / `msbuild -t:Deploy` path — see Gotchas.

## Test

Per `CLAUDE.md`, this repo has no test suite suitable for agents. A clean
`BUILD_EXIT=0` is the bar. `tests/` builds as part of the solution.

## Gotchas

- **`msbuild -t:Deploy` does not exist here.** Not on the solution (every non-packaging
  project errors `MSB4057`) and not on `Files.App.csproj` either — the MSIX packaging
  targets that define it are not imported. Deployment is `Add-AppxPackage -Register` against
  the build output, which is what `driver.ps1 deploy` does.

- **`Assets\AppTiles\**` never reaches `bin\`.** The csproj marks only `Assets\Resources\**`
  and `Assets\FilesOpenDialog\*` as `CopyToOutputDirectory`; `AppTiles` is MSIX *package*
  content. Registering the raw `bin` folder therefore yields an app that hangs on the splash
  forever: `new SystemTrayIcon()` (`App.xaml.cs:131`) constructs `System.Drawing.Icon` from
  `Assets\AppTiles\Dev\Logo.ico`, throws `DirectoryNotFoundException`, and that runs *before*
  the `MainPage` navigation on line 137. `deploy` copies the folder in as a repair step.

- **Startup exceptions are invisible.** The navigation call is fire-and-forget
  (`_ = MainWindow.Instance.InitializeApplicationAsync(...)`), so a startup failure produces
  no log line, no crash, no Event Log entry — just a frozen splash with
  `Responding=True` and near-zero CPU. `debug.log` stops after `App launched.`
  To get the real exception, dump the heap:

  ```bash
  dotnet tool install -g dotnet-dump
  ~/.dotnet/tools/dotnet-dump.exe collect -p <pid> -o files.dmp --type Full
  ~/.dotnet/tools/dotnet-dump.exe analyze files.dmp -c "dumpheap -type Exception -stat" -c "exit"
  ~/.dotnet/tools/dotnet-dump.exe analyze files.dmp -c "printexception -lines <addr>" -c "exit"
  ```

  The dump is ~650 MB; delete it afterwards.

- **The app's identity is `FilesDev`, and the assembly is `Files`.** `<AssemblyName>Files</AssemblyName>`
  means the output is `Files.exe`/`Files.dll` — there is no `Files.App.exe`. Looking for the
  latter will convince you a successful build produced nothing.

- **The WinAppSDK runtime is version-gated.** `AppxManifest.xml` requires
  `Microsoft.WindowsAppRuntime.2` at a `MinVersion` matching the `Microsoft.WindowsAppSDK`
  package version. Having 1.6/1.7 and an older 2.x installed is not enough. `deploy` reads
  the required version out of the manifest and installs the matching MSIX from the
  already-restored NuGet package (`~/.nuget/packages/microsoft.windowsappsdk.runtime/<ver>/tools/MSIX/win10-x64`)
  — no download needed.

- **Synthetic clicks silently hit the wrong app.** `SetForegroundWindow` returns `False` from
  a background process — Windows' foreground lock — so the Files window never activates and
  `mouse_event` clicks land on whatever *is* foreground. Nothing errors; the title just never
  changes. `driver.ps1` works around it by attaching to the foreground thread's input queue
  (`AttachThreadInput`) before calling `SetForegroundWindow`, and prints `FOREGROUND=True`.
  If it prints `CLICK=ABORT`, it refused to click rather than click blindly into another app.

- **The app restores its previous session.** Startup setting *"Continue where you left off"*
  is the default, so `launch` may land on whatever page was last open (e.g. `Settings - Files`)
  rather than `Home - Files`. Don't assert on a specific starting page.

- **`Enter-VsDevShell` prints a harmless `'vswhere.exe' is not recognized` line.** Ignore it;
  the shell still initialises. `driver.ps1` avoids the dev shell entirely and calls
  `MSBuild.exe` by absolute path.

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `error MSB4236: The SDK 'Microsoft.NET.Sdk' could not be found` | Building with VS 2022's MSBuild 17.x. Use VS 2026 (`driver.ps1` picks it automatically). |
| `error MSB8020: build tools for v145 cannot be found` | MSVC 14.5x missing — install the VCTools workload from VS 2026. |
| `fatal error C1083: Cannot open include file: 'atlbase.h'` | ATL not installed. See **ATL** above. |
| `The missing file is ..\..\packages\Microsoft.Windows.ImplementationLibrary...targets` | `packages.config` not restored — `driver.ps1 build` does this pass first. |
| `LAUNCH=STUCK title='Files'` | Splash hang. Check `LOGO_ICO=` and `SERVER_EXE=` from `deploy`; re-run `build` (whole solution) then `deploy`. |
| `REGISTER=FAIL ... 0x80073CF3` (dependency) | WinAppSDK runtime older than the manifest's `MinVersion`. Re-run `deploy`. |
| `RUNTIME_MSIX_MISSING=...` | NuGet restore hasn't run yet — run `driver.ps1 build` first. |
| App window shows the *previous* app's content in a screenshot | You used `CopyFromScreen`. Use `driver.ps1 shot` (PrintWindow). |
| `click` runs, `title=` never changes | Coordinates were read off a scaled view of the PNG — multiply by the scale factor. The PNG is the window's real pixel size (see `WINDOW=`). |
| `CLICK=ABORT window would not take focus` | Another app is holding the foreground lock. Close/minimise it, or re-run; `shot` still works regardless of focus. |
