#!/usr/bin/env bash

PY_VENV="${HOME}/Documents/VSMods/.codex-tools/venv"

if [ ! -f "${PY_VENV}/bin/activate" ]; then
  echo "Tool venv not found at ${PY_VENV}."
  echo "Run ./setup-codex-image-tools.sh first."
  return 1 2>/dev/null || exit 1
fi

# shellcheck disable=SC1090
source "${PY_VENV}/bin/activate"

echo "Codex image tools activated: ${PY_VENV}"
echo "python: $(command -v python)"
echo "pip:    $(command -v pip)"
echo "magick: $(command -v magick || echo 'not found')"
echo "ffmpeg: $(command -v ffmpeg || echo 'not found')"
