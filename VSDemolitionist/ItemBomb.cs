using System;
using System.Text;
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

    private float GetEffectiveBombFloat(ItemStack? stack, string key, float defaultValue)
    {
        string? bombCode = stack?.Collectible?.Code?.Path;
        BombBalanceConfig? overrideConfig = VSDemolitionistModSystem.GetBombOverride(bombCode);

        float? overrideValue = key switch
        {
            "fuseSeconds" => overrideConfig?.FuseSeconds,
            "blastRadius" => overrideConfig?.BlastRadius,
            "rockBlastRadius" => overrideConfig?.RockBlastRadius,
            "oreBlastRadius" => overrideConfig?.OreBlastRadius,
            "entityBlastRadius" => overrideConfig?.EntityBlastRadius,
            "oreDestroyChance" => overrideConfig?.OreDestroyChance,
            "crystalDestroyChance" => overrideConfig?.CrystalDestroyChance,
            "blastDropoff" => overrideConfig?.BlastDropoff,
            "blastPowerLoss" => overrideConfig?.BlastPowerLoss,
            "blastVerticalRadius" => overrideConfig?.BlastVerticalRadius,
            "entityDamage" => overrideConfig?.EntityDamage,
            "fisherSurfaceDestroyChance" => overrideConfig?.FisherSurfaceDestroyChance,
            "inwardWidth" => overrideConfig?.InwardWidth,
            "inwardDepth" => overrideConfig?.InwardDepth,
            "outwardDepth" => overrideConfig?.OutwardDepth,
            "inwardLinkRange" => overrideConfig?.InwardLinkRange,
            _ => null
        };

        return overrideValue ?? GetBombFloat(stack, key, defaultValue);
    }

    private static string FormatPercent(float value)
    {
        return $"{Math.Round(value * 100f, 1):0.#}%";
    }

    private string BuildBombDescription(ItemStack stack)
    {
        string bombCode = stack.Collectible?.Code?.Path ?? "";
        string defaultShape = GetBombString(stack, "blastShape", "sphere");

        if (bombCode == "bomb-fisherman")
        {
            return "This stick is designed for blasting in water. It creates a splash burst and is best used against fish and other nearby creatures.";
        }

        if (bombCode == "blasting-charge")
        {
            float inwardDepth = GetEffectiveBombFloat(stack, "inwardDepth", 5f);
            float outwardDepth = GetEffectiveBombFloat(stack, "outwardDepth", 1f);
            float inwardWidth = GetEffectiveBombFloat(stack, "inwardWidth", 3f);
            float linkRange = GetEffectiveBombFloat(stack, "inwardLinkRange", 7f);

            return $"This placed charge creates a directional blast about {Math.Round(inwardWidth):0} blocks wide, {Math.Round(inwardDepth):0} blocks inward, and {Math.Round(outwardDepth):0} block outward. Best used for controlled excavation and unbroken block extraction. Linked charges can bridge gaps up to {Math.Round(linkRange):0} blocks.";
        }

        if (bombCode == "landmine")
        {
            return "This mine is placed inert, then armed with an ignition source. Once armed, it detonates when a creature or player steps over it.";
        }

        if (bombCode.Contains("bundle"))
        {
            return defaultShape switch
            {
                "disc" => "This bundle creates a wide, shallow blast and is best used to clear broad ore veins and large excavation faces.",
                _ => "This bundle creates a large blast and is best used for heavy excavation work."
            };
        }

        return defaultShape switch
        {
            "disc" => "This dynamite creates a wide blast and is best used to clear broad sections of terrain.",
            _ => "This dynamite creates a compact spherical blast and is best used for general ore and crystal extraction."
        };
    }

    private static AssetLocation ResolveModAsset(string code)
    {
        if (code.Contains(":")) return new AssetLocation(code);
        return new AssetLocation("vsdemolitionist", code);
    }

    private static string GetGuiIconCode(string iconCode)
    {
        if (VSDemolitionistModSystem.Use3DIcons())
        {
            return iconCode;
        }

        if (iconCode.StartsWith("bombicon-", StringComparison.Ordinal))
        {
            return iconCode.Replace("bombicon-", "bombicon2d-", StringComparison.Ordinal);
        }

        return iconCode;
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);

        if (target != EnumItemRenderTarget.Gui && target != EnumItemRenderTarget.Ground)
        {
            return;
        }

        if (target == EnumItemRenderTarget.Ground && !VSDemolitionistModSystem.Use3DIcons())
        {
            return;
        }

        if (target == EnumItemRenderTarget.Gui && VSDemolitionistModSystem.Use3DIcons() && GetBombBool(itemstack, "useShapeIcon", false))
        {
            return;
        }

        string iconCode = GetBombString(itemstack, "iconItemCode", "bombicon");
        Item? iconItem = capi.World.GetItem(new AssetLocation("vsdemolitionist", GetGuiIconCode(iconCode)));
        if (iconItem == null)
        {
            return;
        }

        guiIconSlot ??= new DummySlot();
        guiIconSlot.Itemstack = new ItemStack(iconItem, 1);

        ItemRenderInfo iconRender = capi.Render.GetItemStackRenderInfo(guiIconSlot, target, 0);
        renderinfo = iconRender;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        ItemStack? stack = inSlot?.Itemstack;
        if (stack == null) return;

        float oreDestroyChance = GetEffectiveBombFloat(stack, "oreDestroyChance", 0.5f);
        float crystalDestroyChance = GetEffectiveBombFloat(stack, "crystalDestroyChance", 0.8f);
        string bombCode = stack.Collectible?.Code?.Path ?? "";

        dsc.AppendLine();
        dsc.AppendLine(BuildBombDescription(stack));

        if (bombCode != "blasting-charge" && bombCode != "landmine")
        {
            dsc.AppendLine($"{Lang.Get("Ore destruction chance")}: {FormatPercent(oreDestroyChance)}");
            dsc.AppendLine($"{Lang.Get("Crystal destruction chance")}: {FormatPercent(crystalDestroyChance)}");
        }
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

        float fuseSeconds = GetEffectiveBombFloat(slot.Itemstack, "fuseSeconds", DefaultFuseSeconds);
        float lightTime = GetBombFloat(slot.Itemstack, "lightTime", LightTime);
        float maxHoldSeconds = lightTime + fuseSeconds;
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
                StartHeldFuseSound(byEntity.World.Api as ICoreClientAPI);
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
        float fuseSeconds = GetEffectiveBombFloat(slot.Itemstack, "fuseSeconds", DefaultFuseSeconds);
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

    private void StartHeldFuseSound(ICoreClientAPI? capi)
    {
        if (capi == null) return;

        float effectiveVolume = VSDemolitionistModSystem.GetFuseVolume();

        if (heldFusePlaying)
        {
            heldFuseSound?.SetVolume(effectiveVolume);
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
            Volume = effectiveVolume
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

        float fuseSeconds = GetEffectiveBombFloat(slot.Itemstack, "fuseSeconds", DefaultFuseSeconds);
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

        if (GetBombString(slot.Itemstack, "tier", "") == "landmine")
        {
            Vec3d targetCenter = targetBlockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5);
            if (!IsWithinInteractionRange(byEntity, targetCenter, VSDemolitionistModSystem.GetLandmineInteractRange()))
            {
                SendChargePlacementMessage(byEntity, "Too far away to place landmine.");
                return;
            }
        }

        ICoreServerAPI sapi = (ICoreServerAPI)byEntity.World.Api;
        string entityCode = GetBombString(slot.Itemstack, "placedEntityCode", GetBombString(slot.Itemstack, "entityCode", "bomb"));
        EntityProperties type = sapi.World.GetEntityType(ResolveModAsset(entityCode));
        if (type == null) return;

        bool placeTopOnly = GetBombBool(slot.Itemstack, "placeTopOnly", false);
        BlockFacing face = placeTopOnly ? BlockFacing.UP : ResolveAttachFace(targetBlockSel, byEntity);
        face ??= BlockFacing.UP;
        if (face == BlockFacing.DOWN)
        {
            SendChargePlacementMessage(byEntity, "Cannot place charge on underside.");
            return;
        }

        if (!TryResolveChargeAttachPos(byEntity, targetBlockSel.Position, face, out BlockPos attachPos))
        {
            string tier = GetBombString(slot.Itemstack, "tier", "");
            string message = tier == "landmine"
                ? "Landmine must be placed on the top of a solid ore, stone, soil, or crystal block."
                : "Charge must be placed on a solid ore, stone, soil, or crystal block.";
            SendChargePlacementMessage(byEntity, message);
            return;
        }

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
            bomb.AttachToBlock(attachPos, face);
        }

        slot.TakeOut(1);
        slot.MarkDirty();
    }

    private static bool TryResolveChargeAttachPos(EntityAgent byEntity, BlockPos selectionPos, BlockFacing face, out BlockPos attachPos)
    {
        attachPos = selectionPos.Copy();
        IBlockAccessor blockAccessor = byEntity.World.BlockAccessor;
        Block targetBlock = blockAccessor.GetBlock(attachPos);

        // If the selected block is replaceable/non-solid, mount to the support block behind the hit face.
        if (!IsValidChargeSupportBlock(blockAccessor, attachPos, targetBlock, face))
        {
            attachPos = attachPos.AddCopy(face.Opposite);
            targetBlock = blockAccessor.GetBlock(attachPos);
        }

        return IsValidChargeSupportBlock(blockAccessor, attachPos, targetBlock, face);
    }

    private static bool IsValidChargeSupportBlock(IBlockAccessor blockAccessor, BlockPos pos, Block? block, BlockFacing face)
    {
        if (block == null || block.BlockId == 0) return false;
        if (block.Replaceable >= 6000) return false;
        if (!block.SideSolid.OnSide(face)) return false;
        if (!IsFullBlockCollision(block, blockAccessor, pos)) return false;
        if (block.BlockMaterial == EnumBlockMaterial.Wood) return false;

        string path = block.Code?.Path ?? string.Empty;
        if (path.Length == 0) return false;

        // Prevent placement on vegetation or tree-like supports even if they expose collision.
        if (path.Contains("grass", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("flower", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("crop", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("plant", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("reed", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("vine", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("mushroom", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("sapling", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("leaves", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("bush", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("berry", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("log", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("trunk", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("branch", StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }

    private static bool IsFullBlockCollision(Block block, IBlockAccessor blockAccessor, BlockPos pos)
    {
        Cuboidf[]? boxes = block.GetCollisionBoxes(blockAccessor, pos);
        if (boxes == null || boxes.Length != 1) return false;

        Cuboidf box = boxes[0];
        const float epsilon = 0.001f;
        return box.X1 <= epsilon
            && box.Y1 <= epsilon
            && box.Z1 <= epsilon
            && box.X2 >= 1f - epsilon
            && box.Y2 >= 1f - epsilon
            && box.Z2 >= 1f - epsilon;
    }

    private static void SendChargePlacementMessage(EntityAgent byEntity, string message)
    {
        if (byEntity is EntityPlayer entityPlayer && entityPlayer.Player is IServerPlayer serverPlayer)
        {
            serverPlayer.SendMessage(
                GlobalConstants.GeneralChatGroup,
                message,
                EnumChatType.Notification
            );
        }
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

    private static bool IsWithinInteractionRange(EntityAgent byEntity, Vec3d targetPos, double maxDistance)
    {
        double dx = byEntity.ServerPos.X - targetPos.X;
        double dz = byEntity.ServerPos.Z - targetPos.Z;
        return (dx * dx + dz * dz) <= maxDistance * maxDistance;
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
