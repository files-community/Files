# Copyright (c) Files Community
# Licensed under the MIT License.

# Abstract:
#   Creates an .msixbundle from individual per-platform .msix packages produced
#   by single-project MSIX packaging. Optionally generates an .appinstaller file
#   for sideload deployments, and an .msixupload for Store submissions.

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

# Locate makeappx.exe from the Windows 10 SDK
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

# Find only the app .msix files (exclude dependency packages)
$msixFiles = Get-ChildItem -Path $AppxPackageDir -Filter "*.msix" -Recurse |
    Where-Object { $_.DirectoryName -notmatch '\\Dependencies\\' }
if ($msixFiles.Count -eq 0) {
    Write-Error "No .msix files found in '$AppxPackageDir'"
    exit 1
}

Write-Host "Found $($msixFiles.Count) .msix package(s):"
$msixFiles | ForEach-Object { Write-Host "  $_" }

# Auto-detect version from .msix filename if not provided (e.g. Files.App_4.0.29.0_x64.msix)
if ($Version -eq "") {
    $Version = $msixFiles[0].BaseName -replace '^[^_]+_([^_]+)_.*$','$1'
    Write-Host "Detected version: $Version"
}

# Create a temporary mapping file for MakeAppx
$mappingDir = Join-Path $AppxPackageDir "_bundletemp"
if (Test-Path $mappingDir) { Remove-Item $mappingDir -Recurse -Force }
New-Item -ItemType Directory -Path $mappingDir | Out-Null

# Copy all msix files into the flat temp directory
foreach ($msix in $msixFiles) {
    Copy-Item $msix.FullName -Destination $mappingDir
}

# Output bundle path
$bundlePath = Join-Path $AppxPackageDir "$BundleName.msixbundle"
if (Test-Path $bundlePath) { Remove-Item $bundlePath -Force }

# Use MakeAppx to create the bundle
Write-Host "Creating msixbundle at: $bundlePath"
& $makeAppx.FullName bundle /d $mappingDir /p $bundlePath /o
if ($LASTEXITCODE -ne 0) {
    Write-Error "MakeAppx bundle creation failed with exit code $LASTEXITCODE"
    exit 1
}

# Clean up temp directory
Remove-Item $mappingDir -Recurse -Force

Write-Host "Successfully created: $bundlePath"

# Reorganize output into WAP-compatible folder structure for CDN upload
# Old structure: Files.Package_4.0.28.0_Test/Files.Package_4.0.28.0_x64_arm64.msixbundle
#                Files.Package_4.0.28.0_Test/Dependencies/x64/...
# New single-project output: Files.App_4.0.29.0_x64_Test/Dependencies/x64/...
$platformList = $msixFiles | ForEach-Object { $_.BaseName -replace '.*_(\w+)$','$1' } | Sort-Object -Descending
$platforms = $platformList -join '_'
$bundleFolder = "${BundleName}_${Version}_Test"
$bundleFolderPath = Join-Path $AppxPackageDir $bundleFolder
if (-not (Test-Path $bundleFolderPath)) {
    New-Item -ItemType Directory -Path $bundleFolderPath | Out-Null
}

# Move the bundle into the organized folder with proper name
$organizedBundleName = "${BundleName}_${Version}_${platforms}.msixbundle"
$organizedBundlePath = Join-Path $bundleFolderPath $organizedBundleName
Move-Item $bundlePath $organizedBundlePath -Force
Write-Host "Moved bundle to: $organizedBundlePath"

# Map from build output folder names to canonical arch names used by the old WAP output
$archMap = @{ 'arm64' = 'ARM64'; 'x64' = 'x64'; 'x86' = 'x86'; 'win32' = $null }

