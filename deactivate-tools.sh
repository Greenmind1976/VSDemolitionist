#!/usr/bin/env bash

# Must be sourced to affect current shell.
if ! (return 0 2>/dev/null); then
  echo "This script must be sourced: source ./deactivate-tools.sh"
  exit 1
fi

TOOLS_VENV="${HOME}/Documents/VSMods/.codex-tools/venv"
VENV_BIN="${TOOLS_VENV}/bin"

if command -v deactivate >/dev/null 2>&1; then
  deactivate
else
  # Fallback cleanup if deactivate function is unavailable.
  if [ -n "${PATH:-}" ]; then
    PATH=":$PATH:"
    PATH="${PATH//:${VENV_BIN}:/:}"
    PATH="${PATH#:}"
    PATH="${PATH%:}"
    export PATH
  fi
  unset VIRTUAL_ENV
fi

echo "Codex image tools deactivated."
