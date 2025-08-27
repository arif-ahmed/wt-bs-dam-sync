#!/usr/bin/env bash
# publish.sh — Windows-friendly (Git Bash) build & rename

set -euo pipefail

# Stop MSYS/Git Bash from mangling args
export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL='*'

usage() {
  cat <<'USAGE'
Usage:
  ./scripts/publish.sh [options]

Options:
  -r, --runtime <list>     Comma-separated RIDs (win-x64,linux-x64,osx-x64,osx-arm64)
  -c, --configuration <c>  Debug | Release (default: Release)
      --clean              Remove ./publish before building
  -h, --help               Show help
USAGE
}

# Defaults
RUNTIMES="win-x64"
CONFIG="Release"
CLEAN=false

# Projects (relative to repo root)
WORKER_PROJ="src/BrandshareDamSync.Daemon/BrandshareDamSync.Daemon.csproj"
CLI_PROJ="src/BrandshareDamSync.Cli/BrandshareDamSync.Cli.csproj"

# Original output names (no extension)
WORKER_FROM_NAME="BrandshareDamSync.Daemon"
CLI_FROM_NAME="BrandshareDamSync.Cli"

# Friendly names (no extension)
WORKER_FRIENDLY="BrandshareDamSyncd"
CLI_FRIENDLY="bs-dam-sync"

# Parse args
while [[ $# -gt 0 ]]; do
  case "$1" in
    -r|--runtime) RUNTIMES="${2:-}"; shift 2 ;;
    -c|--configuration) CONFIG="${2:-}"; shift 2 ;;
    --clean) CLEAN=true; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage; exit 2 ;;
  esac
done

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PUB_ROOT="$REPO_ROOT/publish"

# Helper: convert to Windows path if on Git Bash (MINGW*)
to_win_path() {
  if uname | grep -qiE 'mingw|msys'; then
    cygpath -w "$1"
  else
    printf '%s' "$1"
  fi
}

command -v dotnet >/dev/null 2>&1 || { echo "dotnet SDK is required on PATH." >&2; exit 1; }
[[ -f "$REPO_ROOT/$WORKER_PROJ" ]] || { echo "Missing project: $REPO_ROOT/$WORKER_PROJ" >&2; exit 1; }
[[ -f "$REPO_ROOT/$CLI_PROJ"    ]] || { echo "Missing project: $REPO_ROOT/$CLI_PROJ"    >&2; exit 1; }

$CLEAN && rm -rf "$PUB_ROOT"
mkdir -p "$PUB_ROOT"

IFS=',' read -r -a RID_ARR <<< "$RUNTIMES"

echo "Configuration: $CONFIG"
echo "Runtimes     : ${RID_ARR[*]}"
echo "Output       : $PUB_ROOT"
echo

# Work from repo root so project paths are relative (avoids /d/... absolute)
cd "$REPO_ROOT"

for rid in "${RID_ARR[@]}"; do
  os="${rid%%-*}"           # win | linux | osx
  out_os="$PUB_ROOT/$os"
  out_worker="$out_os/worker/$rid"
  out_cli="$out_os/cli/$rid"
  mkdir -p "$out_worker" "$out_cli"

  echo "=== Building for $rid ==="

  # Convert output dirs to Windows paths on Git Bash to keep MSBuild happy
  OUT_WORKER_WIN="$(to_win_path "$out_worker")"
  OUT_CLI_WIN="$(to_win_path "$out_cli")"

  dotnet publish "$WORKER_PROJ" \
    -c "$CONFIG" -r "$rid" \
    -o "$OUT_WORKER_WIN" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:PublishTrimmed=false

  dotnet publish "$CLI_PROJ" \
    -c "$CONFIG" -r "$rid" \
    -o "$OUT_CLI_WIN" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:PublishTrimmed=false

  if [[ "$os" == "win" ]]; then
    worker_from="$out_worker/${WORKER_FROM_NAME}.exe"
    worker_to="$out_worker/${WORKER_FRIENDLY}.exe"
    cli_from="$out_cli/${CLI_FROM_NAME}.exe"
    cli_to="$out_cli/${CLI_FRIENDLY}.exe"
    sep="\\"
  else
    worker_from="$out_worker/${WORKER_FROM_NAME}"
    worker_to="$out_worker/${WORKER_FRIENDLY}"
    cli_from="$out_cli/${CLI_FROM_NAME}"
    cli_to="$out_cli/${CLI_FRIENDLY}"
    sep="/"
  fi

  if [[ -f "$worker_from" ]]; then
    printf 'rename publish%s%s%sworker%s%s%s%s %s\n' "$sep" "$os" "$sep" "$sep" "$rid" "$sep" "$(basename "$worker_from")" "$(basename "$worker_to")"
    mv -f -- "$worker_from" "$worker_to"
  fi

  if [[ -f "$cli_from" ]]; then
    printf 'rename publish%s%s%scli%s%s%s%s %s\n' "$sep" "$os" "$sep" "$sep" "$rid" "$sep" "$(basename "$cli_from")" "$(basename "$cli_to")"
    mv -f -- "$cli_from" "$cli_to"
  fi

  if [[ "$os" != "win" ]]; then
    chmod +x "$worker_to" "$cli_to" || true
  fi

  echo "Output:"
  echo "  $worker_to"
  echo "  $cli_to"
  echo
done

echo "Done."