# Merge dependency folders from each per-platform build (only for bundle platforms)
$organizedDepsDir = Join-Path $bundleFolderPath "Dependencies"
foreach ($msix in $msixFiles) {
    $perPlatDepsDir = Join-Path $msix.DirectoryName "Dependencies"
    if (-not (Test-Path $perPlatDepsDir)) { continue }

    Get-ChildItem -Path $perPlatDepsDir -Directory | ForEach-Object {
        $archDir = $_
        $archName = $archDir.Name

        # Skip dependency folders that don't match a bundle platform (e.g. win32, x86)
        $canonicalArch = $archMap[$archName]
        if ($null -eq $canonicalArch) { return }
        $matchesBundlePlatform = $platformList | Where-Object { $_ -eq $archName }
        if (-not $matchesBundlePlatform) { return }

        $targetArchDir = Join-Path $organizedDepsDir $canonicalArch
        if (-not (Test-Path $targetArchDir)) {
            New-Item -ItemType Directory -Path $targetArchDir | Out-Null
        }
        Get-ChildItem -Path $archDir.FullName -File | ForEach-Object {
            $destFile = Join-Path $targetArchDir $_.Name
            if (-not (Test-Path $destFile)) {
                Copy-Item $_.FullName -Destination $destFile
                Write-Host "  Copied dependency: $destFile"
            }
        }
    }
}

# Add VCLibs framework packages (not produced by single-project MSIX builds).
# Try the VS Extension SDK first; fall back to downloading from Microsoft's CDN.
$vcLibsSdkBase = "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows Kits\10\ExtensionSDKs"
$vcLibsCdnBase = "https://aka.ms"

$vcLibsPackages = @(
    @{ SdkFolder = "Microsoft.VCLibs";         FileTemplate = "Microsoft.VCLibs.{0}.14.00.appx" },
    @{ SdkFolder = "Microsoft.VCLibs.Desktop"; FileTemplate = "Microsoft.VCLibs.{0}.14.00.Desktop.appx" }
)

foreach ($platform in $platformList) {
    $canonicalArch = $archMap[$platform]
    if ($null -eq $canonicalArch) { continue }

    $targetArchDir = Join-Path $organizedDepsDir $canonicalArch
    if (-not (Test-Path $targetArchDir)) {
        New-Item -ItemType Directory -Path $targetArchDir | Out-Null
    }

    foreach ($vcLib in $vcLibsPackages) {
        $fileName = $vcLib.FileTemplate -f $canonicalArch     # e.g. Microsoft.VCLibs.ARM64.14.00.appx
        $destPath = Join-Path $targetArchDir $fileName
        if (Test-Path $destPath) { continue }

        $sdkPath = Join-Path $vcLibsSdkBase "$($vcLib.SdkFolder)\14.0\Appx\Retail\$platform\$fileName"
        if (Test-Path $sdkPath) {
            Copy-Item $sdkPath -Destination $destPath
            Write-Host "  Copied VCLibs from SDK: $destPath"
        } else {
            Write-Host "  Downloading $fileName for $platform..."
            try {
                Invoke-WebRequest -Uri "$vcLibsCdnBase/$fileName" -OutFile $destPath -UseBasicParsing
                Write-Host "  Downloaded VCLibs: $destPath"
            } catch {
                Write-Warning "  Failed to download $fileName for ${platform}: $_"
            }
        }
    }
}

# Clean up the per-platform build folders
foreach ($msix in $msixFiles) {
    $perPlatDir = $msix.DirectoryName
    if (Test-Path $perPlatDir) {
        Remove-Item $perPlatDir -Recurse -Force
        Write-Host "Removed per-platform dir: $perPlatDir"
    }
}

# Generate .msixupload for Store submissions
if ($BuildMode -eq "StoreUpload") {
    $uploadPath = Join-Path $AppxPackageDir "$BundleName.msixupload"
    if (Test-Path $uploadPath) { Remove-Item $uploadPath -Force }

    Write-Host "Creating msixupload at: $uploadPath"
    $zipPath = "$uploadPath.zip"
    Compress-Archive -Path $organizedBundlePath -DestinationPath $zipPath -Force
    Move-Item $zipPath $uploadPath -Force
    Write-Host "Successfully created: $uploadPath"
}

