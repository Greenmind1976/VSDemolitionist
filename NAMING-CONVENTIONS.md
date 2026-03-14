# Naming Conventions

This repo separates **internal bomb system naming** from **player-facing bomb line naming**.

## Goals

- Keep core code/assets generic for all bomb families.
- Let each bomb line (Dynamite, future lines) have its own visual identity.
- Avoid future rename churn.

## Core rule

- `bomb` = internal type/class family (behavior system)
- `dynamite` = one specific bomb line/tier set

## Item codes

Use `bomb-...` item codes for all bomb-family items.

Examples:
- `bomb-copper`
- `bomb-sticky-steel`
- `bomb-bundle-titanium`

Future families should also use `bomb-` item codes, with line tag where needed:
- `bomb-frag-iron`
- `bomb-thermite-steel`

## Shape texture names (block textures)

Use `bomb-...` names for model/shape textures tied to behavior items.

Examples:
- `textures/block/bomb-copper.png`
- `textures/block/bomb-sticky-copper.png`
- `textures/block/bomb-bundle-copper.png`

## Icon texture names (item textures)

Use line-specific names for user-facing icons.

Dynamite line examples:
- `textures/item/dynamite-copper-icon.png`
- `textures/item/dynamite-sticky-copper-icon.png`
- `textures/item/dynamite-bundle-copper-icon.png`

Future line examples:
- `textures/item/frag-iron-icon.png`
- `textures/item/thermite-steel-icon.png`

## Itemtype JSON files

- Behavior itemtypes: `bomb-*.json`
- Icon itemtypes: `bombicon-*.json`

`bombicon-*` entries can point to line-specific icon textures.

## Localization names

Use short, readable player-facing names in `lang/en.json`.

Examples:
- `Copper Cap Dynamite`
- `Sticky Iron Cap Dynamite`
- `Steel Banded Dynamite Bundle`

For future lines, keep same pattern:
- `<Material> <Line Name>`
- `Sticky <Material> <Line Name>`
- `<Material> <Line Name> Bundle`

## Extension checklist for new bomb family

1. Add new item codes under `bomb-*`.
2. Add shapes and block textures under `bomb-*` naming.
3. Add line-specific icon files (e.g., `frag-*`, `thermite-*`, `dynamite-*`).
4. Point `bombicon-*` itemtypes to those line-specific icons.
5. Add localization entries with concise names.

## Current decision

- Internal behavior/assets remain `bomb-*`.
- Dynamite is treated as the current line for player-facing visuals/text.
