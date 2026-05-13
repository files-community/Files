# Packaged build + launch — open issues

Snapshot of unresolved problems found while validating the packaged
(MSIX + SCM) path on `feature/csharp-search-service` as of 2026-05-13.
Pick up here next session.

## What we proved works

1. **MSIX builds end-to-end** via msbuild CLI (see "Reproducing the build" below).
2. **SCM picks up the `desktop6:Service` declaration** at install time:
   `Get-Service FilesSearchService` → `Running`, `LocalSystem`, auto-start.
3. **Named-pipe DACL allows cross-context user → SYSTEM connect** after
   the fix in `src/Files.SearchService/Program.cs:CreatePipeSecurity`:
   added `PipeAccessRights.Synchronize` to the AuthenticatedUsers rule.
   `NamedPipeClientStream` with `PipeOptions.Asynchronous` needs
   Synchronize to wait on the pipe handle for overlapped I/O —
   `ReadWrite` alone throws `UnauthorizedAccessException`.

## Open issues

### Issue 1 — Files.exe exits silently on packaged launch

**Symptom.** Double-click Start menu icon → no window appears. Files.exe
process is created (event log shows AppX container creation + process
add) and torn down in the same second. No crash dump, no event log
error, no first-chance exception logged.

**Diagnostic timeline.**

| Time | State | Finding |
|---|---|---|
| First launch | DeploymentManager auto-init | `COMException 0x80040154 (REGDB_E_CLASSNOTREG)` activating `Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentInitializeOptions`. Stack: `DeploymentManagerCS.AutoInitialize.Access`. |
| Mitigation 1 | Set `<WindowsAppSdkDeploymentManagerInitialize>false</WindowsAppSdkDeploymentManagerInitialize>` in `Files.App.csproj` | Confirmed the generated init code is no longer in `obj/`. The DeploymentManager crash is fixed. |
| Second launch | Different failure mode | Container created+destroyed same second. No managed exception, no WER dump (verified with WER LocalDumps registry set to capture *all* Files.exe crashes — `C:\CrashDumps` stays empty). |
| Mitigation 2 | Added file-based logging to `Program.cs` static constructor | Log file (`%TEMP%\files-startup.log`) **never written** — the process exits before the static constructor runs. |

**Conclusion.** The remaining exit happens *before any managed code in
the Files.App assembly executes* — apphost or .NET runtime
initialization phase. The diagnostic logging in `Program.cs` static
ctor was reverted before commit since it never fired.

**Hypotheses, ranked.**

1. **Apphost can't find a required native dependency.** Possible
   candidates: `Microsoft.WindowsAppRuntime.Bootstrap.dll`,
   `Microsoft.ui.xaml.dll`, or one of the WinAppSDK projection DLLs.
   The framework MSIX dependency *did* install (verified
   `Microsoft.WindowsAppRuntime.1.8 8000.836.2153.0 X64` present), so
   if a DLL is missing it's a load-path issue, not an absence issue.
2. **.NET 10 runtime config mismatch.** `Files.exe` is a
   framework-dependent apphost; if `Files.runtimeconfig.json` points at
   a runtime version not installed in the WindowsApps install
   location, the apphost would error out.
3. **Self-instance redirect on stale kernel state.** `Program.cs:27-46`
   opens a named semaphore `Files-{Env}-Instance` and exits if
   `isNew=false`. A stuck kernel handle from a previously crashed
   Files.exe could make this fire silently. **Ruled out** — the
   diagnostic logging would have caught this, and it didn't fire,
   meaning the exit is upstream of the static ctor.
4. **Single-Project MSIX vs. solution layout disagreement.** The csproj
   has `<EnableMsixTooling>true</EnableMsixTooling>` but build is
   driven from the .csproj directly, not from a separate `.wapproj`.
   Possible config inconsistency between what MSBuild expects for
   resource generation vs. what ends up in the AppxManifest.

**Concrete next steps to try.**

- Enable CLR startup ETW tracing:
  `logman start clrstart -p "Microsoft-Windows-DotNETRuntime" 0x4 0x5
  -ets -o C:\clrstart.etl`, launch Files, stop the trace, open in PerfView.
  This will show whether the runtime even starts.
- Inspect installed package layout for missing DLLs:
  `Get-ChildItem 'C:\Program Files\WindowsApps\FilesDev_*' -Recurse
  -Filter '*.dll'` vs. the build output. Diff for missing entries.
- Run `Files.exe` from inside a packaged-identity wrapper that captures
  stderr. `Invoke-CommandInDesktopPackage` crashed itself on this
  machine (Win10 19045 bug); use `psexec -i <session>` or a manual COM
  activation via `IDesktopAppXActivator` from a small C# host instead.
- Check `Files.runtimeconfig.json` in the installed package against the
  `.NET 10.0.7` runtime declared by WER (`CoreCLR Version: 10.0.726.21808`).

**What we know it's not.**
- Not our search code. Reverting `AppLifecycleHelper.cs` to upstream
  (drop the `Task.Run(SearchServiceManager.EnsureRunning)` line) and
  rebuilding the bundle reproduces the same silent exit. Pre-existing.
