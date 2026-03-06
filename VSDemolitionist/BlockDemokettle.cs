using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace VSDemolitionist;

public class BlockDemokettle : Block
{
    public override bool OnBlockInteractStart(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel)
    {
        if (blockSel?.Position == null) return true;

        // Sneak-right-click picks up the kettle as an item.
        if (byPlayer?.Entity?.Controls?.Sneak == true)
        {
            if (world.Side == EnumAppSide.Server)
            {
                ItemStack stack = new(this);
                bool inserted = byPlayer.InventoryManager.TryGiveItemstack(stack);
                if (!inserted)
                {
                    world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(0.5, 0.2, 0.5));
                }

                world.BlockAccessor.SetBlock(0, blockSel.Position);
            }

            return true;
        }

        return true;
    }
}
