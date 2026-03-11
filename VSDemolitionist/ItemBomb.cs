using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Config;

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
        if (slot?.Itemstack == null)
        {
            handling = EnumHandHandling.NotHandled;
            return;
        }

        bool placeOnly = GetBombBool(slot?.Itemstack, "placeOnly", false);
        if (placeOnly)
        {
            if (blockSel?.Position == null)
            {
                handling = EnumHandHandling.NotHandled;
                return;
            }

            handling = EnumHandHandling.PreventDefault;
            if (byEntity.World.Side == EnumAppSide.Server)
            {
                SpawnPlacedBomb(slot!, byEntity, blockSel);
            }
            return;
        }

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
        if (GetBombBool(slot?.Itemstack, "placeOnly", false))
        {
            return false;
        }

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
        if (GetBombBool(slot?.Itemstack, "placeOnly", false))
        {
            return;
        }

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
        bool placeOnFace = GetBombBool(slot.Itemstack, "placeOnFace", false);
        if (placeOnFace && targetBlockSel?.Position == null)
        {
            // Placement-only charges require a valid target face.
            return;
        }

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
            if (placeOnFace)
            {
                bomb.AttachToBlock(targetBlockSel!.Position, targetBlockSel.Face);
            }
            else if (allowThrow && remainingFuse > 0.001f)
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

    private void SpawnPlacedBomb(ItemSlot slot, EntityAgent byEntity, BlockSelection targetBlockSel)
    {
        if (slot?.Itemstack == null || targetBlockSel?.Position == null) return;
        ICoreServerAPI sapi = (ICoreServerAPI)byEntity.World.Api;
        string entityCode = GetBombString(slot.Itemstack, "placedEntityCode", GetBombString(slot.Itemstack, "entityCode", "bomb"));
        EntityProperties type = sapi.World.GetEntityType(ResolveModAsset(entityCode));
        if (type == null) return;

        Entity entity = sapi.World.ClassRegistry.CreateEntity(type);
        if (entity == null) return;

        entity.ServerPos.SetPos(
            targetBlockSel.Position.X + 0.5,
            targetBlockSel.Position.Y + 0.5,
            targetBlockSel.Position.Z + 0.5
        );
        entity.ServerPos.Motion.Set(0, 0, 0);
        entity.Pos.SetPos(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
        entity.Pos.Motion.Set(0, 0, 0);

        sapi.World.SpawnEntity(entity);

        if (entity is EntityBomb bomb)
        {
            bomb.ApplyConfigFromItemstack(slot.Itemstack);
            bomb.SetThrower(byEntity);
            bool placeTopOnly = GetBombBool(slot.Itemstack, "placeTopOnly", false);
            BlockFacing face = placeTopOnly ? BlockFacing.UP : ResolveAttachFace(targetBlockSel, byEntity);
            face ??= BlockFacing.UP;
            if (face == BlockFacing.DOWN)
            {
                // Underside placement is disabled for charges to avoid clipping/sinking behavior.
                entity.Die(EnumDespawnReason.Removed);
                if (byEntity is EntityPlayer entityPlayer && entityPlayer.Player is IServerPlayer serverPlayer)
                {
                    serverPlayer.SendMessage(
                        GlobalConstants.GeneralChatGroup,
                        "Cannot place charge on underside.",
                        EnumChatType.Notification
                    );
                }
                return;
            }

            BlockPos attachPos = targetBlockSel.Position.Copy();
            Block targetBlock = byEntity.World.BlockAccessor.GetBlock(attachPos);
            // If selection position is non-solid (common on place interactions), snap to the support block behind the hit face.
            if ((targetBlock == null || targetBlock.BlockId == 0 || targetBlock.Replaceable >= 6000) && face != null)
            {
                attachPos = attachPos.AddCopy(face.Opposite);
            }

            bomb.AttachToBlock(attachPos, face);
        }

        slot.TakeOut(1);
        slot.MarkDirty();
    }

    private static BlockFacing ResolveAttachFace(BlockSelection blockSel, EntityAgent byEntity)
    {
        if (blockSel?.Position != null && TryResolveFaceFromRay(blockSel.Position, byEntity, out BlockFacing rayFace))
        {
            return rayFace;
        }

        if (blockSel?.Face != null) return blockSel.Face;

        if (blockSel?.HitPosition != null)
        {
            double hx = NormalizeToUnit(blockSel.HitPosition.X);
            double hy = NormalizeToUnit(blockSel.HitPosition.Y);
            double hz = NormalizeToUnit(blockSel.HitPosition.Z);

            double dx0 = hx;
            double dx1 = 1.0 - hx;
            double dy0 = hy;
            double dy1 = 1.0 - hy;
            double dz0 = hz;
            double dz1 = 1.0 - hz;

            double min = dx0;
            BlockFacing face = BlockFacing.WEST;

            if (dx1 < min) { min = dx1; face = BlockFacing.EAST; }
            if (dy0 < min) { min = dy0; face = BlockFacing.DOWN; }
            if (dy1 < min) { min = dy1; face = BlockFacing.UP; }
            if (dz0 < min) { min = dz0; face = BlockFacing.NORTH; }
            if (dz1 < min) { face = BlockFacing.SOUTH; }

            return face;
        }

        Vec3f dir = byEntity.SidedPos.GetViewVector().Normalize();
        float ax = Math.Abs(dir.X);
        float ay = Math.Abs(dir.Y);
        float az = Math.Abs(dir.Z);

        if (ay >= ax && ay >= az) return dir.Y >= 0 ? BlockFacing.DOWN : BlockFacing.UP;
        if (ax >= ay && ax >= az) return dir.X >= 0 ? BlockFacing.WEST : BlockFacing.EAST;
        return dir.Z >= 0 ? BlockFacing.NORTH : BlockFacing.SOUTH;
    }

    private static double NormalizeToUnit(double value)
    {
        if (value >= 0 && value <= 1) return value;
        double frac = value - Math.Floor(value);
        return frac < 0 ? frac + 1 : frac;
    }

    private static bool TryResolveFaceFromRay(BlockPos blockPos, EntityAgent byEntity, out BlockFacing face)
    {
        face = BlockFacing.UP;

        Vec3d origin = byEntity.ServerPos.XYZ.AddCopy(byEntity.LocalEyePos);
        Vec3f dirf = byEntity.SidedPos.GetViewVector().Normalize();
        Vec3d dir = new Vec3d(dirf.X, dirf.Y, dirf.Z);

        const double eps = 1e-7;
        double minX = blockPos.X;
        double minY = blockPos.Y;
        double minZ = blockPos.Z;
        double maxX = blockPos.X + 1.0;
        double maxY = blockPos.Y + 1.0;
        double maxZ = blockPos.Z + 1.0;

        double tMin = double.NegativeInfinity;
        double tMax = double.PositiveInfinity;
        BlockFacing enterFace = BlockFacing.UP;

        if (Math.Abs(dir.X) < eps)
        {
            if (origin.X < minX || origin.X > maxX) return false;
        }
        else
        {
            double tx1 = (minX - origin.X) / dir.X;
            double tx2 = (maxX - origin.X) / dir.X;
            BlockFacing fx = dir.X > 0 ? BlockFacing.WEST : BlockFacing.EAST;
            if (tx1 > tx2) (tx1, tx2) = (tx2, tx1);
            if (tx1 > tMin) { tMin = tx1; enterFace = fx; }
            if (tx2 < tMax) tMax = tx2;
            if (tMin > tMax) return false;
        }

        if (Math.Abs(dir.Y) < eps)
        {
            if (origin.Y < minY || origin.Y > maxY) return false;
        }
        else
        {
            double ty1 = (minY - origin.Y) / dir.Y;
            double ty2 = (maxY - origin.Y) / dir.Y;
            BlockFacing fy = dir.Y > 0 ? BlockFacing.DOWN : BlockFacing.UP;
            if (ty1 > ty2) (ty1, ty2) = (ty2, ty1);
            if (ty1 > tMin) { tMin = ty1; enterFace = fy; }
            if (ty2 < tMax) tMax = ty2;
            if (tMin > tMax) return false;
        }

        if (Math.Abs(dir.Z) < eps)
        {
            if (origin.Z < minZ || origin.Z > maxZ) return false;
        }
        else
        {
            double tz1 = (minZ - origin.Z) / dir.Z;
            double tz2 = (maxZ - origin.Z) / dir.Z;
            BlockFacing fz = dir.Z > 0 ? BlockFacing.NORTH : BlockFacing.SOUTH;
            if (tz1 > tz2) (tz1, tz2) = (tz2, tz1);
            if (tz1 > tMin) { tMin = tz1; enterFace = fz; }
            if (tz2 < tMax) tMax = tz2;
            if (tMin > tMax) return false;
        }

        if (tMax < 0) return false;

        face = enterFace;
        return true;
    }
}
