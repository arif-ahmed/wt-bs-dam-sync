<#  publish.ps1
    Builds Worker and CLI for multiple RIDs, writes to .\publish\<os>\(worker|cli)\<rid>,
    renames outputs to friendly names, and prints explicit "rename ..." lines.
#>

[CmdletBinding()]
param(
  # Comma-separated RIDs (win-x64,linux-x64,osx-x64,osx-arm64)
  [string]$Runtime = "win-x64,linux-x64,osx-x64,osx-arm64",

  [ValidateSet("Debug","Release")]
  [string]$Configuration = "Release",

  [switch]$Clean
)

$ErrorActionPreference = 'Stop'

# ── Repo root (scripts\..)
$ScriptDir = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
$RepoRoot  = (Resolve-Path (Join-Path $ScriptDir "..")).Path

# ── Config (adjust if your paths/names differ)
# Project files:
$WorkerProj = Join-Path $RepoRoot "src/BrandShareDAMSync.Daemon/BrandShareDAMSync.Daemon.csproj"
$CliProj    = Join-Path $RepoRoot "src/BrandShareDAMSync.Cli/BrandShareDAMSync.Cli.csproj"

# Original binary (assembly) names from the publish output (NO extension here)
$WorkerFromName = "BrandShareDAMSync.Daemon"
$CliFromName    = "BrandShareDAMSync.Cli"

# Friendly target names (NO extension here)
$WorkerFriendly = "BrandShareDAMSyncd"
$CliFriendly    = "bs-dam-sync"

foreach ($p in @($WorkerProj,$CliProj)) {
  if (-not (Test-Path $p)) { throw "Missing project: $p" }
}

# ── Output root
$PubRoot = Join-Path $RepoRoot "publish"
if ($Clean) { if (Test-Path $PubRoot) { Remove-Item -Recurse -Force $PubRoot } }
New-Item -ItemType Directory -Force -Path $PubRoot | Out-Null

$rids = $Runtime -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }

Write-Host "Configuration: $Configuration"
Write-Host "Runtimes     : $($rids -join ', ')"
Write-Host "Output       : $PubRoot`n"

foreach ($rid in $rids) {
  $os = $rid.Split('-')[0]   # win | linux | osx
  $outOs     = Join-Path $PubRoot $os
  $outWorker = Join-Path $outOs  ("worker\" + $rid)
  $outCli    = Join-Path $outOs  ("cli\"    + $rid)

  New-Item -ItemType Directory -Force -Path $outWorker, $outCli | Out-Null

  Write-Host "=== Building for $rid ==="

  dotnet publish $WorkerProj -c $Configuration -r $rid -o $outWorker --self-contained true `
    /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=false

  dotnet publish $CliProj -c $Configuration -r $rid -o $outCli --self-contained true `
    /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=false

  $isWin = $os -eq "win"

  # Compose full paths (with proper extension per OS)
  $workerFrom = Join-Path $outWorker ($WorkerFromName + $(if ($isWin) {'.exe'} else {''}))
  $workerTo   = Join-Path $outWorker ($WorkerFriendly + $(if ($isWin) {'.exe'} else {''}))
  $cliFrom    = Join-Path $outCli    ($CliFromName    + $(if ($isWin) {'.exe'} else {''}))
  $cliTo      = Join-Path $outCli    ($CliFriendly    + $(if ($isWin) {'.exe'} else {''}))

  # Build relative paths for the printed output (use OS-style separator)
  $sep = $(if ($isWin) {'\'} else {'/'})
  $relWorkerFrom = "publish$sep$os$sep" + "worker$sep$rid$sep" + (Split-Path $workerFrom -Leaf)
  $relCliFrom    = "publish$sep$os$sep" + "cli$sep$rid$sep"    + (Split-Path $cliFrom -Leaf)

  if (Test-Path $workerFrom) {
    Write-Host ("rename {0} {1}" -f $relWorkerFrom, (Split-Path $workerTo -Leaf))
    if ($workerFrom -ne $workerTo) {
      Rename-Item -Path $workerFrom -NewName (Split-Path $workerTo -Leaf) -Force
    }
  }

  if (Test-Path $cliFrom) {
    Write-Host ("rename {0} {1}" -f $relCliFrom, (Split-Path $cliTo -Leaf))
    if ($cliFrom -ne $cliTo) {
      Rename-Item -Path $cliFrom -NewName (Split-Path $cliTo -Leaf) -Force
    }
  }

  if (-not $isWin) {
    try { & chmod +x $workerTo $cliTo | Out-Null } catch {}
  }

  Write-Host "Output:"
  Write-Host "  $workerTo"
  Write-Host "  $cliTo`n"
}

Write-Host "`nDone." -ForegroundColor Green
