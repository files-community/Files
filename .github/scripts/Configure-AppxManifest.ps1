# Copyright (c) 2024 Files Community
# Licensed under the MIT License. See the LICENSE.

param(
    [string]$Branch = "", # This has to correspond with one of the AppEnvironment enum values
    [string]$PackageManifestPath = "",
    [string]$Publisher = "",
    [string]$WorkingDir = "",
    [string]$SecretBingMapsKey = "",
    [string]$SecretSentry = "",
    [string]$SecretGitHubOAuthClientId = ""
)

[xml]$xmlDoc = Get-Content $PackageManifestPath
$xmlDoc.Package.Identity.Publisher = $Publisher

if ($Branch -eq "SideloadPreview")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="FilesPreview"
    $xmlDoc.Package.Properties.DisplayName="Files - Preview"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="Files - Preview"
    $xmlDoc.Package.Applications.Application.VisualElements.DefaultTile.ShortName="Files - Preview"
    $xmlDoc.Save($PackageManifestPath)

    Get-ChildItem $WorkingDir -Include *.csproj, *.appxmanifest, *.wapproj, *.xaml -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Preview" }) | `
        Set-Content $_ -NoNewline `
    }
}
elseif ($Branch -eq "StorePreview")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="49306atecsolution.FilesPreview"
    $xmlDoc.Package.Properties.DisplayName="Files - Preview"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="Files - Preview"
    $xmlDoc.Package.Applications.Application.VisualElements.DefaultTile.ShortName="49306atecsolution.FilesPreview"

    # Remove an capability that is used for the sideload
    $nsmgr = New-Object System.Xml.XmlNamespaceManager($xmlDoc.NameTable)
    $nsmgr.AddNamespace("pkg", "http://schemas.microsoft.com/appx/manifest/foundation/windows10")
    $nsmgr.AddNamespace("rescap", "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities")
    $pm = $xmlDoc.SelectSingleNode("/pkg:Package/pkg:Capabilities/rescap:Capability[@Name='packageManagement']", $nsmgr)
    $xmlDoc.Package.Capabilities.RemoveChild($pm)
    $xmlDoc.Save($PackageManifestPath)

    Get-ChildItem $WorkingDir -Include *.csproj, *.appxmanifest, *.wapproj, *.xaml -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Preview" }) | `
        Set-Content $_ -NoNewline `
    }
}
elseif ($Branch -eq "SideloadStable")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="Files"
    $xmlDoc.Package.Properties.DisplayName="Files"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="Files"
    $xmlDoc.Package.Applications.Application.VisualElements.DefaultTile.ShortName="Files"
    $xmlDoc.Save($PackageManifestPath)

    Get-ChildItem $WorkingDir -Include *.csproj, *.appxmanifest, *.wapproj, *.xaml -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Release" }) | `
        Set-Content $_ -NoNewline `
    }
}
elseif ($Branch -eq "StoreStable")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="49306atecsolution.FilesUWP"
    $xmlDoc.Package.Properties.DisplayName="Files App"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="Files"
    $xmlDoc.Package.Applications.Application.VisualElements.DefaultTile.ShortName="Files"

    # Remove an capability that is used for the sideload
    $nsmgr = New-Object System.Xml.XmlNamespaceManager($xmlDoc.NameTable)
    $nsmgr.AddNamespace("pkg", "http://schemas.microsoft.com/appx/manifest/foundation/windows10")
    $nsmgr.AddNamespace("rescap", "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities")
    $pm = $xmlDoc.SelectSingleNode("/pkg:Package/pkg:Capabilities/rescap:Capability[@Name='packageManagement']", $nsmgr)
    $xmlDoc.Package.Capabilities.RemoveChild($pm)
    $xmlDoc.Save($PackageManifestPath)

    Get-ChildItem $WorkingDir -Include *.csproj, *.appxmanifest, *.wapproj, *.xaml -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Release" }) | `
        Set-Content $_ -NoNewline `
    }
}

Get-ChildItem $WorkingDir -Include *.cs -recurse | ForEach-Object -Process `
{ `
    (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "cd_app_env_placeholder", $Branch }) | `
    Set-Content $_ -NoNewline `
}

Get-ChildItem $WorkingDir -Include *.cs -recurse | ForEach-Object -Process `
{ `
    (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "bingmapskey.secret", "$SecretBingMapsKey" }) | `
    Set-Content $_ -NoNewline `
}

Get-ChildItem $WorkingDir -Include *.cs -recurse | ForEach-Object -Process `
{
    (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "sentry.secret", "$SecretSentry" }) | `
    Set-Content $_ -NoNewline `
}

Get-ChildItem $WorkingDir -Include *.cs -recurse | ForEach-Object -Process `
{ `
    (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "githubclientid.secret", "$SecretGitHubOAuthClientId" }) | `
    Set-Content $_ -NoNewline `
}
