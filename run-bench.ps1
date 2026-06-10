# run-bench.ps1
# Builds, runs, and compares the search service benchmark in one shot.
# Usage: .\run-bench.ps1 [-Corpus <path>] [-Out <path>] [-SkipBuild] [-NoNaiveScan]
#
# Prerequisites: dotnet SDK, corpus generated at .bench\small (run files-corpora first).
param(
    [string]$Corpus    = ".bench\small",
    [string]$Out       = "bench-results",
    [switch]$SkipBuild,
    [switch]$NoNaiveScan
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

function Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Ok($msg)   { Write-Host "    $msg"   -ForegroundColor Green }
function Warn($msg) { Write-Host "    $msg"   -ForegroundColor Yellow }
function Fail($msg) { Write-Host "FAIL: $msg" -ForegroundColor Red; exit 1 }

# --- 1. Resolve and validate corpus ---
$corpusAbs = if ([System.IO.Path]::IsPathRooted($Corpus)) { $Corpus } else { Join-Path $root $Corpus }
$manifest  = Join-Path $corpusAbs "manifest.json"
if (-not (Test-Path $manifest)) {
    Fail "manifest.json not found at '$corpusAbs'. Run: dotnet run --project tests\corpora -- --preset small --out $Corpus"
}
$m = Get-Content $manifest | ConvertFrom-Json
Ok "corpus: $($m.preset) ($($m.fileCount.ToString('N0')) files, seed=$($m.seed))"

$outAbs = if ([System.IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path $root $Out }
New-Item -ItemType Directory -Force -Path $outAbs | Out-Null

# --- 2. Build ---
if (-not $SkipBuild) {
    Step "Building search service"
    $built = $false
    $tries = 0
    while (-not $built -and $tries -lt 3) {
        $result = & dotnet build "$root\src\Files.SearchService\Files.SearchService.csproj" -c Release 2>&1
        if ($LASTEXITCODE -eq 0) { $built = $true }
        else {
            $tries++
            if ($tries -lt 3) { Start-Sleep -Seconds 5 }
            else { Fail "Service build failed after 3 tries. Kill any running files-search-service.exe and retry, or use -SkipBuild." }
        }
    }
    Ok "service built"

    Step "Building bench"
    & dotnet build "$root\tests\Files.Search.Bench\Files.Search.Bench.csproj" -c Release | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail "Bench build failed." }
    Ok "bench built"
}

# --- 3. Start service ---
Step "Starting search service (root=$corpusAbs)"

# Kill any stray service instance that might be holding the pipe.
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" |
    Where-Object { $_.CommandLine -like "*Files.SearchService*" } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 1

$indexDir = Join-Path $root ".bench\index"
$svcOut   = [System.IO.Path]::GetTempFileName()
$svcErr   = [System.IO.Path]::GetTempFileName()
$svcProj  = "$root\src\Files.SearchService\Files.SearchService.csproj"

$env:FILES_SEARCH_ROOT      = $corpusAbs
$env:FILES_SEARCH_INDEX_DIR = $indexDir

# Start-Process with file redirection avoids the PS 5.1 event-handler incompatibilities.
$svc = Start-Process -FilePath "dotnet" `
    -ArgumentList "run","--project",$svcProj,"-c","Release","--no-build" `
    -RedirectStandardOutput $svcOut `
    -RedirectStandardError  $svcErr `
    -PassThru -NoNewWindow

# Wait up to 3 minutes for the service to finish bootstrap and start listening.
# "Now listening" is emitted only after BootstrapAsync completes, so it means index is ready.
$deadline = (Get-Date).AddMinutes(3)
$ready    = $false
while ((Get-Date) -lt $deadline) {
    $log = Get-Content $svcOut -ErrorAction SilentlyContinue
    if ($log -like "*Now listening*") { $ready = $true; break }
    if ($svc.HasExited) { Fail "Service exited prematurely. See: $svcOut" }
    Start-Sleep -Milliseconds 500
}
if (-not $ready) { $svc.Kill(); Fail "Service did not start within 3 minutes." }
Ok "service ready (PID $($svc.Id)) -- bootstrap complete"

try {
    $runs = @{}

    # --- 4. naive-scan baseline ---
    if (-not $NoNaiveScan) {
        Step "Running naive-scan (baseline)"
        & dotnet run --project "$root\tests\Files.Search.Bench\Files.Search.Bench.csproj" `
            -c Release --no-build -- `
            --corpus $corpusAbs --provider naive-scan --out $outAbs
        if ($LASTEXITCODE -ne 0) { Fail "naive-scan bench failed." }

        $scanFile = Get-ChildItem $outAbs -Filter "*.json" |
                    Where-Object { $_.Name -ne "baseline.json" } |
                    Sort-Object LastWriteTime -Descending | Select-Object -First 1
        $runs["naive-scan"] = Get-Content $scanFile.FullName | ConvertFrom-Json
        Ok "naive-scan done -> $($scanFile.Name)"
    }

    # --- 5. indexed ---
    Step "Running indexed"
    & dotnet run --project "$root\tests\Files.Search.Bench\Files.Search.Bench.csproj" `
        -c Release --no-build -- `
        --corpus $corpusAbs --provider indexed --out $outAbs
    if ($LASTEXITCODE -ne 0) { Fail "indexed bench failed." }

    $idxFile = Get-ChildItem $outAbs -Filter "*.json" |
               Where-Object { $_.Name -ne "baseline.json" } |
               Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $runs["indexed"] = Get-Content $idxFile.FullName | ConvertFrom-Json
    Ok "indexed done -> $($idxFile.Name)"

} finally {
    # --- 6. Stop service ---
    if (-not $svc.HasExited) {
        $svc.Kill()
        $svc.WaitForExit(5000) | Out-Null
    }
    $env:FILES_SEARCH_ROOT      = $null
    $env:FILES_SEARCH_INDEX_DIR = $null
}

# --- 7. Print comparison table ---
Write-Host ""
$fileCountStr = $m.fileCount.ToString('N0')
$header = "=== Results: {0} corpus, {1} files ===" -f $m.preset, $fileCountStr
Write-Host $header -ForegroundColor White

$metrics = @(
    @{ Key="ttfrMedianMs";  Label="TTFR median" },
    @{ Key="ttfrP95Ms";     Label="TTFR p95" },
    @{ Key="ttfrP99Ms";     Label="TTFR p99" },
    @{ Key="totalMedianMs"; Label="Total median" },
    @{ Key="totalP95Ms";    Label="Total p95" },
    @{ Key="totalP99Ms";    Label="Total p99" }
)

$fmt = "{0,-20} {1,14} {2,14} {3,10}"
Write-Host ($fmt -f "metric", "naive-scan", "indexed", "ratio")
Write-Host ("-" * 60)
foreach ($m2 in $metrics) {
    $iv = $runs["indexed"].aggregates.($m2.Key)
    if ($runs.ContainsKey("naive-scan")) {
        $sv    = $runs["naive-scan"].aggregates.($m2.Key)
        $ratio = if ($sv -gt 0) { "{0:F2}x" -f ($iv / $sv) } else { "n/a" }
        Write-Host ($fmt -f $m2.Label, "${sv}ms", "${iv}ms", $ratio)
    } else {
        Write-Host ($fmt -f $m2.Label, "skipped", "${iv}ms", "-")
    }
}

# --- 8. Gate check vs baseline.json ---
$baselinePath = Join-Path $outAbs "baseline.json"
if (Test-Path $baselinePath) {
    Write-Host ""
    Write-Host "=== Gate check vs baseline ===" -ForegroundColor White
    $bl  = (Get-Content $baselinePath | ConvertFrom-Json).pinned.aggregates
    $ia  = $runs["indexed"].aggregates
    $pass = $true

    $gates = @(
        @{ Label="TTFR median"; Got=$ia.ttfrMedianMs; Pinned=$bl.ttfrMedianMs; ThresholdPct=150 },
        @{ Label="TTFR p99";    Got=$ia.ttfrP99Ms;    Pinned=$bl.ttfrP99Ms;    ThresholdPct=200 },
        @{ Label="Total p99";   Got=$ia.totalP99Ms;   Pinned=$bl.totalP99Ms;   ThresholdPct=150 }
    )
    foreach ($g in $gates) {
        $pct    = if ($g.Pinned -gt 0) { [int]($g.Got / $g.Pinned * 100) } else { 100 }
        $ok     = $pct -le $g.ThresholdPct
        $symbol = if ($ok) { "PASS" } else { "FAIL" }
        $color  = if ($ok) { "Green" } else { "Red" }
        $pctStr = "$pct" + "%"
        Write-Host ("  {0,-14} {1,6}ms vs pinned {2,6}ms ({3})  [{4}]" -f `
            $g.Label, $g.Got, $g.Pinned, $pctStr, $symbol) -ForegroundColor $color
        if (-not $ok) { $pass = $false }
    }

    if ($pass) {
        Write-Host "`n  All gates PASS" -ForegroundColor Green
    } else {
        Write-Host "`n  One or more gates FAILED -- update baseline.json if this is intentional" -ForegroundColor Red
        exit 1
    }
} else {
    Warn "No baseline.json found at '$baselinePath' -- skipping gate check"
    Warn "Run once to establish baseline, then copy the indexed result to baseline.json"
}
