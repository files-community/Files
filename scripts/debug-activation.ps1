<#
.SYNOPSIS
    Diagnose why packaged Files.exe activation isn't producing a process.

.DESCRIPTION
    Bundles every diagnostic we found useful while chasing the Win10 19045
    packaged-launch failure (see docs/packaged-build-debug-notes.md):

      1. Kernel-process ETW trace — every CreateProcess in the window.
         Definitive answer to "did Files.exe spawn at all?"
      2. AppXDeploymentServer log — register-loop / state-repo signals
         (7028, 856/857, 603, 9621, 9626).
      3. AppModel-Runtime/Admin log — container lifecycle (210, 211, 212, 217).
      4. TWinUI/Operational log — activation success/failure (1621, 5961).
      5. .NET Runtime + Application Error log for any post-spawn crashes.
      6. WER LocalDumps state.

    All output is timestamped and saved to a single output directory you can
    grep or compare across runs.

    Run from an ELEVATED PowerShell. The kernel ETW session requires admin.

.PARAMETER Aumid
    AppUserModelId to activate. Default: FilesDev_j4wp4nz5mtqsg!App.

.PARAMETER Seconds
    How long to watch after activation. Default: 10.

.PARAMETER OutputDir
    Where to write the trace + summary. Default: %TEMP%\files-activation-<timestamp>.

.EXAMPLE
    .\scripts\debug-activation.ps1
    .\scripts\debug-activation.ps1 -Seconds 20
    .\scripts\debug-activation.ps1 -Aumid 'Files_j4wp4nz5mtqsg!App'
#>
[CmdletBinding()]
param(
    [string]$Aumid     = 'FilesDev_j4wp4nz5mtqsg!App',
    [int]$Seconds      = 10,
    [string]$OutputDir = (Join-Path $env:TEMP ("files-activation-" + (Get-Date -Format 'yyyyMMdd-HHmmss')))
)

$ErrorActionPreference = 'Stop'