- Not DACL/SCM. Those work — see "What we proved" above.
- Not the framework dependency. The MSIX install registers the package
  with the correct `WindowsAppRuntime.1.8` framework, and resolves to
  the installed `8000.836.2153.0` build.

### Issue 2 — First MSBuild pass fails manifest validation

**Symptom.**

```
MakeAppx : error : Manifest validation error: Line 101, Column 56,
Reason: The file name "SearchService\files-search-service.exe"
declared for element ... doesn't exist in the package.
MakeAppx : error : 0x80080204 - The specified package format is not valid
```

**Root cause.** In `src/Files.App/Files.App.csproj` the `<Content
Include="..\Files.SearchService\bin\...">` glob that stages the service
binary into the MSIX has a `Condition="Exists(...)"` predicate. MSBuild
evaluates `Exists()` during the static-evaluation phase, *before any
project is built.* On a clean tree, `Files.SearchService\bin\...`
doesn't exist yet, so the Content items get dropped — but the manifest
still references the path, and MakeAppx fails.

A second `msbuild` invocation right after the failed one succeeds
because the SearchService output now exists on disk from the prior
attempt. **The build is non-deterministic on a clean tree.**

**Fix.** Remove the `Condition="Exists(...)"` — the
`<ProjectReference>` on `Files.SearchService.csproj` (with
`ReferenceOutputAssembly="false"`) is already a build dependency, so
the output is guaranteed to exist by the time Content evaluates *if*
we let MSBuild order things correctly. Alternative: move the staging
into a `<Target BeforeTargets="GenerateAppxManifest">` block that copies
the binaries on demand.

This wasn't fixed in the committed change because it requires testing
on a clean tree and we wanted to bank progress first.

### Issue 3 — v143 platform toolset not installed

**Symptom.**

```
The build tools for Visual Studio 2022 (Platform Toolset = 'v143')
cannot be found. To build using the v143 build tools, please install
Visual Studio 2022 build tools.
```

**Root cause.** The C++ launcher (`src/Files.App.Launcher/Files.App.Launcher.vcxproj`)
declares `<PlatformToolset>v143</PlatformToolset>`. This machine has VS 2026
(toolset v145) installed; v143 isn't present.

**Workarounds.**
1. Install the VS 2022 Build Tools side-by-side with VS 2026.
2. Edit the vcxproj to `v145` locally (matches `project_build_env`
   memory note: "two upstream divergences in `Files.App.Launcher`").
   Don't commit this — it'd break CI which uses v143.

Memory entry already exists at `project_build_env.md` covering the
related stdcpp20 + towupper divergences. Worth extending to mention
the toolset version pin.

## Reproducing the build

```powershell
# 1. Restore.
& "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe" `
    src/Files.App/Files.App.csproj -t:Restore -p:Platform=x64 -p:Configuration=Release

# 2. First build — may fail with the manifest validation error above.
& "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe" `
    src/Files.App/Files.App.csproj -t:Build `
    -p:Platform=x64 -p:Configuration=Release -p:AppxBundlePlatforms=x64 `
    -p:AppxPackageDir="$pwd\artifacts\AppxPackages\" `
    -p:AppxBundle=Always -p:UapAppxPackageBuildMode=SideloadOnly `
    -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=true `
    -p:PackageCertificateKeyFile="src\Files.App\Files.App_TemporaryKey.pfx" `
    -v:minimal

# 3. If step 2 failed with manifest validation, run it again — second pass
#    succeeds because Files.SearchService output now exists on disk.

# 4. Install (admin PowerShell):
Add-AppxPackage -Path "artifacts\AppxPackages\Files.App_4.1.0.0_Test\Files.App_4.1.0.0_x64.msixbundle" `
    -DependencyPath "artifacts\AppxPackages\Files.App_4.1.0.0_Test\Dependencies\x64\Microsoft.WindowsAppRuntime.1.8.msix"
```

## State of the validation

- Search-service infrastructure: **proven** in packaged mode.
- Files.App launch from packaged install: **broken**, pre-existing, root
  cause unknown. This is a ship-blocker for any release that uses the
  packaged path, but does not block sending the PR upstream — the Files
  team's CI builds packaged Files routinely and would not see this
  machine-local failure.

## Useful one-liners for next session

```powershell
# Latest crashes for Files.exe (bypasses WER dedup if LocalDumps is set):
$base = "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\Files.exe"
Get-ItemProperty $base
ls C:\CrashDumps

# Force a fresh launch and watch for activation events:
Start-Process 'shell:appsFolder\FilesDev_j4wp4nz5mtqsg!App'
Start-Sleep 5
Get-WinEvent -LogName 'Microsoft-Windows-AppModel-Runtime/Admin' -MaxEvents 10 |
  Where-Object { $_.TimeCreated -gt (Get-Date).AddMinutes(-1) -and $_.Id -in 211,212,217 }

# Real AUMID (publisher hash varies if cert changes):
Get-StartApps | Where-Object Name -like '*Files - Dev*'
```
