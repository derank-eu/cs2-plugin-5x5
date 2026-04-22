#!/bin/bash
# Build, copy DLLs, and create two versioned zips for deployment:
#   - MatchZy-<version>.zip  (plugin DLLs + lang + spawns)
#   - cfg-<version>.zip      (cfg files)
#
# Usage: ./deploy.sh            → bumps Derank fork version
#        ./deploy.sh --no-bump

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# ─── version helpers (self-contained) ───────────────────────────────────────────
read_version() {
    grep -oP "$2" "$1" | head -n1 | grep -oP '"\K[^"]+'
}
bump_patch_version() {
    local file="$1" regex="$2" current new
    current=$(read_version "$file" "$regex")
    [ -z "$current" ] && { echo "ERROR: could not parse version from $file" >&2; return 1; }
    if [ "${NO_BUMP:-0}" = "1" ]; then echo "$current"; return 0; fi
    if [[ "$current" =~ ^([0-9]+(\.[0-9]+)*)\.([0-9]+)$ ]]; then
        new="${BASH_REMATCH[1]}.$((BASH_REMATCH[3] + 1))"
        sed -i "s|\"${current}\"|\"${new}\"|" "$file"
        echo "$new"
    else
        echo "$current"
    fi
}
zip_with_version() {
    local deploy_dir="$1" basename="$2" version="$3"
    local bin_dir; bin_dir=$(dirname "$deploy_dir")
    local versioned="${bin_dir}/${basename}-${version}.zip"
    rm -f "$versioned"
    if command -v powershell.exe &>/dev/null; then
        powershell.exe -Command "Compress-Archive -Path '$(wslpath -w "$deploy_dir")' -DestinationPath '$(wslpath -w "$versioned")' -Force"
    else
        (cd "$bin_dir" && zip -qr "$(basename "$versioned")" "$(basename "$deploy_dir")")
    fi
    echo
    echo "Done: $versioned"
}
# ─── end helpers ────────────────────────────────────────────────────────────────

[ "${1:-}" = "--no-bump" ] && NO_BUMP=1 || NO_BUMP=0
export NO_BUMP

# Target the DerankVersion constant (upstream MatchZy version stays frozen).
VERSION_FILE="$SCRIPT_DIR/MatchZy.cs"
VERSION_REGEX='DerankVersion = "([^"]+)"'

VERSION=$(bump_patch_version "$VERSION_FILE" "$VERSION_REGEX")
echo "Deploying MatchZy (Derank v$VERSION)"

PUBLISH_DIR="$SCRIPT_DIR/bin/publish"
DEPLOY_DIR="$SCRIPT_DIR/bin/MatchZy"
CFG_DIR="$SCRIPT_DIR/bin/cfg"

# Build & publish (copies all dependency DLLs)
echo "Building..."
cd "$SCRIPT_DIR" && dotnet publish -c Release -o "$PUBLISH_DIR"

# --- MatchZy.zip (plugin folder) ---
rm -rf "$DEPLOY_DIR"
mkdir -p "$DEPLOY_DIR"

# Copy plugin DLLs
cp "$PUBLISH_DIR/MatchZy.dll" "$DEPLOY_DIR/"
cp "$PUBLISH_DIR/MatchZy.pdb" "$DEPLOY_DIR/" 2>/dev/null || true
cp "$PUBLISH_DIR/MatchZy.deps.json" "$DEPLOY_DIR/" 2>/dev/null || true

# Copy dependency DLLs
for dll in Dapper Npgsql Newtonsoft.Json CsvHelper MySqlConnector Microsoft.Data.Sqlite \
           SQLitePCLRaw.core SQLitePCLRaw.batteries_v2 SQLitePCLRaw.provider.e_sqlite3; do
  cp "$PUBLISH_DIR/$dll.dll" "$DEPLOY_DIR/" 2>/dev/null || echo "Warning: $dll.dll not found"
done

# Copy lang files
if [ -d "$PUBLISH_DIR/lang" ]; then
  cp -r "$PUBLISH_DIR/lang" "$DEPLOY_DIR/lang"
fi

# Copy spawns files
if [ -d "$PUBLISH_DIR/spawns" ]; then
  cp -r "$PUBLISH_DIR/spawns" "$DEPLOY_DIR/spawns"
fi

echo ""
echo "Plugin folder contents:"
ls -la "$DEPLOY_DIR/"

# --- cfg.zip (config files wrapped in cfg/MatchZy/) ---
rm -rf "$CFG_DIR"
mkdir -p "$CFG_DIR/cfg/MatchZy"
cp -r "$SCRIPT_DIR/cfg/MatchZy/"* "$CFG_DIR/cfg/MatchZy/"

echo ""
echo "Config folder contents:"
ls -la "$CFG_DIR/cfg/MatchZy/"

# --- Create versioned zips ---
zip_with_version "$DEPLOY_DIR" "MatchZy" "$VERSION"
zip_with_version "$CFG_DIR/cfg" "cfg" "$VERSION"

echo ""
echo "To deploy on DatHost:"
echo "  1. Upload MatchZy-$VERSION.zip -> extract to: csgo/addons/counterstrikesharp/plugins/MatchZy/"
echo "  2. Upload cfg-$VERSION.zip     -> extract to: csgo/ (contains cfg/MatchZy/)"
echo "  3. Restart the server"
