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
        /p:Configuration=$Configuration `
        /p:AppxBundle=Never `
        /p:GenerateAppxPackageOnBuild=False
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
            /p:AppxPackageSigningEnabled=true `
            /p:PackageCertificateKeyFile=$AppxPackageCertKeyFilePath
    }
    else
    {
        msbuild $StartupProjectPath `
            /clp:ErrorsOnly `
            /p:Platform=$Platform `
            /p:Configuration=$Configuration `
            /p:AppxBundle=Never `
            /p:GenerateAppxPackageOnBuild=False
    }
}
elseif ($ReleaseBranch -contains "Sideload")
{
    msbuild $StartupProjectPath `
        /t:Build `
        /t:_GenerateAppxPackage `
        /clp:ErrorsOnly `
        /p:Platform=$Platform `
        /p:Configuration=$Configuration `
        /p:UapAppxPackageBuildMode=SideloadOnly `
        /p:GenerateAppInstallerFile=True `
        /p:AppInstallerUri=$AppInstallerUrl `

    $newSchema = 'http://schemas.microsoft.com/appx/appinstaller/2018'
    $localFilePath = '$AppxPackageDir/Files.Package.appinstaller'
    $fileContent = Get-Content $localFilePath
    $fileContent = $fileContent.Replace('http://schemas.microsoft.com/appx/appinstaller/2017/2', $newSchema)
    $fileContent | Set-Content $localFilePath
}
elseif ($ReleaseBranch -contains "Store")
{
    msbuild $StartupProjectPath `
        /t:Build `
        /t:_GenerateAppxPackage `
        /clp:ErrorsOnly `
        /p:Platform=$Platform `
        /p:Configuration=$Configuration `
        /p:AppxPackageDir=$AppxPackageDir `
        /p:UapAppxPackageBuildMode=StoreUpload `
}
