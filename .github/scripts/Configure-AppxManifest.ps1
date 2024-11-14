# Copyright (c) 2024 Files Community
# Licensed under the MIT License. See the LICENSE.

param(
    [string]$Branch = "",
    [string]$PackageManifestPath = "",
    [string]$Publisher = "",
    [string]$WorkingDir = "",
    [string]$SecretBingMapsKey = "",
    [string]$SecretSentry = "",
    [string]$SecretGitHubOAuthClientId = ""
)

# Load Package.appxmanifest
[xml]$xmlDoc = Get-Content $PackageManifestPath

# Add namespaces
$nsmgr = New-Object System.Xml.XmlNamespaceManager($xmlDoc.NameTable)
$nsmgr.AddNamespace("pkg", "http://schemas.microsoft.com/appx/manifest/foundation/windows10")
$nsmgr.AddNamespace("rescap", "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities")
$nsmgr.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10")
$nsmgr.AddNamespace("uap5", "http://schemas.microsoft.com/appx/manifest/uap/windows10/5")
$ap = $xmlDoc.SelectSingleNode("/pkg:Package/pkg:Applications/pkg:Application/pkg:Extensions/uap:Extension[@Category='windows.protocol']/uap:Protocol", $nsmgr)
$aea = $xmlDoc.SelectSingleNode("/pkg:Package/pkg:Applications/pkg:Application/pkg:Extensions/uap5:Extension[@Category='windows.appExecutionAlias']/uap5:AppExecutionAlias", $nsmgr)
$ea = $xmlDoc.SelectSingleNode("/pkg:Package/pkg:Applications/pkg:Application/pkg:Extensions/uap5:Extension[@Category='windows.appExecutionAlias']/uap5:AppExecutionAlias/uap5:ExecutionAlias", $nsmgr)

# Update the publisher
$xmlDoc.Package.Identity.Publisher = $Publisher

if ($Branch -eq "Preview")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="FilesPreview"
    $xmlDoc.Package.Properties.DisplayName="Files - Preview"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="Files - Preview"
    $xmlDoc.Package.Applications.Application.VisualElements.DefaultTile.ShortName="Files - Preview"

    # Update app protocol and execution alias
    $ap.SetAttribute("Name", "files-pre");
    $ea.SetAttribute("Alias", "files-pre.exe");

    # Save modified Package.appxmanifest
    $xmlDoc.Save($PackageManifestPath)

    Get-ChildItem $WorkingDir -Include *.csproj, *.appxmanifest, *.wapproj, *.xaml -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Preview" }) | `
        Set-Content $_ -NoNewline `
    }

    Get-ChildItem $WorkingDir -Include *.cs, *.cpp -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "files-dev", "files-pre" }) | `
        Set-Content $_ -NoNewline `
    }
}
elseif ($Branch -eq "Stable")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="Files"
    $xmlDoc.Package.Properties.DisplayName="Files"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="Files"
    $xmlDoc.Package.Applications.Application.VisualElements.DefaultTile.ShortName="Files"

    # Update app protocol and execution alias
    $ap.SetAttribute("Name", "files");
    $aea.RemoveChild(aea.FirstChild); # Avoid duplication
    
    # Save modified Package.appxmanifest
    $xmlDoc.Save($PackageManifestPath)

    Get-ChildItem $WorkingDir -Include *.csproj, *.appxmanifest, *.wapproj, *.xaml -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Release" }) | `
        Set-Content $_ -NoNewline `
    }

    Get-ChildItem $WorkingDir -Include *.cs, *.cpp -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "files-dev", "files" }) | `
        Set-Content $_ -NoNewline `
    }
}
elseif ($Branch -eq "Store")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="49306atecsolution.FilesUWP"
    $xmlDoc.Package.Properties.DisplayName="Files App"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="Files"
    $xmlDoc.Package.Applications.Application.VisualElements.DefaultTile.ShortName="Files"

    # Remove capability that is only used for the sideload package
    $pm = $xmlDoc.SelectSingleNode("/pkg:Package/pkg:Capabilities/rescap:Capability[@Name='packageManagement']", $nsmgr)
    $xmlDoc.Package.Capabilities.RemoveChild($pm)

    # Update app protocol and execution alias
    $ap.SetAttribute("Name", "files");
    $aea.RemoveChild(aea.FirstChild); # Avoid duplication

    # Save modified Package.appxmanifest
    $xmlDoc.Save($PackageManifestPath)

    Get-ChildItem $WorkingDir -Include *.csproj, *.appxmanifest, *.wapproj, *.xaml -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Release" }) | `
        Set-Content $_ -NoNewline `
    }

    Get-ChildItem $WorkingDir -Include *.cs, *.cpp -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "files-dev", "files" }) | `
        Set-Content $_ -NoNewline `
    }
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
