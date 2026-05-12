# Copyright (c) Files Community
# Licensed under the MIT License.

param(
    [string]$ReleaseBranch =              "Debug", # Debug, Release, SideloadPreview, SideloadStable, StorePreview, or StoreStable
    [string]$SolutionPath =               "Files.slnx",
    [string]$StartupProjectPath =         "",
    [string]$Platform =                   "x64",
    [string]$Configuration =              "Debug",
    [string]$AppxPackageDir =             "",
    [string]$AppInstallerUrl =            "", # Sideload only
    [string]$AppxPackageCertKeyFilePath = "" # Release|x64 only (fully qualified path)
)

msbuild $SolutionPath /t:Restore /p:Platform=$Platform /p:Configuration=$Configuration

if ($ReleaseBranch -eq "Debug")
{
    msbuild $StartupProjectPath `
        /clp:ErrorsOnly `
        /p:Platform=$Platform `
        /p:Configuration=$Configuration
}
elseif ($ReleaseBranch -eq "Release")
{
    if ($Platform -eq "x64")
    {
        Invoke-Expression "$PSScriptRoot/Generate-SelfCertPfx.ps1 -Destination $AppxPackageCertKeyFilePath"

        msbuild $StartupProjectPath `
            /clp:ErrorsOnly `
            /p:Platform=$Platform `
            /p:Configuration=$Configuration `
            /p:AppxBundlePlatforms=$Platform `
            /p:UapAppxPackageBuildMode=SideloadOnly `
            /p:AppxPackageDir=$AppxPackageDir `
            /p:GenerateAppInstallerFile=true `
            /p:AppInstallerUri=$AppxPackageDir `
            /p:AppxPackageSigningEnabled=true `
            /p:PackageCertificateKeyFile=$AppxPackageCertKeyFilePath
    }
    else
    {
        msbuild $StartupProjectPath `
            /clp:ErrorsOnly `
            /p:Platform=$Platform `
            /p:Configuration=$Configuration
    }
}
elseif ($ReleaseBranch -eq "SideloadPreview" -or $ReleaseBranch -eq "SideloadStable")
{
    msbuild $StartupProjectPath `
        /clp:ErrorsOnly `
        /p:Platform=$Platform `
        /p:Configuration=$Configuration `
        /p:UapAppxPackageBuildMode=SideloadOnly `
        /p:AppxPackageDir=$AppxPackageDir `
        /p:GenerateAppInstallerFile=true `
        /p:AppInstallerUri=$AppInstallerUrl

    # Path
    $localFilePath = Join-Path $AppxPackageDir "Files.App.appinstaller"

    # Update scheme (namespace URI string swap)
    $fileContent = Get-Content -LiteralPath $localFilePath -Raw
    $fileContent = $fileContent.Replace(
      'http://schemas.microsoft.com/appx/appinstaller/2017/2',
      'http://schemas.microsoft.com/appx/appinstaller/2018'
    )
    Set-Content -LiteralPath $localFilePath -Value $fileContent -Encoding UTF8

    # Delete UpdateSettings section (proper XML load)
    [xml]$xml = Get-Content -LiteralPath $localFilePath
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace("ai", "http://schemas.microsoft.com/appx/appinstaller/2018")

    $node = $xml.SelectSingleNode("//ai:UpdateSettings", $ns)
    if ($node) { [void]$node.ParentNode.RemoveChild($node) }

    $xml.Save($localFilePath)

    # Rename from 'Files.App' to 'Files.Package' in all occurrences in file/folder names to keep backwards compatibility with older versions of the installer/updater
    Get-ChildItem -Path $AppxPackageDir -Recurse -Force |
        Where-Object { $_.Name -like "*Files.App*" } |
        Sort-Object FullName -Descending |
        ForEach-Object {
            $newName = $_.Name -replace "Files\.App", "Files.Package"
            Rename-Item -LiteralPath $_.FullName -NewName $newName
        }
}
elseif ($ReleaseBranch -eq "StorePreview" -or $ReleaseBranch -eq "StoreStable")
{
    msbuild $StartupProjectPath `
        /clp:ErrorsOnly `
        /p:Platform=$Platform `
        /p:Configuration=$Configuration `
        /p:UapAppxPackageBuildMode=StoreOnly `
        /p:AppxPackageDir=$AppxPackageDir
}
