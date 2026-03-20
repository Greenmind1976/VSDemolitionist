# VSDemolitionist 1.1.0

## Highlights
- Added tiered dynamite sticks with configurable blast radius, entity radius, and ore/crystal destruction rates.
- Added tiered dynamite bundles with wide, shallow excavation blasts and configurable rubble output.
- Added block-blasting charges and a detonator for controlled excavation and intact block recovery.
- Added landmines as place-and-arm proximity explosives.
- Added claymore mines as directional proximity explosives with forward trigger coverage.
- Added fisherman's dynamite for underwater use with splash effects and separate water blast handling.
- Added dynamic tooltips for explosives and the detonator, including config-driven destruction rates and charge blast descriptions.
- Added broader ConfigLib integration for explosive balance, proximity mine behavior, and custom explosion sound controls.

## Crafting and processing
- Added a full fuse production chain:
  - bark and resiny bark
  - bark mash and resiny bark mash
  - liquid tar and thick tar
  - fuse crafting
- Added recovery recipes for explosive extraction results, including ore, crystal, halite, meteoric iron, clay, coal, and quartz-hosted resources.

## Audio and effects
- Added separate land blast sounds for sticks, bundles, and charges.
- Added rubble tail sounds for sticks, bundles, and charges.
- Added underwater explosion sound handling and water splash effects.
- Added a config option to disable custom explosion sounds and use vanilla blast audio instead.

## Systems and cleanup
- Removed sticky dynamite from the active mod and archived its assets/code path.
- Added charge placement validation so charges only attach to valid solid terrain.
- Added separate configuration options for standard dynamite, landmine, and claymore damage behavior.
- Fixed standard dynamite sticks underperforming on loose terrain like sand by cleaning up exposed loose blocks around rock-blast craters.
- Added documentation for debug and sound commands.
- Cleaned unused repo assets and archived them.

## Notes
- Unbroken block stack size is not managed by this mod. Use a dedicated stack-size mod if you want larger stacks for recovered blocks.
- The GitHub workflow currently expects a runner with `VINTAGE_STORY` configured so `VintagestoryAPI.dll` is available during build.
