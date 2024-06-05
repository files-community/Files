# Copyright (c) 2024 Files Community
# Licensed under the MIT License. See the LICENSE.

param(
    [string]$Branch = "",
    [string]$PackageProjectDir = "",
    [string]$Publisher = "",
    [string]$WorkingDir = "",
    [string]$SecretBingMapsKey = "",
    [string]$SecretSentry = "",
    [string]$SecretGitHubOAuthClientId = ""
)

[xml]$xmlDoc = Get-Content "$PackageProjectDir\Package.appxmanifest"
$xmlDoc.Package.Identity.Publisher="$Publisher"

if ($Branch -eq "Preview")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="FilesPreview"
    $xmlDoc.Package.Properties.DisplayName="Files - Preview"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="Files - Preview"

    Get-ChildItem "$WorkingDir\src" -Include *.csproj, *.appxmanifest, *.wapproj, *.xaml -recurse | ForEach -Process
    {
        (Get-Content $_ -Raw | ForEach -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Preview" }) |
        Set-Content $_ -NoNewline
    }
}
elseif ($Branch -eq "Stable")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="Files"
    $xmlDoc.Package.Properties.DisplayName="Files"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="Files"

    Get-ChildItem "$WorkingDir\src" -Include *.csproj, *.appxmanifest, *.wapproj, *.xaml -recurse | ForEach -Process
    {
        (Get-Content $_ -Raw | ForEach -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Release" }) |
        Set-Content $_ -NoNewline
    }
}
elseif ($Branch -eq "Store")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="49306atecsolution.FilesUWP"
    $xmlDoc.Package.Properties.DisplayName="Files"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="Files"

    # Remove an capability that is used for the sideload
    $nsmgr = New-Object System.Xml.XmlNamespaceManager($xmlDoc.NameTable)
    $nsmgr.AddNamespace("pkg", "http://schemas.microsoft.com/appx/manifest/foundation/windows10")
    $nsmgr.AddNamespace("rescap", "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities")
    $pm = $xmlDoc.SelectSingleNode("/pkg:Package/pkg:Capabilities/rescap:Capability[@Name='packageManagement']", $nsmgr)
    $xmlDoc.Package.Capabilities.RemoveChild($pm)

    Get-ChildItem "$WorkingDir\src" -Include *.csproj, *.appxmanifest, *.wapproj, *.xaml -recurse | ForEach -Process
    {
        (Get-Content $_ -Raw | ForEach -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Release" }) |
        Set-Content $_ -NoNewline
    }
}

$xmlDoc.Save("$PackageProjectDir\Package.appxmanifest")

Get-ChildItem "$WorkingDir\src" -Include *.cs -recurse | ForEach-Object -Process
{
    (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "bingmapskey.secret", "$SecretBingMapsKey" }) |
    Set-Content $_ -NoNewline
}

Get-ChildItem "$WorkingDir\src" -Include *.cs -recurse | ForEach-Object -Process
{
    (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "sentry.secret", "$SecretSentry" }) |
    Set-Content $_ -NoNewline
}

Get-ChildItem "$WorkingDir\src" -Include *.cs -recurse | ForEach-Object -Process
{
    (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "githubclientid.secret", "$SecretGitHubOAuthClientId" }) |
    Set-Content $_ -NoNewline
}