# Generate .appinstaller for sideload deployments
if ($AppInstallerUri -ne "" -and ($BuildMode -eq "Sideload" -or $BuildMode -eq "SideloadOnly")) {
    $appInstallerPath = Join-Path $AppxPackageDir "$BundleName.appinstaller"

    # Read package identity from manifest
    if ($PackageManifestPath -ne "" -and (Test-Path $PackageManifestPath)) {
        [xml]$manifestXml = Get-Content $PackageManifestPath
        $packageName = $manifestXml.Package.Identity.Name
    } else {
        Write-Warning "PackageManifestPath not provided or not found; falling back to BundleName for identity"
        $packageName = $BundleName
    }

    # Build the bundle URI using the reorganized folder structure
    $bundleUri = "${AppInstallerUri}${bundleFolder}/${organizedBundleName}"

    # Collect dependency files from the reorganized Dependencies folder
    $dependencyEntries = ""
    if (Test-Path $organizedDepsDir) {
        Get-ChildItem -Path $organizedDepsDir -Recurse -Include *.appx, *.msix | ForEach-Object {
            $depFile = $_
            # Determine the sub-folder name (arch folder under Dependencies)
            $depArch = Split-Path (Split-Path $depFile.FullName -Parent) -Leaf

            # Extract Name, Publisher, Version from the dependency package manifest
            Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction SilentlyContinue
            $depZip = [System.IO.Compression.ZipFile]::OpenRead($depFile.FullName)
            try {
                $depManifestEntry = $depZip.Entries | Where-Object { $_.FullName -eq "AppxManifest.xml" } | Select-Object -First 1
                if ($null -eq $depManifestEntry) { return }

                $depReader = New-Object System.IO.StreamReader($depManifestEntry.Open())
                [xml]$depManifest = $depReader.ReadToEnd()
                $depReader.Close()
            }
            finally {
                $depZip.Dispose()
            }

            $depNsMgr = New-Object System.Xml.XmlNamespaceManager($depManifest.NameTable)
            $depNsMgr.AddNamespace("pkg", "http://schemas.microsoft.com/appx/manifest/foundation/windows10")
            $depIdentity = $depManifest.SelectSingleNode("/pkg:Package/pkg:Identity", $depNsMgr)
            if ($null -eq $depIdentity) { return }

            $depName = $depIdentity.GetAttribute("Name")
            $depPublisher = $depIdentity.GetAttribute("Publisher")
            $depVersion = $depIdentity.GetAttribute("Version")
            $depProcessorArch = $depIdentity.GetAttribute("ProcessorArchitecture")
            if ([string]::IsNullOrEmpty($depProcessorArch)) { $depProcessorArch = $depArch }

            # Build the URI: {base}/{bundleFolder}/Dependencies/{archFolder}/{filename}
            $depRelPath = "Dependencies/$depArch/$($depFile.Name)"
            $depUri = "${AppInstallerUri}${bundleFolder}/$depRelPath"

            $dependencyEntries += @"

		<Package
			Name="$depName"
			Publisher="$depPublisher"
			ProcessorArchitecture="$depProcessorArch"
			Uri="$depUri"
			Version="$depVersion" />
"@
        }
    }

    if ($dependencyEntries -eq "") {
        Write-Warning "No dependency packages found"
    } else {
        Write-Host "Discovered dependencies:$dependencyEntries"
    }

    $appInstallerContent = @"
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller
	Uri="${AppInstallerUri}${BundleName}.appinstaller"
	Version="$Version" xmlns="http://schemas.microsoft.com/appx/appinstaller/2018">
	<MainBundle
		Name="$packageName"
		Version="$Version"
		Uri="$bundleUri" />
	<Dependencies>$dependencyEntries
	</Dependencies>
</AppInstaller>
"@

    Write-Host "Creating appinstaller at: $appInstallerPath"
    $appInstallerContent | Set-Content -Path $appInstallerPath -Encoding UTF8
    Write-Host "Successfully created: $appInstallerPath"
}
