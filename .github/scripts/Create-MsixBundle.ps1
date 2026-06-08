# Copyright (c) Files Community
# Licensed under the MIT License.

# Creates an .msixbundle from individual per-platform .msix packages produced
# by single-project MSIX packaging. Optionally generates an .appinstaller file
# for sideload deployments, and an .msixupload for Store submissions.

param(
    [Parameter(Mandatory)]
    [string]$AppxPackageDir,

    [Parameter(Mandatory)]
    [string]$BundleName,

    [string]$Version = "",

    [string]$PackageManifestPath = "",

    [string]$AppInstallerUri = "",

    [ValidateSet("Sideload", "StoreUpload", "SideloadOnly")]
    [string]$BuildMode = "Sideload"
)

$ErrorActionPreference = "Stop"

function Ensure-Directory([string]$Path) {
    if (-not (Test-Path $Path)) { New-Item -ItemType Directory -Path $Path | Out-Null }
}

# Canonical arch names for the WAP-compatible folder layout
$archMap = @{ 'arm64' = 'ARM64'; 'x64' = 'x64'; 'x86' = 'x86' }

# --- Locate makeappx.exe ---

$sdkRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
$makeAppx = Get-ChildItem -Path $sdkRoot -Filter "makeappx.exe" -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.DirectoryName -match '\\x64$' } |
    Sort-Object { [version]($_.DirectoryName -replace '^.*\\bin\\([^\\]+)\\.*$','$1') } -Descending |
    Select-Object -First 1
if ($null -eq $makeAppx) {
    Write-Error "makeappx.exe not found under '$sdkRoot'"
    exit 1
}
Write-Host "Using makeappx: $($makeAppx.FullName)"

# --- Discover per-platform .msix packages ---

$msixFiles = Get-ChildItem -Path $AppxPackageDir -Filter "*.msix" -Recurse |
    Where-Object { $_.DirectoryName -notmatch '\\Dependencies\\' }
if ($msixFiles.Count -eq 0) {
    Write-Error "No .msix files found in '$AppxPackageDir'"
    exit 1
}
Write-Host "Found $($msixFiles.Count) .msix package(s):"
$msixFiles | ForEach-Object { Write-Host "  $_" }

if ($Version -eq "" -and $PackageManifestPath -ne "" -and (Test-Path $PackageManifestPath)) {
    [xml]$versionManifest = Get-Content $PackageManifestPath
    $Version = $versionManifest.Package.Identity.Version
    Write-Host "Version from manifest: $Version"
}
if ($Version -eq "") {
    $Version = $msixFiles[0].BaseName -replace '^[^_]+_([^_]+)_.*$','$1'
    Write-Host "Detected version from filename: $Version"
}

$platformList = $msixFiles | ForEach-Object { $_.BaseName -replace '.*_(\w+)$','$1' } | Sort-Object -Descending
$platforms = $platformList -join '_'

# --- Create .msixbundle ---

$mappingDir = Join-Path $AppxPackageDir "_bundletemp"
if (Test-Path $mappingDir) { Remove-Item $mappingDir -Recurse -Force }
New-Item -ItemType Directory -Path $mappingDir | Out-Null
foreach ($msix in $msixFiles) { Copy-Item $msix.FullName -Destination $mappingDir }

$bundlePath = Join-Path $AppxPackageDir "$BundleName.msixbundle"
if (Test-Path $bundlePath) { Remove-Item $bundlePath -Force }

Write-Host "Creating msixbundle at: $bundlePath"
& $makeAppx.FullName bundle /d $mappingDir /p $bundlePath /bv $Version /o
if ($LASTEXITCODE -ne 0) {
    Write-Error "MakeAppx bundle creation failed with exit code $LASTEXITCODE"
    exit 1
}
Remove-Item $mappingDir -Recurse -Force
Write-Host "Successfully created: $bundlePath"

# --- Reorganize into WAP-compatible folder structure for CDN upload ---
# Target layout: {BundleName}_{Version}_Test/{BundleName}_{Version}_{platforms}.msixbundle
#                {BundleName}_{Version}_Test/Dependencies/{arch}/...

