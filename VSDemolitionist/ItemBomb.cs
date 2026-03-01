using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace VSDemolitionist;

public class ItemBomb : Item
{
    private const float LightTime = 1.0f;
    private const string AttrEntityId = "vsd_entityId";

    private bool IsIgnitionItem(ItemSlot slot)
    {
        if (slot?.Itemstack == null) return false;

        string path = slot.Itemstack.Collectible.Code?.Path ?? "";

        if (path.Contains("torch")) return true;
        if (path.Contains("lantern")) return true;
        if (path.Contains("lamp")) return true;

        return false;
    }

    public override void OnHeldInteractStart(
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        bool firstEvent,
        ref EnumHandHandling handling)
    {
        if (!firstEvent) return;

        ItemSlot offhand = byEntity.LeftHandItemSlot;

        if (!IsIgnitionItem(offhand))
        {
            handling = EnumHandHandling.NotHandled;
            return;
        }

        handling = EnumHandHandling.PreventDefault;
    }

    public override bool OnHeldInteractStep(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel)
    {
        if (slot?.Itemstack == null) return true;

        ItemSlot offhand = byEntity.LeftHandItemSlot;
        if (!IsIgnitionItem(offhand))
            return true;

        long id = slot.Itemstack.Attributes.GetLong(AttrEntityId, 0);

        if (id != 0)
        {
            if (byEntity.World.Side == EnumAppSide.Server)
            {
                Entity e = byEntity.World.GetEntityById(id);
                if (e is EntityBomb bomb)
                {
                    bomb.SetHeldBy(byEntity);
                }
            }

            return true;
        }

        if (secondsUsed >= LightTime && byEntity.World.Side == EnumAppSide.Server)
        {
            ICoreServerAPI sapi = (ICoreServerAPI)byEntity.World.Api;

            EntityProperties type = sapi.World.GetEntityType(new AssetLocation("vsdemolitionist:bomb"));
            if (type == null) return true;

            Entity entity = sapi.World.ClassRegistry.CreateEntity(type);
            if (entity == null) return true;

            entity.ServerPos.SetPos(byEntity.ServerPos.X, byEntity.ServerPos.Y, byEntity.ServerPos.Z);
            entity.ServerPos.Motion.Set(0, 0, 0);

            entity.Pos.SetPos(byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
            entity.Pos.Motion.Set(0, 0, 0);

            sapi.World.SpawnEntity(entity);

            if (entity is EntityBomb bomb)
            {
                bomb.StartFuse();
                bomb.SetHeldBy(byEntity);
            }

            slot.Itemstack.Attributes.SetLong(AttrEntityId, entity.EntityId);
            slot.MarkDirty();
        }

        return true;
    }

    public override void OnHeldInteractStop(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel)
    {
        if (slot?.Itemstack == null) return;

        long id = slot.Itemstack.Attributes.GetLong(AttrEntityId, 0);
        if (id == 0) return;

        if (byEntity.World.Side != EnumAppSide.Server) return;

        Entity e = byEntity.World.GetEntityById(id);
        if (e is EntityBomb bomb)
        {
            bomb.Release(byEntity);
        }

        slot.TakeOut(1);
        slot.MarkDirty();
        slot.Itemstack.Attributes.RemoveAttribute(AttrEntityId);
    }
}