<#
.SYNOPSIS
    Build / deploy / launch / drive the Files WinUI 3 app on Windows.

.DESCRIPTION
    Agent-facing harness. Every subcommand prints a machine-greppable
    KEY=VALUE result line so callers can assert without parsing prose.

    Usage:  pwsh -NoProfile -File .claude\skills\run-files\driver.ps1 <command> [args]

    Commands:
      prereqs        Verify toolchain; prints MISSING=... if anything is absent
      build          Full solution build (x64 Debug). NEVER build the app project alone.
      deploy         Repair the loose layout + register the MSIX package
      launch         Start the app, wait until it is past the splash screen
      status         Process / window / package state
      log [n]        Tail the app's own debug.log
      shot [path]    Screenshot the app window (PrintWindow; works when occluded)
      click <x> <y>  Click at window-relative coordinates, then screenshot
      stop           Kill the app
      all            prereqs -> build -> deploy -> launch -> shot

    Screenshots default to .claude\skills\run-files\_out\.
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)][string]$Command = 'help',
    [Parameter(Position = 1)][string]$Arg1,
    [Parameter(Position = 2)][string]$Arg2,
    [string]$Configuration = 'Debug',
    [string]$Platform = 'x64'
)

$ErrorActionPreference = 'Continue'
Set-StrictMode -Off

$RepoRoot  = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$OutDir    = Join-Path $PSScriptRoot '_out'
$PkgName   = 'FilesDev'          # Identity Name for Debug builds (Preview/Release differ)
$AppId     = 'App'
$LogPath   = "$env:LOCALAPPDATA\Packages\FilesDev_ykqwq8d6ps0ag\LocalState\debug.log"

if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }

# ---------------------------------------------------------------- helpers

function Get-VsPath {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) { return $null }
    # -version 18 matters: VS 2022 (17.x) cannot build this repo at all.
    & $vswhere -products '*' -version '[18.0,19.0)' -property installationPath -latest 2>$null | Select-Object -First 1
}

function Get-LayoutDir {
    # The registered layout is the plain build output folder, not a packaging dir.
    $base = Join-Path $RepoRoot "src\Files.App\bin\$Platform\$Configuration"
    if (-not (Test-Path $base)) { return $null }
    $m = Get-ChildItem $base -Recurse -Filter 'AppxManifest.xml' -File -EA SilentlyContinue |
         Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($m) { return $m.Directory.FullName }
    return $null
}

# Sets $script:LastExit. MSBuild's own output is echoed, but kept out of the
# return value so the KEY=VALUE result lines stay clean.
function Invoke-MSBuild {
    param([string[]]$MsBuildArgs)
    $script:LastExit = 1
    $vs = Get-VsPath
    if (-not $vs) { Write-Output 'MISSING=VS2026BuildTools'; return }
    $msbuild = Join-Path $vs 'MSBuild\Current\Bin\amd64\MSBuild.exe'
    if (-not (Test-Path $msbuild)) { Write-Output 'MISSING=MSBuild18'; return }
    Push-Location $RepoRoot
    try {
        # -m:1 on purpose: parallel builds race on the shared native output dir.
        $out = & $msbuild @MsBuildArgs -m:1 2>&1
        $script:LastExit = $LASTEXITCODE
        $out | ForEach-Object { Write-Output $_ }
    } finally { Pop-Location }
}

function Get-AppWindow {
    Get-Process -Name 'Files' -EA SilentlyContinue |
        Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
}

Add-Type -AssemblyName System.Drawing -EA SilentlyContinue
if (-not ('FilesWin' -as [type])) {
    Add-Type @'
using System; using System.Runtime.InteropServices;
public class FilesWin {
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RC r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
  [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool attach);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  public struct RC { public int L, T, Rt, B; }

  // A background process cannot call SetForegroundWindow directly -- Windows'
  // foreground lock makes it return false and do nothing. Attaching our input
  // queue to the current foreground thread lifts that restriction.
  public static bool ForceForeground(IntPtr h) {
    IntPtr fg = GetForegroundWindow();
    if (fg == h) return true;
    uint us = GetCurrentThreadId();
    uint them = GetWindowThreadProcessId(fg, IntPtr.Zero);
    bool attached = (us != them) && AttachThreadInput(us, them, true);
    ShowWindow(h, 9);
    BringWindowToTop(h);
    bool ok = SetForegroundWindow(h);
    if (attached) AttachThreadInput(us, them, false);
    return ok || GetForegroundWindow() == h;
  }
}
'@
}

