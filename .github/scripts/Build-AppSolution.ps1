# Copyright (c) Files Community
# Licensed under the MIT License.

param(
    [string]$ReleaseBranch =            "Debug", # Debug, Release, SideloadPreview, SideloadStable, StorePreview, or StoreStable
    [string]$SolutionPath =             "Files.slnx",
    [string]$StartupProjectPath =       "",
    [string]$Platform =                 "x64",
    [string]$Configuration =            "Debug",
    [string]$AppxBundlePlatforms =      "x64|arm64",
    [string]$AppxPackageDir =           "",
    [string]$AppInstallerUrl =          "", # Sideload only
    [string]$AppxPackageCertKeyFile =   "" # Debug only
)

# Restore the solution
msbuild $SolutionPath /t:Restore /p:Platform=$Platform /p:Configuration=$Configuration /p:PublishReadyToRun=true

if ($ReleaseBranch -eq "Debug")
{
    msbuild $StartupProjectPath `
        /t:Build `
        /clp:ErrorsOnly `
        /p:Platform=$Platform `
        /p:Configuration=$Configuration `
        /p:AppxBundle=Never `
        /v:quiet
}
elseif ($ReleaseBranch -eq "Release")
{
    if ($Platform -eq "x64")
    {
        msbuild $StartupProjectPath `
            /t:Build `
            /clp:ErrorsOnly `
            /p:Platform=$Platform `
            /p:Configuration=$Configuration `
            /p:AppxBundlePlatforms=$Platform `
            /p:AppxBundle=Always `
            /p:UapAppxPackageBuildMode=SideloadOnly `
            /p:AppxPackageDir=$AppxPackageDir `
            /p:AppxPackageSigningEnabled=true `
            /p:PackageCertificateKeyFile=$AppxPackageCertKeyFile `
            /p:PackageCertificatePassword="" `
            /p:PackageCertificateThumbprint="" `
            /v:quiet
    }
    else
    {
        msbuild $StartupProjectPath `
            /t:Build `
            /clp:ErrorsOnly `
            /p:Platform=$Platform `
            /p:Configuration=$Configuration `
            /p:AppxBundle=Never `
            /v:quiet
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
        /p:AppxBundlePlatforms=$AppxBundlePlatforms `
        /p:AppxPackageDir=$AppxPackageDir `
        /p:AppxBundle=Always `
        /p:UapAppxPackageBuildMode=Sideload `
        /p:GenerateAppInstallerFile=True `
        /p:AppInstallerUri=$AppInstallerUrl `
        /v:quiet

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
        /p:AppxBundlePlatforms=$AppxBundlePlatforms `
        /p:AppxPackageDir=$AppxPackageDir `
        /p:AppxBundle=Always `
        /p:UapAppxPackageBuildMode=StoreUpload `
        /v:quiet
}
