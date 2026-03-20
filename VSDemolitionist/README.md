# VSDemolitionist

Explosives-focused Vintage Story mod with thrown dynamite, bundles, blasting charges, proximity mines, and supporting crafting/process chains.

## Notes

- Charges can generate very large numbers of dropped blocks when used aggressively.
- Large linked charge blasts can overwhelm a world/session if the drops are not collected or cleared.
- Test large charge patterns carefully, especially in worlds where many old dropped items may already exist.
- This mod does not change stack sizes for recovered unbroken blocks. If you want larger stack sizes, use a dedicated stack-size mod.
- Claymores are wall-mounted directional proximity mines and only trigger on targets moving through the space in front of them.
- Proximity mines (landmines and claymores) arm after a short delay and include an owner grace period, so the player who armed them has a brief window to step clear before they can trigger.
- ConfigLib settings now include shared dynamite damage toggles plus separate landmine and claymore damage/grace settings.

## Debug Commands

- `/dynamitedebug on`
- `/dynamitedebug off`
- `/dynamitesounds on`
- `/dynamitesounds off`

## Release

- Build a release zip locally with:

```bash
./release.sh
```

- The packaged mod zip is written to:
  - `dist/vsdemolitionist-<version>.zip`
