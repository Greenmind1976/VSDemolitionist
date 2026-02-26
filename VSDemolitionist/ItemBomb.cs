using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace VSDemolitionist;

public class ItemBomb : Item
{
    public override void OnHeldInteractStart(
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        bool firstEvent,
        ref EnumHandHandling handling)
    {
        if (!firstEvent) return;

        handling = EnumHandHandling.PreventDefault;

        if (byEntity.World.Side != EnumAppSide.Server) return;

        ICoreServerAPI sapi = (ICoreServerAPI)byEntity.World.Api;

        EntityProperties type = sapi.World.GetEntityType(
            new AssetLocation("vsdemolitionist:bomb")
        );

        if (type == null)
        {
            sapi.World.Logger.Notification("Bomb entity not found!");
            return;
        }

        // -------------------------------
        // SPAWN + THROW (this is the block)
        // -------------------------------
        Entity entity = sapi.World.ClassRegistry.CreateEntity(type);

        Vec3f view = byEntity.SidedPos.GetViewVector();

        // spawn slightly in front and slightly below eye level
        double startX = byEntity.ServerPos.X + view.X * 0.5;
        double startY = byEntity.ServerPos.Y + byEntity.LocalEyePos.Y - 0.2;
        double startZ = byEntity.ServerPos.Z + view.Z * 0.5;

        // Set BOTH Pos and ServerPos
        entity.ServerPos.SetPos(startX, startY, startZ);
        entity.Pos.SetPos(startX, startY, startZ);

        // Set motion BEFORE spawning
        float strength = 1.2f;
        entity.ServerPos.Motion.Set(
            view.X * strength,
            view.Y * strength,
            view.Z * strength
        );

        // Now spawn
        sapi.World.SpawnEntity(entity);

        // Consume one item
        slot.TakeOut(1);
        slot.MarkDirty();
    }
}