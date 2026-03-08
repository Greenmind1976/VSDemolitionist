using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace VSDemolitionist;

public class ItemDemokettle : Item
{
    private static readonly AssetLocation KettleBlockCode = new("vsdemolitionist", "demokettle");

    public override void OnHeldInteractStart(
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        bool firstEvent,
        ref EnumHandHandling handling)
    {
        if (!firstEvent || slot?.Itemstack == null || blockSel?.Position == null)
        {
            handling = EnumHandHandling.NotHandled;
            return;
        }

        // Place on right click when targeting the top face of a solid block.
        handling = EnumHandHandling.PreventDefault;

        if (byEntity.World.Side != EnumAppSide.Server)
        {
            return;
        }

        Block kettleBlock = byEntity.World.GetBlock(KettleBlockCode);
        if (kettleBlock == null)
        {
            return;
        }

        // Always floor-place: one block above the selected block.
        if (blockSel.Face != BlockFacing.UP)
        {
            return;
        }

        BlockPos placePos = blockSel.Position.UpCopy();
        Block existing = byEntity.World.BlockAccessor.GetBlock(placePos);
        if (existing != null && existing.Replaceable < 6000)
        {
            return;
        }

        Block support = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
        if (support == null || !support.SideSolid[BlockFacing.UP.Index])
        {
            return;
        }

        byEntity.World.BlockAccessor.SetBlock(kettleBlock.BlockId, placePos);
        slot.TakeOut(1);
        slot.MarkDirty();
    }
}
