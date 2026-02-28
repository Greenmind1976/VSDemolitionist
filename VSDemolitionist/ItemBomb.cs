using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace VSDemolitionist;

public class ItemBomb : Item
{
    private const float LightTime = 1.0f;
    private ILoadedSound? fuseSound;

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

        if (byEntity.RightHandItemSlot != slot) return;

        ItemSlot offhand = byEntity.LeftHandItemSlot;
        if (offhand?.Itemstack == null) return;
        if (offhand.Itemstack.Collectible.Code.Path != "torch") return;
    }

    public override bool OnHeldInteractStep(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel)
    {
        if (byEntity.World.Side != EnumAppSide.Client)
            return true;

        // Start sound once fuse is lit
        if (secondsUsed >= LightTime && fuseSound == null)
        {
            ICoreClientAPI capi = (ICoreClientAPI)byEntity.World.Api;

            fuseSound = capi.World.LoadSound(new SoundParams()
            {
                Location = new AssetLocation("vsdemolitionist", "sounds/fuse"),
                ShouldLoop = true,
                DisposeOnFinish = true,
                Range = 32f,
                Volume = 1f,
                Position = new Vec3f(
                    (float)byEntity.Pos.X,
                    (float)byEntity.Pos.Y,
                    (float)byEntity.Pos.Z
                )
            });

            fuseSound?.Start();
        }

        if (fuseSound != null)
        {
            fuseSound.SetPosition(new Vec3f(
                (float)byEntity.Pos.X,
                (float)byEntity.Pos.Y,
                (float)byEntity.Pos.Z
            ));
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
        if (byEntity.World.Side == EnumAppSide.Client)
        {
            fuseSound?.Stop();
            fuseSound = null;
        }

        if (secondsUsed < LightTime) return;
        if (byEntity.World.Side != EnumAppSide.Server) return;

        ICoreServerAPI sapi = (ICoreServerAPI)byEntity.World.Api;

        EntityProperties type = sapi.World.GetEntityType(new AssetLocation("vsdemolitionist:bomb"));
        if (type == null) return;

        Entity entity = sapi.World.ClassRegistry.CreateEntity(type);
        if (entity == null) return;

        Vec3f dir = byEntity.SidedPos.GetViewVector().Normalize();

        double startX = byEntity.ServerPos.X + dir.X * 0.5;
        double startY = byEntity.ServerPos.Y + byEntity.LocalEyePos.Y;
        double startZ = byEntity.ServerPos.Z + dir.Z * 0.5;

        entity.ServerPos.SetPos(startX, startY, startZ);
        entity.Pos.SetFrom(entity.ServerPos);

        double forwardStrength = 0.40;
        double upwardBoost = 0.10;

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