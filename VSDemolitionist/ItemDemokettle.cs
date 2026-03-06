using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace VSDemolitionist;

public class ItemDemokettle : Item
{
    private static readonly AssetLocation KettleBlockCode = new("vsdemolitionist", "demokettle");
    private const string IconItemCode = "demokettleicon";
    private ItemSlot? guiIconSlot;

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);

        if (target != EnumItemRenderTarget.Gui) return;

        Item? iconItem = capi.World.GetItem(new AssetLocation("vsdemolitionist", IconItemCode));
        if (iconItem == null) return;

        guiIconSlot ??= new DummySlot();
        guiIconSlot.Itemstack = new ItemStack(iconItem, 1);
        renderinfo = capi.Render.GetItemStackRenderInfo(guiIconSlot, target, 0);
    }

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

        // Place like other ground-placed utility items: sneak + right click.
        if (!byEntity.Controls.Sneak)
        {
            handling = EnumHandHandling.NotHandled;
            return;
        }

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
