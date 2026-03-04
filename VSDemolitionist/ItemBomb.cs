using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace VSDemolitionist;

public class ItemBomb : Item
{
    private const float LightTime = 1.0f;
    private const float DefaultFuseSeconds = 4.0f;
    private const float DefaultHeldFuseVolume = 2.0f;
    private const string BombAttrRoot = "bomb";
    private const string AttrFuseLitMs = "vsd_fuseLitMs";
    private static readonly AssetLocation ThrowSound = new("vsdemolitionist", "sounds/bomb-toss");

    private ILoadedSound? heldFuseSound;
    private bool heldFusePlaying;
    private int heldFusePlaybackId;
    private ItemSlot? guiIconSlot;

    private bool IsIgnitionItem(ItemSlot slot)
    {
        if (slot?.Itemstack == null) return false;

        string path = slot.Itemstack.Collectible.Code?.Path ?? "";

        if (path.Contains("torch")) return true;
        if (path.Contains("lantern")) return true;
        if (path.Contains("lamp")) return true;

        return false;
    }

    private float GetBombFloat(ItemStack? stack, string key, float defaultValue)
    {
        return stack?.Collectible?.Attributes?[BombAttrRoot]?[key].AsFloat(defaultValue) ?? defaultValue;
    }

    private string GetBombString(ItemStack? stack, string key, string defaultValue)
    {
        return stack?.Collectible?.Attributes?[BombAttrRoot]?[key].AsString(defaultValue) ?? defaultValue;
    }

    private bool GetBombBool(ItemStack? stack, string key, bool defaultValue)
    {
        return stack?.Collectible?.Attributes?[BombAttrRoot]?[key].AsBool(defaultValue) ?? defaultValue;
    }

    private static AssetLocation ResolveModAsset(string code)
    {
        if (code.Contains(":")) return new AssetLocation(code);
        return new AssetLocation("vsdemolitionist", code);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);

        if (target != EnumItemRenderTarget.Gui)
        {
            return;
        }

        string iconCode = GetBombString(itemstack, "iconItemCode", "bombicon");
        Item? iconItem = capi.World.GetItem(new AssetLocation("vsdemolitionist", iconCode));
        if (iconItem == null)
        {
            return;
        }

        guiIconSlot ??= new DummySlot();
        guiIconSlot.Itemstack = new ItemStack(iconItem, 1);

        ItemRenderInfo iconRender = capi.Render.GetItemStackRenderInfo(guiIconSlot, target, 0);
        renderinfo = iconRender;
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
        if (slot?.Itemstack == null)
        {
            StopHeldFuseSound();
            return false;
        }

        ItemSlot offhand = byEntity.LeftHandItemSlot;
        if (!IsIgnitionItem(offhand))
        {
            StopHeldFuseSound();
            return false;
        }

        float fuseSeconds = GetBombFloat(slot.Itemstack, "fuseSeconds", DefaultFuseSeconds);
        float lightTime = GetBombFloat(slot.Itemstack, "lightTime", LightTime);
        float maxHoldSeconds = lightTime + fuseSeconds;
        float heldFuseVolume = GetBombFloat(slot.Itemstack, "heldFuseVolume", DefaultHeldFuseVolume);

        if (secondsUsed >= lightTime)
        {
            if (byEntity.World.Side == EnumAppSide.Server)
            {
                long litMs = slot.Itemstack.Attributes.GetLong(AttrFuseLitMs, 0);
                if (litMs == 0)
                {
                    slot.Itemstack.Attributes.SetLong(AttrFuseLitMs, byEntity.World.ElapsedMilliseconds);
                    slot.MarkDirty();
                }
            }

            if (byEntity.World.Side == EnumAppSide.Client)
            {
                StartHeldFuseSound(byEntity.World.Api as ICoreClientAPI, heldFuseVolume);
            }
        }

