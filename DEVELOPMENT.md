# Development Setup (VSDemolitionist)

This repo uses:
- Vintage Story API via local game install (`VINTAGE_STORY` env var)
- Shared external image tooling in `~/Documents/VSMods/.codex-tools`

## 1) One-time tooling setup

From repo root:

```bash
chmod +x setup-codex-image-tools.sh activate-tools.sh
./setup-codex-image-tools.sh
```

## 2) Activate tools in a shell

```bash
source ./activate-tools.sh
```

## 3) Set Vintage Story path

```bash
export VINTAGE_STORY="/Applications/Vintage Story.app/Contents/Resources"
```

This project file expects that path variable:
- `VSDemolitionist/VSDemolitionist.csproj`

## 4) Build

```bash
./build.sh
```

Or:

```bash
dotnet build VSDemolitionist.sln
```

## Notes

- Tools are intentionally installed outside this repo.
- Re-running setup is safe.
- If Python packages break after updates, run setup again.