# MUST run before any window/cursor call. Without it this process is DPI-virtualised:
# PrintWindow yields a physical-pixel bitmap while SetCursorPos is interpreted in
# scaled coordinates, so clicks land in the wrong place on a HiDPI display.
# -4 = DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
if (-not [FilesWin]::SetProcessDpiAwarenessContext([IntPtr](-4))) {
    [FilesWin]::SetProcessDPIAware() | Out-Null
}

function Save-WindowShot {
    param([string]$Path)
    $p = Get-AppWindow
    if (-not $p) { Write-Output 'SHOT=NOWINDOW'; return }
    $h = $p.MainWindowHandle
    $r = New-Object FilesWin+RC
    [FilesWin]::GetWindowRect($h, [ref]$r) | Out-Null
    $w = $r.Rt - $r.L; $ht = $r.B - $r.T
    if ($w -le 0 -or $ht -le 0) { Write-Output 'SHOT=BADRECT'; return }
    $bmp = New-Object System.Drawing.Bitmap($w, $ht)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    # flag 2 = PW_RENDERFULLCONTENT: required for WinUI 3 / DWM-composited windows,
    # and captures correctly even when another window is on top.
    [FilesWin]::PrintWindow($h, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    Write-Output "SHOT=$Path"
    Write-Output "WINDOW=${w}x${ht} title='$($p.MainWindowTitle)'"
}

# ---------------------------------------------------------------- commands

function Cmd-Prereqs {
    $missing = @()

    $sdks = (& dotnet --list-sdks 2>$null) -join "`n"
    if ($sdks -notmatch '(?m)^10\.') { $missing += 'dotnet-sdk-10' }

    $vs = Get-VsPath
    if (-not $vs) { $missing += 'vs2026-buildtools' }
    else {
        $msvc = Get-ChildItem (Join-Path $vs 'VC\Tools\MSVC') -Directory -EA SilentlyContinue |
                Sort-Object Name -Descending | Select-Object -First 1
        if (-not $msvc) { $missing += 'msvc-v145' }
        else {
            Write-Output "MSVC=$($msvc.Name)"
            # PlatformToolset v145 in the .vcxproj files maps to MSVC 14.5x.
            if (-not (Test-Path (Join-Path $msvc.FullName 'atlmfc\include\atlbase.h'))) {
                $missing += 'atl'    # NOT included by --includeRecommended
            }
        }
        Write-Output "VS=$vs"
    }

    $devmode = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' `
                -Name AllowDevelopmentWithoutDevLicense -EA SilentlyContinue).AllowDevelopmentWithoutDevLicense
    if ($devmode -ne 1) { $missing += 'developer-mode' }

    if ($missing.Count) { Write-Output "MISSING=$($missing -join ',')" }
    else { Write-Output 'PREREQS=OK' }
    $script:Ok = ($missing.Count -eq 0)
}

function Cmd-Build {
    # packages.config restore is a SEPARATE pass: plain `-restore` only handles
    # PackageReference, and Files.App.Launcher.vcxproj still uses packages.config.
    Invoke-MSBuild @('-t:restore', '-p:RestorePackagesConfig=true', 'Files.slnx',
                     "-p:Configuration=$Configuration", "-p:Platform=$Platform",
                     '-v:quiet', '-clp:ErrorsOnly')
    Write-Output "RESTORE_EXIT=$($script:LastExit)"

    # Always the whole solution: the C++ projects and Files.App.Server are
    # solution-level BuildDependency entries, not ProjectReferences.
    Invoke-MSBuild @('-restore', 'Files.slnx',
                     "-p:Configuration=$Configuration", "-p:Platform=$Platform",
                     '-v:quiet', '-clp:ErrorsOnly')
    Write-Output "BUILD_EXIT=$($script:LastExit)"
    $script:Ok = ($script:LastExit -eq 0)
}

