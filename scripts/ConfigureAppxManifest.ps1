<#
    .SYNOPSIS 
    A

    .DESCRIPTION
    B

    .PARAMETER Configuration
    Runs against main vs. current branch

    .EXAMPLE
    PS> .\C.ps1
#>
param(
    [string]$Configuration = "Store"
)

$IconVariant = switch ($Configuration)
{
    "Debug" { "Dev" }
    "Release" { "Dev" }
    "Preview" { "Preview" }
    "Stable" { "Release" }
    "Store" { "Release" }
}
$WorkingDir = "C:\Users\onein\source\repos\Files"
$SearchPath = "Assets\\AppTiles\\Dev"
$ReplacePath = "Assets\AppTiles\" + $IconVariant

Get-ChildItem $WorkingDir\src -Include *.csproj, *.appxmanifest, *.wapproj, *.xaml -recurse | ForEach-Object -Process `
{ `
    (Get-Content $_ -Raw | ForEach-Object -Process {$_ -replace $SearchPath, $ReplacePath}) | `
    Set-Content $_ -NoNewline `
}