# --- Elevation check ---
$id = [System.Security.Principal.WindowsIdentity]::GetCurrent()
if (-not (New-Object System.Security.Principal.WindowsPrincipal($id)).IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Must run elevated. logman -ets requires admin.'
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Save-Section($name, $content) {
    $path = Join-Path $OutputDir "$name.txt"
    $content | Out-File -FilePath $path -Encoding UTF8
    Write-Host "    -> $path"
}

$etl = Join-Path $OutputDir 'kproc.etl'
$xml = Join-Path $OutputDir 'kproc.xml'
$startTime = Get-Date

# --- Baseline event record IDs so we only collect new events ---
function Get-LastRecord($logName) {
    try {
        (Get-WinEvent -LogName $logName -MaxEvents 1 -ErrorAction Stop).RecordId
    } catch { 0 }
}
$baselines = @{
    'Microsoft-Windows-AppXDeploymentServer/Operational' = Get-LastRecord 'Microsoft-Windows-AppXDeploymentServer/Operational'
    'Microsoft-Windows-AppModel-Runtime/Admin'           = Get-LastRecord 'Microsoft-Windows-AppModel-Runtime/Admin'
    'Microsoft-Windows-TWinUI/Operational'               = Get-LastRecord 'Microsoft-Windows-TWinUI/Operational'
    'Application'                                        = Get-LastRecord 'Application'
}

# --- Kernel-process ETW trace ---
Write-Step 'Starting kernel-process ETW trace'
$null = logman stop kproc-debug -ets 2>&1
$null = logman start kproc-debug -p 'Microsoft-Windows-Kernel-Process' 0x10 5 -ets -o $etl

# --- Activate ---
Write-Step "Activating $Aumid"
Start-Process explorer.exe "shell:appsFolder\$Aumid"

# --- Watch for Files.exe ---
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$firstSeen = $null
$lastSeen  = $null
while ($sw.Elapsed.TotalSeconds -lt $Seconds) {
    $p = Get-Process Files -ErrorAction SilentlyContinue
    if ($p) {
        if (-not $firstSeen) { $firstSeen = $sw.Elapsed.TotalMilliseconds }
        $lastSeen = $sw.Elapsed.TotalMilliseconds
    }
    Start-Sleep -Milliseconds 50
}

Write-Step 'Stopping trace'
$null = logman stop kproc-debug -ets

# --- Convert kernel trace to XML ---
Write-Step 'Converting trace'
$null = tracerpt $etl -o $xml -of XML -y 2>&1

# --- Parse kernel-process events ---
Write-Step 'Parsing kernel events'
[xml]$traceXml = Get-Content $xml
$ns = New-Object System.Xml.XmlNamespaceManager($traceXml.NameTable)
$ns.AddNamespace('e','http://schemas.microsoft.com/win/2004/08/events/event')
$creates = $traceXml.SelectNodes('//e:Event[e:System/e:EventID=1]', $ns)
$exits   = $traceXml.SelectNodes('//e:Event[e:System/e:EventID=2]', $ns)

$pkgFamily = ($Aumid -split '!')[0]
$kprocReport = [System.Collections.Generic.List[string]]::new()
$kprocReport.Add("Total process creates in trace: $($creates.Count)")
$kprocReport.Add("Total process exits in trace:   $($exits.Count)")
$kprocReport.Add('')
$kprocReport.Add("--- Creates matching FilesDev / Files.exe / files-search / RuntimeBroker ---")
$matched = $false
foreach ($n in $creates) {
    $d = @{}
    if ($n.EventData -and $n.EventData.Data) { foreach ($x_ in $n.EventData.Data) { $d[$x_.Name] = $x_."#text" } }
    $img = $d['ImageName']
    if ($img -and ($img -match "$pkgFamily|\\Files\.exe|files-search|RuntimeBroker")) {
        $kprocReport.Add(("  PID {0,6} -> {1} (parent {2})" -f $d['ProcessID'], $img, $d['ParentProcessID']))
        $matched = $true
    }
}
if (-not $matched) { $kprocReport.Add('  (no matching creates)') }
Save-Section 'kernel-process' ($kprocReport -join "`n")

# --- Pull new events from each log since baseline ---
function Get-NewEvents($logName, $idsOfInterest) {
    $base = $baselines[$logName]
    try {
        Get-WinEvent -LogName $logName -MaxEvents 200 -ErrorAction Stop |
            Where-Object { $_.RecordId -gt $base } |
            Sort-Object TimeCreated |
            Where-Object { ($null -eq $idsOfInterest) -or ($_.Id -in $idsOfInterest) }
    } catch { @() }
}

# AppXDeploymentServer — focus on register-loop + manifest-parse + service errors
$deployEvents = Get-NewEvents 'Microsoft-Windows-AppXDeploymentServer/Operational' @(400, 401, 404, 603, 604, 607, 613, 649, 856, 857, 7028, 9621, 9626, 9627, 9644, 9647, 9650, 10000, 10001) |
    Where-Object { $_.Message -match $pkgFamily.Split('_')[0] }
$deployReport = if ($deployEvents) {
    ($deployEvents | ForEach-Object {
        "{0:HH:mm:ss.fff} {1,-5} {2,-7} {3}" -f $_.TimeCreated, $_.Id, $_.LevelDisplayName, (($_.Message -split '\r?\n')[0])
    }) -join "`n"
} else { '(no new events)' }
Save-Section 'appx-deployment' $deployReport

# AppModel-Runtime/Admin — container lifecycle
$appmodelEvents = Get-NewEvents 'Microsoft-Windows-AppModel-Runtime/Admin' @(210, 211, 212, 217) |
    Where-Object { $_.Message -match $pkgFamily.Split('_')[0] }
$appmodelReport = if ($appmodelEvents) {
    ($appmodelEvents | ForEach-Object {
        "{0:HH:mm:ss.fff} {1,-5} {2}" -f $_.TimeCreated, $_.Id, (($_.Message -split '\r?\n')[0])
    }) -join "`n"
} else { '(no new events)' }
Save-Section 'appmodel-runtime' $appmodelReport

# TWinUI — activation attempts
$twinuiEvents = Get-NewEvents 'Microsoft-Windows-TWinUI/Operational' $null |
    Where-Object { $_.Message -match $Aumid -or $_.Message -match $pkgFamily.Split('_')[0] }
$twinuiReport = if ($twinuiEvents) {
    ($twinuiEvents | ForEach-Object {
        "{0:HH:mm:ss.fff} {1,-5} {2,-7} {3}" -f $_.TimeCreated, $_.Id, $_.LevelDisplayName, (($_.Message -split '\r?\n')[0])
    }) -join "`n"
} else { '(no new events)' }
Save-Section 'twinui' $twinuiReport

# Application log — .NET Runtime + Application Error
$appLogEvents = Get-NewEvents 'Application' @(1000, 1026) |
    Where-Object { $_.Message -match 'Files\.exe|files-search' }
$appLogReport = if ($appLogEvents) {
    ($appLogEvents | ForEach-Object {
        "{0:HH:mm:ss.fff} {1,-5} {2,-7} {3}`n----`n{4}`n----" -f $_.TimeCreated, $_.Id, $_.LevelDisplayName, $_.ProviderName, $_.Message
    }) -join "`n`n"
} else { '(no Files-related errors in Application log)' }
Save-Section 'application-log' $appLogReport

# WER LocalDumps state
$werReport = @()
$key = 'HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\Files.exe'
$werCfg = Get-ItemProperty $key -ErrorAction SilentlyContinue
if ($werCfg) {
    $werReport += "LocalDumps configured for Files.exe:"
    $werReport += "  DumpFolder: $($werCfg.DumpFolder)"
    $werReport += "  DumpType:   $($werCfg.DumpType)"
    $werReport += "  DumpCount:  $($werCfg.DumpCount)"
} else {
    $werReport += "LocalDumps NOT configured for Files.exe (HKLM\...\WER\LocalDumps\Files.exe missing)"
}
$werReport += ''
$werReport += '--- Recent dumps in C:\CrashDumps ---'
$dumps = Get-ChildItem C:\CrashDumps -Filter 'Files*.dmp' -ErrorAction SilentlyContinue |
    Where-Object { $_.LastWriteTime -gt $startTime.AddMinutes(-1) }
if ($dumps) {
    foreach ($d in $dumps) { $werReport += ("  {0}  {1:N0} bytes  {2}" -f $d.Name, $d.Length, $d.LastWriteTime) }
} else {
    $werReport += '  (no new dumps since trace start)'
}
Save-Section 'wer-state' ($werReport -join "`n")

# --- Build summary ---
$summary = [System.Collections.Generic.List[string]]::new()
$summary.Add("# Activation diagnostic summary")
$summary.Add("AUMID:        $Aumid")
$summary.Add("Watched:      $Seconds seconds")
$summary.Add("Output dir:   $OutputDir")
$summary.Add('')
$summary.Add("## Files.exe process state")
if ($firstSeen) {
    $summary.Add("  SPAWNED at T+$([int]$firstSeen)ms, last seen at T+$([int]$lastSeen)ms")
    $stillAlive = Get-Process Files -ErrorAction SilentlyContinue
    $summary.Add("  Currently alive: $($null -ne $stillAlive)")
} else {
    $summary.Add("  NEVER spawned in $Seconds seconds")
}
$summary.Add('')
$summary.Add("## Files-related processes seen in kernel trace")
if ($matched) {
    foreach ($n in $creates) {
        $d = @{}
        if ($n.EventData -and $n.EventData.Data) { foreach ($x_ in $n.EventData.Data) { $d[$x_.Name] = $x_."#text" } }
        $img = $d['ImageName']
        if ($img -and ($img -match "$pkgFamily|\\Files\.exe|files-search|RuntimeBroker")) {
            $summary.Add("  PID $($d['ProcessID']) -> $img")
        }
    }
} else {
    $summary.Add("  (none)")
}
$summary.Add('')
$summary.Add("## Event counts")
$summary.Add("  AppXDeploymentServer (filtered): $($deployEvents.Count)")
$summary.Add("  AppModel-Runtime/Admin:          $($appmodelEvents.Count)")
$summary.Add("  TWinUI/Operational:              $($twinuiEvents.Count)")
$summary.Add("  Application (.NET / WER):        $($appLogEvents.Count)")
$summary.Add('')
$summary.Add("Detailed per-source reports written to:")
$summary.Add("  $OutputDir\kernel-process.txt")
$summary.Add("  $OutputDir\appx-deployment.txt")
$summary.Add("  $OutputDir\appmodel-runtime.txt")
$summary.Add("  $OutputDir\twinui.txt")
$summary.Add("  $OutputDir\application-log.txt")
$summary.Add("  $OutputDir\wer-state.txt")

$summaryText = $summary -join "`n"
Save-Section 'summary' $summaryText
Write-Host ''
Write-Host $summaryText
