#!/bin/bash
# Build, copy DLLs, and zip for deployment

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
RELEASE_DIR="$SCRIPT_DIR/bin/Release/net8.0"
DEPLOY_DIR="$SCRIPT_DIR/bin/DerankMix"

# Build
echo "Building..."
cd "$SCRIPT_DIR" && dotnet restore && dotnet build -c Release

# Prepare deploy folder
rm -rf "$DEPLOY_DIR"
mkdir -p "$DEPLOY_DIR"

# Copy main plugin DLL and metadata
cp "$RELEASE_DIR/DerankMix.dll" "$DEPLOY_DIR/"
cp "$RELEASE_DIR/DerankMix.pdb" "$DEPLOY_DIR/" 2>/dev/null || true
cp "$RELEASE_DIR/DerankMix.deps.json" "$DEPLOY_DIR/" 2>/dev/null || true

# Copy dependency DLLs
cp "$RELEASE_DIR/Dapper.dll" "$DEPLOY_DIR/"
cp "$RELEASE_DIR/Npgsql.dll" "$DEPLOY_DIR/"
cp "$RELEASE_DIR/Newtonsoft.Json.dll" "$DEPLOY_DIR/"
cp "$RELEASE_DIR/CsvHelper.dll" "$DEPLOY_DIR/"
cp "$RELEASE_DIR/MySqlConnector.dll" "$DEPLOY_DIR/"
cp "$RELEASE_DIR/Microsoft.Data.Sqlite.dll" "$DEPLOY_DIR/"
cp "$RELEASE_DIR/SQLitePCLRaw.core.dll" "$DEPLOY_DIR/" 2>/dev/null || true
cp "$RELEASE_DIR/SQLitePCLRaw.batteries_v2.dll" "$DEPLOY_DIR/" 2>/dev/null || true
cp "$RELEASE_DIR/SQLitePCLRaw.provider.e_sqlite3.dll" "$DEPLOY_DIR/" 2>/dev/null || true

# Copy lang files
if [ -d "$RELEASE_DIR/lang" ]; then
  mkdir -p "$DEPLOY_DIR/lang"
  cp "$RELEASE_DIR/lang/"*.json "$DEPLOY_DIR/lang/" 2>/dev/null || echo "Warning: no lang files"
fi

# Copy spawns files
if [ -d "$RELEASE_DIR/spawns" ]; then
  cp -r "$RELEASE_DIR/spawns" "$DEPLOY_DIR/spawns"
fi

# Copy cfg files (so the zip is ready to drop in)
cp -r "$SCRIPT_DIR/cfg" "$DEPLOY_DIR/cfg"

# Display deploy folder contents
echo ""
echo "Deploy folder contents:"
ls -la "$DEPLOY_DIR/"
echo ""

# Zip
cd "$SCRIPT_DIR/bin"
rm -f DerankMix.zip
powershell.exe -Command "Compress-Archive -Path '$(wslpath -w "$DEPLOY_DIR")' -DestinationPath '$(wslpath -w "$SCRIPT_DIR/bin/DerankMix.zip")' -Force"

echo ""
echo "Done! Zip at: $SCRIPT_DIR/bin/DerankMix.zip"
echo ""
echo "To deploy on DatHost:"
echo "  1. Upload DerankMix.zip to the server"
echo "  2. Extract to: csgo/addons/counterstrikesharp/plugins/DerankMix/"
echo "  3. Move DerankMix/cfg/DerankMix/ to: csgo/cfg/DerankMix/"
echo "  4. Restart the server"