$bundleFolder = "${BundleName}_${Version}_Test"
$bundleFolderPath = Join-Path $AppxPackageDir $bundleFolder
Ensure-Directory $bundleFolderPath

$organizedBundleName = "${BundleName}_${Version}_${platforms}.msixbundle"
$organizedBundlePath = Join-Path $bundleFolderPath $organizedBundleName
Move-Item $bundlePath $organizedBundlePath -Force
Write-Host "Moved bundle to: $organizedBundlePath"

# --- Merge dependency folders from each per-platform build ---

$organizedDepsDir = Join-Path $bundleFolderPath "Dependencies"
foreach ($msix in $msixFiles) {
    $perPlatDepsDir = Join-Path $msix.DirectoryName "Dependencies"
    if (-not (Test-Path $perPlatDepsDir)) { continue }

    Get-ChildItem -Path $perPlatDepsDir -Directory | ForEach-Object {
        $archName = $_.Name
        $canonicalArch = $archMap[$archName]
        if (-not $canonicalArch) { return }
        if (-not ($platformList -contains $archName)) { return }

        $targetArchDir = Join-Path $organizedDepsDir $canonicalArch
        Ensure-Directory $targetArchDir
        Get-ChildItem -Path $_.FullName -File | ForEach-Object {
            $destFile = Join-Path $targetArchDir $_.Name
            if (-not (Test-Path $destFile)) {
                Copy-Item $_.FullName -Destination $destFile
                Write-Host "  Copied dependency: $destFile"
            }
        }
    }
}

# --- Add VCLibs framework packages (not produced by single-project builds) ---

$vcLibsSdkBase = "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows Kits\10\ExtensionSDKs"
$vcLibsPackages = @(
    @{ SdkFolder = "Microsoft.VCLibs";         FileTemplate = "Microsoft.VCLibs.{0}.14.00.appx" },
    @{ SdkFolder = "Microsoft.VCLibs.Desktop"; FileTemplate = "Microsoft.VCLibs.{0}.14.00.Desktop.appx" }
)

foreach ($platform in $platformList) {
    $canonicalArch = $archMap[$platform]
    if (-not $canonicalArch) { continue }

    $targetArchDir = Join-Path $organizedDepsDir $canonicalArch
    Ensure-Directory $targetArchDir

    foreach ($vcLib in $vcLibsPackages) {
        $fileName = $vcLib.FileTemplate -f $canonicalArch
        $destPath = Join-Path $targetArchDir $fileName
        if (Test-Path $destPath) { continue }

        $sdkPath = Join-Path $vcLibsSdkBase "$($vcLib.SdkFolder)\14.0\Appx\Retail\$platform\$fileName"
        if (Test-Path $sdkPath) {
            Copy-Item $sdkPath -Destination $destPath
            Write-Host "  Copied VCLibs from SDK: $destPath"
        } else {
            Write-Host "  Downloading $fileName for $platform..."
            try {
                Invoke-WebRequest -Uri "https://aka.ms/$fileName" -OutFile $destPath -UseBasicParsing
                Write-Host "  Downloaded VCLibs: $destPath"
            } catch {
                Write-Warning "  Failed to download $fileName for ${platform}: $_"
            }
        }
    }
}

# --- Collect symbol files and clean up per-platform build folders ---

foreach ($msix in $msixFiles) {
    Get-ChildItem -Path $msix.DirectoryName -Filter "*.appxsym" -ErrorAction SilentlyContinue | ForEach-Object {
        $symPlatform = $_.BaseName -replace '.*_(\w+)$','$1'
        $newSymPath = Join-Path $bundleFolderPath "${BundleName}_${Version}_${symPlatform}.appxsym"
        Move-Item $_.FullName $newSymPath -Force
        Write-Host "Moved symbol file to: $newSymPath"
    }
}

Get-ChildItem -Path $AppxPackageDir -Filter "*.msixupload" -ErrorAction SilentlyContinue |
    ForEach-Object { Remove-Item $_.FullName -Force; Write-Host "Removed per-platform upload: $($_.Name)" }

