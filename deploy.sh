#!/bin/bash
# Build, copy DLLs, and create two zips for deployment:
#   - MatchZy.zip  (plugin DLLs + lang + spawns)
#   - cfg.zip      (cfg files)

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
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

# --- Create zips ---
cd "$SCRIPT_DIR/bin"
rm -f MatchZy.zip cfg.zip
powershell.exe -Command "Compress-Archive -Path '$(wslpath -w "$DEPLOY_DIR")' -DestinationPath '$(wslpath -w "$SCRIPT_DIR/bin/MatchZy.zip")' -Force"
powershell.exe -Command "Compress-Archive -Path '$(wslpath -w "$CFG_DIR/cfg")' -DestinationPath '$(wslpath -w "$SCRIPT_DIR/bin/cfg.zip")' -Force"

echo ""
echo "Done!"
echo "  bin/MatchZy.zip  - plugin files"
echo "  bin/cfg.zip      - config files"
echo ""
echo "To deploy on DatHost:"
echo "  1. Upload MatchZy.zip -> extract to: csgo/addons/counterstrikesharp/plugins/MatchZy/"
echo "  2. Upload cfg.zip     -> extract to: csgo/ (contains cfg/MatchZy/)"
echo "  3. Restart the server"
