<#
.SYNOPSIS
    One-shot packaged-build test cycle: stop service, uninstall, build, install, activate.

.DESCRIPTION
    Replaces the ~10-step manual sequence with a single command. Useful when
    iterating on packaging-affecting changes (manifest, csproj content staging,
    AppxBundlePlatforms, certificates, etc.).

    Run from an ELEVATED PowerShell. Add-AppxPackage of packaged services
    requires admin.

.PARAMETER Platform
    x64 (default), x86, or arm64.

.PARAMETER Configuration
    Release (default) or Debug.

.PARAMETER NoActivate
    Skip the post-install Start-Process. Useful for CI / scripted runs.

.PARAMETER SkipBuild
    Reuse the bundle already at artifacts/AppxPackages/. Useful when the
    build is unchanged and you just want to re-install.

.EXAMPLE
    .\scripts\dev-cycle.ps1
    .\scripts\dev-cycle.ps1 -Platform x86
    .\scripts\dev-cycle.ps1 -SkipBuild -NoActivate
#>
[CmdletBinding()]
param(
    [ValidateSet('x64', 'x86', 'arm64')]
    [string]$Platform = 'x64',

    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [switch]$NoActivate,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$msbuild  = 'C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe'
$bundle   = Join-Path $repoRoot "artifacts\AppxPackages\Files.App_4.1.0.0_Test\Files.App_4.1.0.0_$Platform.msixbundle"
$depMsix  = Join-Path $repoRoot "artifacts\AppxPackages\Files.App_4.1.0.0_Test\Dependencies\$Platform\Microsoft.WindowsAppRuntime.1.8.msix"
$aumid    = 'FilesDev_j4wp4nz5mtqsg!App'

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }

# --- Elevation check ---
$id = [System.Security.Principal.WindowsIdentity]::GetCurrent()
if (-not (New-Object System.Security.Principal.WindowsPrincipal($id)).IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Must run elevated. Re-launch PowerShell as Administrator.'
}

# --- Stop service ---
Write-Step 'Stopping FilesSearchService (if running)'
Stop-Service FilesSearchService -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

# --- Uninstall existing ---
Write-Step 'Uninstalling existing FilesDev'
Get-AppxPackage FilesDev -AllUsers -ErrorAction SilentlyContinue |
    Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# --- Build ---
if (-not $SkipBuild) {
    Write-Step "Building $Configuration|$Platform"
    Push-Location $repoRoot
    try {
        $args = @(
            'src/Files.App/Files.App.csproj', '-t:Build',
            "-p:Platform=$Platform",
            "-p:Configuration=$Configuration",
            "-p:AppxBundlePlatforms=$Platform",
            "-p:AppxPackageDir=$repoRoot\artifacts\AppxPackages\",
            '-p:AppxBundle=Always',
            '-p:UapAppxPackageBuildMode=SideloadOnly',
            '-p:GenerateAppxPackageOnBuild=true',
            '-p:AppxPackageSigningEnabled=true',
            '-p:PackageCertificateKeyFile=src\Files.App\Files.App_TemporaryKey.pfx',
            '-v:minimal'
        )
        & $msbuild @args | Tee-Object -Variable buildLog | Out-Null
        if ($LASTEXITCODE -ne 0) {
            # First pass can fail on a clean tree with manifest-validation error
            # (the Content Condition=Exists race). Retry once — the SearchService
            # output exists on disk from the failed first attempt.
            $manifestRace = $buildLog -match 'doesn''t exist in the package'
            if ($manifestRace) {
                Write-Host '   (manifest-validation race on first pass — retrying)' -ForegroundColor Yellow
                & $msbuild @args | Out-Host
            }
            if ($LASTEXITCODE -ne 0) {
                throw "Build failed with exit $LASTEXITCODE"
            }
        }
    } finally {
        Pop-Location
    }
}

# --- Install ---
Write-Step "Installing $(Split-Path -Leaf $bundle)"
if (-not (Test-Path $bundle)) {
    throw "Bundle not found: $bundle"
}
Add-AppxPackage -Path $bundle -DependencyPath $depMsix -ErrorAction Stop
Start-Sleep -Seconds 1

$pkg = Get-AppxPackage FilesDev -AllUsers | Select-Object -First 1
Write-Host "    Installed: $($pkg.PackageFullName)" -ForegroundColor Green

# --- Activate ---
if (-not $NoActivate) {
    Write-Step 'Activating'
    Start-Process explorer.exe "shell:appsFolder\$aumid"

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $seen = $false
    while ($sw.Elapsed.TotalSeconds -lt 8) {
        $p = Get-Process Files -ErrorAction SilentlyContinue
        if ($p -and -not $seen) {
            Write-Host ("    Files.exe PID={0} caught at T+{1:F0}ms" -f ($p.Id -join ','), $sw.Elapsed.TotalMilliseconds) -ForegroundColor Green
            $seen = $true
            break
        }
        Start-Sleep -Milliseconds 50
    }
    if (-not $seen) {
        Write-Host '    Files.exe never observed. Run scripts\debug-activation.ps1 to diagnose.' -ForegroundColor Yellow
    }
}

Write-Step 'Done'