foreach ($msix in $msixFiles) {
    if (Test-Path $msix.DirectoryName) {
        Remove-Item $msix.DirectoryName -Recurse -Force
        Write-Host "Removed per-platform dir: $($msix.DirectoryName)"
    }
}

# --- Generate .msixupload for Store submissions ---

if ($BuildMode -eq "StoreUpload") {
    $uploadName = "${BundleName}_${Version}_${platforms}_bundle.msixupload"
    $uploadPath = Join-Path $AppxPackageDir $uploadName
    if (Test-Path $uploadPath) { Remove-Item $uploadPath -Force }

    Write-Host "Creating msixupload at: $uploadPath"
    $zipPath = "$uploadPath.zip"
    Compress-Archive -Path $organizedBundlePath -DestinationPath $zipPath -Force
    Move-Item $zipPath $uploadPath -Force
    Write-Host "Successfully created: $uploadPath"
}

# --- Generate .appinstaller for sideload deployments ---

if ($AppInstallerUri -ne "" -and ($BuildMode -eq "Sideload" -or $BuildMode -eq "SideloadOnly")) {
    $appInstallerPath = Join-Path $AppxPackageDir "$BundleName.appinstaller"

    if ($PackageManifestPath -ne "" -and (Test-Path $PackageManifestPath)) {
        [xml]$manifestXml = Get-Content $PackageManifestPath
        $packageName = $manifestXml.Package.Identity.Name
        $packagePublisher = $manifestXml.Package.Identity.Publisher
    } else {
        Write-Warning "PackageManifestPath not provided; falling back to BundleName for identity"
        $packageName = $BundleName
        $packagePublisher = ""
    }

    $bundleUri = "${AppInstallerUri}${bundleFolder}/${organizedBundleName}"

    # Build dependency XML entries by reading each package's embedded manifest
    $dependencyEntries = ""
    if (Test-Path $organizedDepsDir) {
        Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
        Get-ChildItem -Path $organizedDepsDir -Recurse -Include *.appx, *.msix | ForEach-Object {
            $depArch = Split-Path (Split-Path $_.FullName -Parent) -Leaf

            $depZip = [System.IO.Compression.ZipFile]::OpenRead($_.FullName)
            try {
                $entry = $depZip.Entries | Where-Object { $_.FullName -eq "AppxManifest.xml" } | Select-Object -First 1
                if ($null -eq $entry) { return }
                $reader = New-Object System.IO.StreamReader($entry.Open())
                [xml]$depManifest = $reader.ReadToEnd()
                $reader.Close()
            } finally { $depZip.Dispose() }

            $nsMgr = New-Object System.Xml.XmlNamespaceManager($depManifest.NameTable)
            $nsMgr.AddNamespace("pkg", "http://schemas.microsoft.com/appx/manifest/foundation/windows10")
            $id = $depManifest.SelectSingleNode("/pkg:Package/pkg:Identity", $nsMgr)
            if ($null -eq $id) { return }

            $arch = $id.GetAttribute("ProcessorArchitecture")
            if ([string]::IsNullOrEmpty($arch)) { $arch = $depArch }
            $depUri = "${AppInstallerUri}${bundleFolder}/Dependencies/$depArch/$($_.Name)"

            $dependencyEntries += @"

		<Package
			Name="$($id.GetAttribute("Name"))"
			Publisher="$($id.GetAttribute("Publisher"))"
			ProcessorArchitecture="$arch"
			Uri="$depUri"
			Version="$($id.GetAttribute("Version"))" />
"@
        }
    }

    if ($dependencyEntries -eq "") {
        Write-Warning "No dependency packages found"
    } else {
        Write-Host "Discovered dependencies:$dependencyEntries"
    }

    @"
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller
	Uri="${AppInstallerUri}${BundleName}.appinstaller"
	Version="$Version" xmlns="http://schemas.microsoft.com/appx/appinstaller/2018">
	<MainBundle
		Name="$packageName"
		Publisher="$packagePublisher"
		Version="$Version"
		Uri="$bundleUri" />
	<Dependencies>$dependencyEntries
	</Dependencies>
</AppInstaller>
"@ | Set-Content -Path $appInstallerPath -Encoding UTF8

    Write-Host "Created appinstaller at: $appInstallerPath"
}
