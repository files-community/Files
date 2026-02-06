# PowerShell script to extract all resource string keys used in settings pages and output as JSON mapping page to keys
# Place this script in src/Files.App/Scripts and ensure it is referenced in the build process

param(
    [string]$SettingsPagesPath = (Join-Path $PSScriptRoot "..\Views\Settings"),
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\Assets\Data\settings_string_keys.json")
)

$allKeys = @{}


Get-ChildItem -Path $SettingsPagesPath -Filter *.xaml -Recurse | ForEach-Object {
    $page = $_.BaseName
    $content = Get-Content $_.FullName -Raw
    # capture the full start tag so we can inspect attributes that may come after Header
    $settingsCardPattern = '<(?:wctcontrols:)?SettingsCard[^>]*?Header="\{helpers:ResourceString\s+Name=([a-zA-Z0-9_]+)\}"[^>]*>'
    $settingsExpanderPattern = '<(?:wctcontrols:)?SettingsExpander[^>]*?Header="\{helpers:ResourceString\s+Name=([a-zA-Z0-9_]+)\}"[^>]*>'
    $textBlockPattern = '<(?:wctcontrols:)?TextBlock[^>]*?(FontSize\s*=\s*"(24|16)")[^>]*?\{helpers:ResourceString\s+Name=([a-zA-Z0-9_]+)\}'
    # pattern to detect dev-only bindings (x:Load or Visibility bound to ViewModel.IsAppEnvironmentDev)
    $devBindingPattern = '(?i)(x:Load\s*=\s*"\{x:Bind\s+ViewModel\.IsAppEnvironmentDev\b)|(Visibility\s*=\s*"\{x:Bind\s+ViewModel\.IsAppEnvironmentDev\b)'
    $settingsCardMatches = [regex]::Matches($content, $settingsCardPattern)
    $settingsExpanderMatches = [regex]::Matches($content, $settingsExpanderPattern)
    $textBlockMatches = [regex]::Matches($content, $textBlockPattern)
    $keys = @()
    foreach ($match in $settingsCardMatches) {
        $key = $match.Groups[1].Value
        $tagText = $match.Value
        # Skip dev-only elements where x:Load or Visibility is bound to ViewModel.IsAppEnvironmentDev
        if ($tagText -match $devBindingPattern) {
            continue
        }
        if ($key -and ($keys -notcontains $key)) {
            $keys += $key
        }
    }
    foreach ($match in $settingsExpanderMatches) {
        $key = $match.Groups[1].Value
        $tagText = $match.Value
        # Skip dev-only elements where x:Load or Visibility is bound to ViewModel.IsAppEnvironmentDev
        if ($tagText -match $devBindingPattern) {
            continue
        }
        if ($key -and ($keys -notcontains $key)) {
            $keys += $key
        }
    }
    foreach ($match in $textBlockMatches) {
        $key = $match.Groups[3].Value
        $tagText = $match.Value
        # Skip dev-only header TextBlocks where x:Load or Visibility is bound to ViewModel.IsAppEnvironmentDev
        if ($tagText -match $devBindingPattern) {
            continue
        }
        if ($key -and ($keys -notcontains $key)) {
            $keys += $key
        }
    }
    if ($keys.Count -gt 0) {
        $allKeys[$page] = $keys
    }
}

# Output as JSON object
$allKeys | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 $OutputPath
