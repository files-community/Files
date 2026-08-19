# Copyright (c) Files Community
# SPDX-License-Identifier: MPL-2.0

# Zips the portable publish output per platform, computes hashes, and generates
# the Files.Portable.json update manifest consumed by PortableUpdateService.

param(
    [string]$AppProjectDir = "",   # e.g. src\Files.App
    [string]$Configuration = "Release",
    [string]$Platforms = "x64|arm64",
    [string]$Version = "",
    [string]$CdnBaseUrl = "",      # e.g. https://cdn.files.community/files/stable/
    [string]$StagingDir = ""       # local folder mirroring the CDN destination
)

$ErrorActionPreference = "Stop"

if (-not $CdnBaseUrl.EndsWith('/')) { $CdnBaseUrl += '/' }

$portableDir = Join-Path $StagingDir "portable"
New-Item -ItemType Directory -Force $portableDir | Out-Null

$downloads = [ordered]@{}

foreach ($platform in $Platforms -split '\|') {
    $publishDir = Get-ChildItem -Path (Join-Path $AppProjectDir "bin\$Configuration") -Directory |
        ForEach-Object { Join-Path $_.FullName "win-$platform\publish" } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1

    if (-not $publishDir) {
        throw "No publish output found for platform '$platform' under '$AppProjectDir\bin\$Configuration'."
    }

    $zipName = "Files.Portable.$platform.zip"
    $zipPath = Join-Path $portableDir $zipName

    Write-Host "Zipping $publishDir -> $zipPath"
    Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -CompressionLevel Optimal -Force

    $downloads[$platform] = [ordered]@{
        url    = "${CdnBaseUrl}portable/$zipName"
        sha256 = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

$manifest = [ordered]@{
    version   = $Version
    downloads = $downloads
}

$manifestPath = Join-Path $StagingDir "Files.Portable.json"
$manifest | ConvertTo-Json -Depth 4 | Out-File -FilePath $manifestPath -Encoding utf8

Write-Host "Wrote manifest ${manifestPath}:"
Get-Content $manifestPath
