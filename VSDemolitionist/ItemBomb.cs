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

        EntityProperties type = sapi.World.GetEntityType(new AssetLocation("vsdemolitionist:bomb"));
        if (type == null)
        {
            sapi.World.Logger.Error("Bomb entity not found!");
            return;
        }

        Entity entity = sapi.World.ClassRegistry.CreateEntity(type);
        if (entity == null)
        {
            sapi.World.Logger.Error("Failed to create bomb entity instance!");
            return;
        }

        // -----------------------
        // Get throw direction
        // -----------------------

        Vec3f dir = byEntity.SidedPos.GetViewVector().Normalize();

        // Spawn slightly in front of player at eye height
        double startX = byEntity.ServerPos.X + dir.X * 0.5;
        double startY = byEntity.ServerPos.Y + byEntity.LocalEyePos.Y;
        double startZ = byEntity.ServerPos.Z + dir.Z * 0.5;

        entity.ServerPos.SetPos(startX, startY, startZ);
        entity.Pos.SetFrom(entity.ServerPos);

        // -----------------------
        // Soft arc tuning
        // -----------------------

        double forwardStrength = 0.40;   // horizontal speed
        double upwardBoost = 0.10;       // arc lift

        double motionX = dir.X * forwardStrength;
        double motionY = dir.Y * forwardStrength + upwardBoost;
        double motionZ = dir.Z * forwardStrength;

        entity.ServerPos.Motion.Set(motionX, motionY, motionZ);
        entity.Pos.Motion.Set(motionX, motionY, motionZ);

        sapi.World.SpawnEntity(entity);

        slot.TakeOut(1);
        slot.MarkDirty();
    }
}