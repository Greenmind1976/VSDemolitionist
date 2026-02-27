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

        Vec3f view = byEntity.SidedPos.GetViewVector();

        // Spawn clearly in front of the player (avoid spawning inside camera/player hitbox)
        double startX = byEntity.ServerPos.X + view.X * 2.5;
        double startY = byEntity.ServerPos.Y + byEntity.LocalEyePos.Y - 0.2;
        double startZ = byEntity.ServerPos.Z + view.Z * 2.5;

        entity.ServerPos.SetPos(startX, startY, startZ);
        entity.Pos.SetFrom(entity.ServerPos);

        float strength = 2.5f;

        entity.ServerPos.Motion.Set(view.X * strength, view.Y * strength, view.Z * strength);
        entity.Pos.Motion.Set(view.X * strength, view.Y * strength, view.Z * strength);

        sapi.World.SpawnEntity(entity);

        slot.TakeOut(1);
        slot.MarkDirty();
    }
}