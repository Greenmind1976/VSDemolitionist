# VSDemolitionist

Explosives-focused Vintage Story mod with thrown dynamite, bundles, charges, and supporting crafting/process chains.

## Notes

- Charges can generate very large numbers of dropped blocks when used aggressively.
- Large linked charge blasts can overwhelm a world/session if the drops are not collected or cleared.
- Test large charge patterns carefully, especially in worlds where many old dropped items may already exist.
- This mod does not change stack sizes for recovered unbroken blocks. If you want larger stack sizes, use a dedicated stack-size mod.

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