function Cmd-Deploy {
    $script:Ok = $false
    $layout = Get-LayoutDir
    if (-not $layout) { Write-Output 'DEPLOY=NOLAYOUT (run build first)'; return }
    Write-Output "LAYOUT=$layout"

    # --- WinAppSDK framework package ---------------------------------------
    # The manifest hard-requires a MinVersion; the runtime MSIX ships inside the
    # already-restored NuGet package, so no download is needed.
    $manifest = Join-Path $layout 'AppxManifest.xml'
    $dep = ([xml](Get-Content $manifest)).Package.Dependencies.PackageDependency |
           Where-Object { $_.Name -like 'Microsoft.WindowsAppRuntime*' } | Select-Object -First 1
    if ($dep) {
        $need = [version]$dep.MinVersion
        $have = Get-AppxPackage -Name $dep.Name -EA SilentlyContinue |
                ForEach-Object { [version]$_.Version } | Sort-Object -Descending | Select-Object -First 1
        Write-Output "WINAPPSDK_NEED=$need HAVE=$have"
        if (-not $have -or $have -lt $need) {
            $ver = "$($need.Major).$($need.Minor).$($need.Build)"
            $msixDir = "$env:USERPROFILE\.nuget\packages\microsoft.windowsappsdk.runtime\$ver\tools\MSIX\win10-x64"
            if (Test-Path $msixDir) {
                foreach ($n in 'Microsoft.WindowsAppRuntime.2', 'Microsoft.WindowsAppRuntime.Main.2',
                               'Microsoft.WindowsAppRuntime.Singleton.2', 'Microsoft.WindowsAppRuntime.DDLM.2') {
                    $f = Join-Path $msixDir "$n.msix"
                    if (Test-Path $f) {
                        try { Add-AppxPackage -Path $f -EA Stop; Write-Output "RUNTIME_OK=$n" }
                        catch { Write-Output "RUNTIME_FAIL=$n" }
                    }
                }
            } else { Write-Output "RUNTIME_MSIX_MISSING=$msixDir" }
        }
    }

    # --- layout repair ------------------------------------------------------
    # Assets\AppTiles\** is MSIX *package* content and is NOT marked
    # CopyToOutputDirectory, so it never lands in bin\. Registering bin\ without
    # it makes `new SystemTrayIcon()` throw DirectoryNotFoundException on Logo.ico
    # during startup -- swallowed by a fire-and-forget, leaving a frozen splash.
    $tile = Join-Path $layout 'Assets\AppTiles\Dev\Logo.ico'
    if (-not (Test-Path $tile)) {
        Copy-Item (Join-Path $RepoRoot 'src\Files.App\Assets\AppTiles') `
                  (Join-Path $layout 'Assets\') -Recurse -Force
        Write-Output 'REPAIR=AppTiles copied'
    }
    Write-Output "LOGO_ICO=$(Test-Path $tile)"
    Write-Output "SERVER_EXE=$(Test-Path (Join-Path $layout 'Files.App.Server.exe'))"

    try { Add-AppxPackage -Register $manifest -EA Stop; Write-Output 'REGISTER=OK' }
    catch { Write-Output "REGISTER=FAIL $($_.Exception.Message)"; return }

    $pkg = Get-AppxPackage -Name $PkgName
    Write-Output "AUMID=$($pkg.PackageFamilyName)!$AppId"
    $script:Ok = $true
}

function Cmd-Launch {
    $script:Ok = $false
    $pkg = Get-AppxPackage -Name $PkgName -EA SilentlyContinue
    if (-not $pkg) { Write-Output 'LAUNCH=NOTREGISTERED (run deploy first)'; return }
    Get-Process -Name 'Files' -EA SilentlyContinue | Stop-Process -Force
    Remove-Item $LogPath -Force -EA SilentlyContinue
    Start-Sleep -Seconds 2

    Start-Process "shell:AppsFolder\$($pkg.PackageFamilyName)!$AppId"

    # Splash title is exactly 'Files'; a loaded page reads '<Page> - Files'.
    # Debug builds are not ReadyToRun, so first paint takes a while.
    # Require two consecutive matches: the tab title can be restored (Startup
    # setting "Continue where you left off") before the content actually renders.
    $hits = 0
    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Seconds 3
        $p = Get-AppWindow
        if ($p -and $p.MainWindowTitle -match ' - Files$') { $hits++ } else { $hits = 0 }
        if ($hits -ge 2) {
            Write-Output "LAUNCH=OK title='$($p.MainWindowTitle)' after=$(($i+1)*3)s"
            $script:Ok = $true
            return
        }
    }
    $p = Get-AppWindow
    Write-Output "LAUNCH=STUCK title='$($p.MainWindowTitle)' (see: driver.ps1 log)"
}

function Cmd-Status {
    $p = Get-Process -Name 'Files' -EA SilentlyContinue
    if ($p) {
        foreach ($x in $p) {
            Write-Output ("PROC=Files pid={0} cpu={1}s responding={2} title='{3}'" -f `
                $x.Id, [math]::Round($x.CPU, 1), $x.Responding, $x.MainWindowTitle)
        }
    } else { Write-Output 'PROC=none' }
    $srv = Get-Process -Name 'Files.App.Server' -EA SilentlyContinue
    Write-Output "SERVER_PROC=$(if ($srv) { $srv.Id } else { 'none' })"
    $pkg = Get-AppxPackage -Name $PkgName -EA SilentlyContinue
    Write-Output "PACKAGE=$(if ($pkg) { $pkg.PackageFullName } else { 'not registered' })"
    Write-Output "LOG=$LogPath exists=$(Test-Path $LogPath)"
}

