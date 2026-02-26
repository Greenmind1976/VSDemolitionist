#!/usr/bin/env bash
set -euo pipefail

###############################################################################
# Build + Install VSDemolitionist into /Applications/Vintage Story/Mods (macOS)
###############################################################################

#########################
# Configure
#########################
MOD_ID="vsdemolitionist"
MOD_BUILD_DIR="VSDemolitionist/bin/Debug/Mods/mod"
VS_MODS_DIR="/Applications/Vintage Story.app/Mods"

#########################
# Build solution
#########################
dotnet build

#########################
# Install mod
#########################
if [[ ! -d "$MOD_BUILD_DIR" ]]; then
  echo "ERROR: Expected build output folder not found: $MOD_BUILD_DIR" >&2
  exit 1
fi

# /Applications is often write-protected; use sudo when needed.
if [[ ! -w "$VS_MODS_DIR" ]]; then
  echo "🔐 Mods folder not writable, using sudo..."
  sudo mkdir -p "$VS_MODS_DIR"
  sudo rm -rf "$VS_MODS_DIR/$MOD_ID"
  sudo cp -R "$MOD_BUILD_DIR" "$VS_MODS_DIR/$MOD_ID"
else
  mkdir -p "$VS_MODS_DIR"
  rm -rf "$VS_MODS_DIR/$MOD_ID"
  cp -R "$MOD_BUILD_DIR" "$VS_MODS_DIR/$MOD_ID"
fi

echo "✅ Installed '$MOD_ID' to:"
echo "  $VS_MODS_DIR/$MOD_ID"

#########################
# Launch Vintage Story (optional)
#########################
if [[ -n "${VINTAGE_STORY:-}" ]]; then
  open "$VINTAGE_STORY"
fi