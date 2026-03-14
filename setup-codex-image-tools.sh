#!/usr/bin/env bash
set -euo pipefail

# Prevent leaking shell options when accidentally sourced.
if (return 0 2>/dev/null); then
  echo "Run this script, do not source it:"
  echo "  ./setup-codex-image-tools.sh"
  return 1
fi

# Installs reusable tooling outside any specific mod repo.
# Safe to run multiple times.

TOOLS_DIR="${HOME}/Documents/VSMods/.codex-tools"
PY_VENV="${TOOLS_DIR}/venv"

echo "Creating tools folder at: ${TOOLS_DIR}"
mkdir -p "${TOOLS_DIR}"

if ! command -v brew >/dev/null 2>&1; then
  echo "Homebrew not found. Install Homebrew first: https://brew.sh/"
  exit 1
fi

echo "Installing system tools (python, imagemagick, ffmpeg)..."
export HOMEBREW_NO_AUTO_UPDATE=1
brew list --formula python >/dev/null 2>&1 || brew install python
brew list --formula imagemagick >/dev/null 2>&1 || brew install imagemagick
brew list --formula ffmpeg >/dev/null 2>&1 || brew install ffmpeg

if [ ! -f "${PY_VENV}/bin/activate" ]; then
  echo "Creating Python virtual environment at ${PY_VENV}"
  python3 -m venv "${PY_VENV}"
else
  echo "Using existing Python virtual environment at ${PY_VENV}"
fi

# shellcheck disable=SC1090
source "${PY_VENV}/bin/activate"
MISSING_PKGS="$(python - <<'PY'
import importlib.util
missing = []
for name in ("PIL", "numpy"):
    if importlib.util.find_spec(name) is None:
        missing.append(name)
print(" ".join(missing))
PY
)"

if [ -n "${MISSING_PKGS}" ]; then
  echo "Installing missing Python packages: ${MISSING_PKGS}"
  pip install ${MISSING_PKGS}
else
  echo "Python packages already present: pillow numpy"
fi

cat <<INFO

Done.

Tool locations:
- Venv: ${PY_VENV}
- ImageMagick: $(command -v magick || true)
- ffmpeg: $(command -v ffmpeg || true)

To activate in a shell:
  source "${PY_VENV}/bin/activate"

Quick verification:
  python - <<'PY'
from PIL import Image
import numpy
print("Pillow + numpy OK")
PY

INFO