function Cmd-Log {
    $n = if ($Arg1) { [int]$Arg1 } else { 30 }
    if (Test-Path $LogPath) { Get-Content $LogPath -Tail $n } else { Write-Output 'LOG=missing' }
}

function Cmd-Shot {
    $path = if ($Arg1) { $Arg1 } else { Join-Path $OutDir 'app.png' }
    $p = Get-AppWindow
    if ($p) { [FilesWin]::ShowWindow($p.MainWindowHandle, 9) | Out-Null; Start-Sleep -Seconds 1 }
    Save-WindowShot -Path $path
}

function Cmd-Click {
    if (-not $Arg1 -or -not $Arg2) { Write-Output 'CLICK=USAGE click <x> <y>'; return }
    $p = Get-AppWindow
    if (-not $p) { Write-Output 'CLICK=NOWINDOW'; return }
    $h = $p.MainWindowHandle
    $fg = [FilesWin]::ForceForeground($h)
    Write-Output "FOREGROUND=$fg"
    if (-not $fg) { Write-Output 'CLICK=ABORT window would not take focus; clicks would hit another app'; return }
    Start-Sleep -Seconds 2
    # Re-read the rect immediately before clicking: the window can move between
    # a screenshot and the click, which silently shifts every coordinate.
    $r = New-Object FilesWin+RC
    [FilesWin]::GetWindowRect($h, [ref]$r) | Out-Null
    $x = $r.L + [int]$Arg1; $y = $r.T + [int]$Arg2
    [FilesWin]::SetCursorPos($x, $y) | Out-Null; Start-Sleep -Milliseconds 400
    [FilesWin]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)
    [FilesWin]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Seconds 6
    $p.Refresh()
    Write-Output "CLICK=($Arg1,$Arg2) screen=($x,$y) title='$($p.MainWindowTitle)'"
    Save-WindowShot -Path (Join-Path $OutDir 'click.png')
}

function Cmd-Stop {
    Get-Process -Name 'Files', 'Files.App.Server' -EA SilentlyContinue | Stop-Process -Force
    Write-Output 'STOP=OK'
}

$script:Ok = $true

switch ($Command.ToLower()) {
    'prereqs' { Cmd-Prereqs }
    'build'   { Cmd-Build }
    'deploy'  { Cmd-Deploy }
    'launch'  { Cmd-Launch }
    'status'  { Cmd-Status }
    'log'     { Cmd-Log }
    'shot'    { Cmd-Shot }
    'click'   { Cmd-Click }
    'stop'    { Cmd-Stop }
    'all'     {
        Cmd-Prereqs; if (-not $script:Ok) { Write-Output 'ABORT=prereqs'; break }
        Cmd-Build;   if (-not $script:Ok) { Write-Output 'ABORT=build';   break }
        Cmd-Deploy;  if (-not $script:Ok) { Write-Output 'ABORT=deploy';  break }
        Cmd-Launch;  if (-not $script:Ok) { Write-Output 'ABORT=launch';  break }
        Cmd-Shot
    }
    default {
        Write-Output 'commands: prereqs build deploy launch status log shot click stop all'
    }
}