        // Auto-force release/throw when fuse has fully elapsed while still holding right click.
        if (secondsUsed >= maxHoldSeconds)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                ICoreClientAPI? capi = byEntity.World.Api as ICoreClientAPI;
                StopHeldFuseSound();
            }

            if (byEntity.World.Side == EnumAppSide.Server)
            {
                SpawnBombFromLitState(slot, byEntity, allowThrow: false, targetBlockSel: null);
            }

            return false;
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
        float fuseSeconds = GetBombFloat(slot.Itemstack, "fuseSeconds", DefaultFuseSeconds);
        float lightTime = GetBombFloat(slot.Itemstack, "lightTime", LightTime);
        bool timedOut = secondsUsed >= (lightTime + fuseSeconds);

        if (byEntity.World.Side == EnumAppSide.Client)
        {
            ICoreClientAPI? capi = byEntity.World.Api as ICoreClientAPI;
            // Bridge network latency so entity fuse audio can take over without an audible gap.
            if (timedOut)
            {
                StopHeldFuseSound();
            }
            else
            {
                StopHeldFuseSoundDelayed(capi, 1200);
            }
        }

        if (byEntity.World.Side == EnumAppSide.Server)
        {
            SpawnBombFromLitState(slot, byEntity, allowThrow: !timedOut, targetBlockSel: blockSel);
        }
    }

    public override bool OnHeldInteractCancel(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        EnumItemUseCancelReason cancelReason)
    {
        if (byEntity.World.Side == EnumAppSide.Client)
        {
            StopHeldFuseSoundDelayed(byEntity.World.Api as ICoreClientAPI, 1200);
        }
        else
        {
            StopHeldFuseSound();
        }
        if (slot?.Itemstack != null && byEntity.World.Side == EnumAppSide.Server)
        {
            slot.Itemstack.Attributes.RemoveAttribute(AttrFuseLitMs);
            slot.MarkDirty();
        }
        return true;
    }

    private void StartHeldFuseSound(ICoreClientAPI? capi, float volume)
    {
        if (capi == null) return;

        if (heldFusePlaying)
        {
            heldFuseSound?.SetVolume(volume);
            return;
        }

        heldFuseSound = capi.World.LoadSound(new SoundParams()
        {
            Location = new AssetLocation("vsdemolitionist", "sounds/fuse"),
            ShouldLoop = true,
            DisposeOnFinish = false,
            RelativePosition = true,
            Position = new Vec3f(0, 0, 0),
            Range = 8f,
            Volume = volume
        });

        heldFuseSound?.Start();
        heldFusePlaying = true;
        heldFusePlaybackId++;
    }

    private void StopHeldFuseSound()
    {
        if (!heldFusePlaying) return;

        heldFuseSound?.Stop();
        heldFuseSound?.Dispose();
        heldFuseSound = null;
        heldFusePlaying = false;
    }

    private void StopHeldFuseSoundDelayed(ICoreClientAPI? capi, int delayMs)
    {
        if (!heldFusePlaying || capi == null)
        {
            StopHeldFuseSound();
            return;
        }

        int playbackIdAtSchedule = heldFusePlaybackId;
        capi.Event.RegisterCallback(_ =>
        {
            if (heldFusePlaying && heldFusePlaybackId == playbackIdAtSchedule)
            {
                StopHeldFuseSound();
            }
        }, delayMs);
    }

    private static void PlayClientOneShot(ICoreClientAPI? capi, string soundPath, bool relative, float volume)
    {
        if (capi == null) return;

        ILoadedSound? oneshot = capi.World.LoadSound(new SoundParams()
        {
            Location = new AssetLocation(soundPath),
            ShouldLoop = false,
            DisposeOnFinish = true,
            RelativePosition = relative,
            Position = new Vec3f(0, 0, 0),
            Range = 16f,
            Volume = volume
        });

        oneshot?.Start();
    }

    private void SpawnBombFromLitState(ItemSlot slot, EntityAgent byEntity, bool allowThrow, BlockSelection? targetBlockSel)
    {
        long litMs = slot.Itemstack.Attributes.GetLong(AttrFuseLitMs, 0);
        slot.Itemstack.Attributes.RemoveAttribute(AttrFuseLitMs);
        if (litMs == 0) return;

        float fuseSeconds = GetBombFloat(slot.Itemstack, "fuseSeconds", DefaultFuseSeconds);
        double elapsed = Math.Max(0, (byEntity.World.ElapsedMilliseconds - litMs) / 1000.0);
        float remainingFuse = (float)Math.Max(0, fuseSeconds - elapsed);

        ICoreServerAPI sapi = (ICoreServerAPI)byEntity.World.Api;
        string entityCode = GetBombString(slot.Itemstack, "entityCode", "bomb");
        EntityProperties type = sapi.World.GetEntityType(ResolveModAsset(entityCode));
        if (type == null) return;

        Entity entity = sapi.World.ClassRegistry.CreateEntity(type);
        if (entity == null) return;

        Vec3f dir = byEntity.SidedPos.GetViewVector().Normalize();
        double spawnX = byEntity.ServerPos.X + dir.X * 0.25;
        double spawnY = byEntity.ServerPos.Y + byEntity.LocalEyePos.Y - 0.35;
        double spawnZ = byEntity.ServerPos.Z + dir.Z * 0.25;

        entity.ServerPos.SetPos(spawnX, spawnY, spawnZ);
        entity.ServerPos.Motion.Set(0, 0, 0);
        entity.Pos.SetPos(spawnX, spawnY, spawnZ);
        entity.Pos.Motion.Set(0, 0, 0);

        sapi.World.SpawnEntity(entity);

        if (entity is EntityBomb bomb)
        {
            bool isSticky = GetBombBool(slot.Itemstack, "isSticky", false);
            bomb.ApplyConfigFromItemstack(slot.Itemstack);
            bomb.SetThrower(byEntity);
            bomb.StartFuseWithRemainingSeconds(remainingFuse);
            if (allowThrow && remainingFuse > 0.001f)
            {
                bomb.Release(byEntity);
                if (isSticky && targetBlockSel?.Position != null)
                {
                    bomb.SetPreferredAttach(targetBlockSel.Position, targetBlockSel.Face);
                }

                byEntity.World.PlaySoundAt(
                    ThrowSound,
                    spawnX, spawnY, spawnZ,
                    null,
                    false,
                    24f,
                    0.8f
                );
            }
        }

        slot.TakeOut(1);
        slot.MarkDirty();
    }
}
