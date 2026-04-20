#!/usr/bin/env bash
set -euo pipefail

###############################################################################
# Build + Install VSDemolitionist into Vintage Story 1.21.7
###############################################################################

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CURRENT_BRANCH="$(git -C "$ROOT_DIR" branch --show-current 2>/dev/null || true)"
TARGET_BRANCH="support/1.21"

find_worktree_for_branch() {
  local branch_name="$1"

  git -C "$ROOT_DIR" worktree list --porcelain | awk -v target="refs/heads/$branch_name" '
    $1 == "worktree" { wt = $2 }
    $1 == "branch" && $2 == target { print wt; exit }
  '
}

if [[ "$CURRENT_BRANCH" != "$TARGET_BRANCH" ]]; then
  TARGET_WORKTREE="$(find_worktree_for_branch "$TARGET_BRANCH")"

  if [[ -z "$TARGET_WORKTREE" ]]; then
    echo "ERROR: Could not find worktree for $TARGET_BRANCH" >&2
    exit 1
  fi

  echo "Switching to $TARGET_BRANCH worktree:"
  echo "  $TARGET_WORKTREE"
  exec "$TARGET_WORKTREE/build-install.sh" "$@"
fi

cd "$ROOT_DIR"

#########################
# Configure
#########################
MOD_ID="vsdemolitionist"
MOD_BUILD_DIR="VSDemolitionist/bin/Debug/Mods/mod"
VS_APP_DIR="/Applications/Vintage Story 1.21.7.app"
VS_MODS_DIR="$VS_APP_DIR/Mods"
VS_EXECUTABLE="$VS_APP_DIR/Vintagestory"
VS_DATA_PATH="$HOME/Library/Application Support/VintagestoryData-1.21.7"

#########################
# Clean up old builds
#########################
rm -rf "VSDemolitionist/bin" "VSDemolitionist/obj"

echo "Deleting installed mod dir: $VS_MODS_DIR/$MOD_ID"
rm -rf "$VS_MODS_DIR/$MOD_ID"

# Sanity check
if [[ -e "$VS_MODS_DIR/$MOD_ID" ]]; then
  echo "ERROR: Mod dir still exists: $VS_MODS_DIR/$MOD_ID"
  exit 1
fi

#########################
# Build solution
#########################
VINTAGE_STORY="$VS_APP_DIR" dotnet build -p:NuGetAudit=false

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
if [[ ! -x "$VS_EXECUTABLE" ]]; then
  echo
  echo "Vintage Story 1.21.7 executable not found at: $VS_EXECUTABLE"
  echo "Check that the app bundle contains the Vintagestory executable."
  exit 0
fi

echo
echo "Launching Vintage Story 1.21.7 via:"
echo "  $VS_EXECUTABLE"
echo "Using data path:"
echo "  $VS_DATA_PATH"
mkdir -p "$VS_DATA_PATH"
"$VS_EXECUTABLE" --dataPath "$VS_DATA_PATH" >/dev/null 2>&1 &
