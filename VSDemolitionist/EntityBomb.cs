using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using System.Collections.Generic;

namespace VSDemolitionist;

public class EntityBomb : Entity
{
    private readonly struct ResourceDestructionStats
    {
        public readonly int OreTotal;
        public readonly int OreDestroyed;
        public readonly int OrePreserved;
        public readonly int CrystalTotal;
        public readonly int CrystalDestroyed;
        public readonly int CrystalPreserved;

        public ResourceDestructionStats(
            int oreTotal,
            int oreDestroyed,
            int orePreserved,
            int crystalTotal,
            int crystalDestroyed,
            int crystalPreserved)
        {
            OreTotal = oreTotal;
            OreDestroyed = oreDestroyed;
            OrePreserved = orePreserved;
            CrystalTotal = crystalTotal;
            CrystalDestroyed = crystalDestroyed;
            CrystalPreserved = crystalPreserved;
        }
    }

    private readonly struct TrackedSolidBlock
    {
        public readonly BlockPos Pos;
        public readonly int BlockId;

        public TrackedSolidBlock(BlockPos pos, int blockId)
        {
            Pos = pos;
            BlockId = blockId;
        }
    }

    private readonly struct TrackedResourceBlock
    {
        public readonly BlockPos Pos;
        public readonly int BlockId;
        public readonly bool IsCrystal;

        public TrackedResourceBlock(BlockPos pos, int blockId, bool isCrystal)
        {
            Pos = pos;
            BlockId = blockId;
            IsCrystal = isCrystal;
        }
    }

    private const float DefaultFuseSeconds = 4f;
    private const float DefaultThrowForwardStrength = 0.40f;
    private const float DefaultThrowUpwardBoost = 0.10f;
    private const float DefaultThrownFuseVolume = 1.8f;
    private const float DefaultBlastRadius = 4f;
    private const float DefaultBlastDropoff = 1f;
    private const float DefaultBlastPowerLoss = 0f;
    private const string DefaultBlastShape = "sphere";
    private const int MaxManualBlockChanges = 10000;
    private const string BombAttrRoot = "bomb";
    private const string DefaultStaticStickyEntityCode = "bombstuck";

    private const string AttrLit = "vsd_lit";
    private const string AttrFuseEndMs = "vsd_fuseEndMs";
    private const string AttrFuseRemainingSeconds = "vsd_fuseRemainingSeconds";
    private const string AttrFuseSeconds = "vsd_fuseSeconds";
    private const string AttrThrowForwardStrength = "vsd_throwForwardStrength";
    private const string AttrThrowUpwardBoost = "vsd_throwUpwardBoost";
    private const string AttrThrownFuseVolume = "vsd_thrownFuseVolume";
    private const string AttrRockBlastRadius = "vsd_rockBlastRadius";
    private const string AttrOreBlastRadius = "vsd_oreBlastRadius";
    private const string AttrEntityBlastRadius = "vsd_entityBlastRadius";
    private const string AttrBlastDropoff = "vsd_blastDropoff";
    private const string AttrBlastPowerLoss = "vsd_blastPowerLoss";
    private const string AttrBlastShape = "vsd_blastShape";
    private const string AttrBlastVerticalRadius = "vsd_blastVerticalRadius";
    private const string AttrBlockBlastType = "vsd_blockBlastType";
    private const string AttrBombTier = "vsd_bombTier";
    private const string AttrIsSticky = "vsd_isSticky";
    private const string AttrOreDestroyChance = "vsd_oreDestroyChance";
    private const string AttrCrystalDestroyChance = "vsd_crystalDestroyChance";
    private const string AttrThrowerPlayerUid = "vsd_throwerPlayerUid";

    private static readonly HashSet<string> DebugBlastReportUids = new();

    private long holderId = -1;

    private ILoadedSound? fuseSound;
    private bool soundStarted = false;
    private bool airborneClient = false;
    private bool impactPlayedClient = false;
    private bool airborneServer = false;
    private bool impactPlayedServer = false;
    private float sparkAccum;
    private float smokeAccum;
    private double lastMotionX;
    private double lastMotionY;
    private double lastMotionZ;
    private bool attachedToBlock;
    private bool attachedToEntity;
    private readonly BlockPos attachedBlockPos = new(0);
    private BlockFacing attachedFace = BlockFacing.UP;
    private long attachedEntityId = -1;
    private Vec3d attachedEntityOffset = new();
    private bool hasPreferredAttach;
    private readonly BlockPos preferredAttachPos = new(0);
    private BlockFacing preferredAttachFace = BlockFacing.UP;

    private float GetConfigFloat(string key, float defaultValue)
    {
        return WatchedAttributes.GetFloat(key, defaultValue);
    }

    private bool GetConfigBool(string key, bool defaultValue)
    {
        return WatchedAttributes.GetBool(key, defaultValue);
    }

    private static float GetBombFloat(ItemStack? stack, string key, float defaultValue)
    {
        return stack?.Collectible?.Attributes?[BombAttrRoot]?[key].AsFloat(defaultValue) ?? defaultValue;
    }

    private static string GetBombString(ItemStack? stack, string key, string defaultValue)
    {
        return stack?.Collectible?.Attributes?[BombAttrRoot]?[key].AsString(defaultValue) ?? defaultValue;
    }

    private static float Clamp01(float value)
    {
        if (value < 0f) return 0f;
        if (value > 1f) return 1f;
        return value;
    }

    private static EnumBlastType ParseBlockBlastType(string blastType)
    {
        return blastType?.Trim().ToLowerInvariant() switch
        {
            "ore" or "oreblast" => EnumBlastType.OreBlast,
            _ => EnumBlastType.RockBlast
        };
    }

    private static bool IsOreBlock(Block block)
    {
        string path = block?.Code?.Path ?? "";
        if (path.Length == 0) return false;
        return path.Contains("ore");
    }

    private static bool IsCrystalBlock(Block block)
    {
        string path = block?.Code?.Path ?? "";
        if (path.Length == 0) return false;
        return path.Contains("crystal") || path.Contains("quartz");
    }

    public void ApplyConfigFromItemstack(ItemStack? stack)
    {
        if (World.Side != EnumAppSide.Server || stack == null) return;

        float blastRadius = GetBombFloat(stack, "blastRadius", DefaultBlastRadius);

        WatchedAttributes.SetFloat(AttrFuseSeconds, GetBombFloat(stack, "fuseSeconds", DefaultFuseSeconds));
        WatchedAttributes.SetFloat(AttrThrowForwardStrength, GetBombFloat(stack, "throwForwardStrength", DefaultThrowForwardStrength));
        WatchedAttributes.SetFloat(AttrThrowUpwardBoost, GetBombFloat(stack, "throwUpwardBoost", DefaultThrowUpwardBoost));
        WatchedAttributes.SetFloat(AttrThrownFuseVolume, GetBombFloat(stack, "thrownFuseVolume", DefaultThrownFuseVolume));
        WatchedAttributes.SetFloat(AttrRockBlastRadius, GetBombFloat(stack, "rockBlastRadius", blastRadius));
        WatchedAttributes.SetFloat(AttrOreBlastRadius, GetBombFloat(stack, "oreBlastRadius", blastRadius));
        WatchedAttributes.SetFloat(AttrEntityBlastRadius, GetBombFloat(stack, "entityBlastRadius", blastRadius));
        WatchedAttributes.SetFloat(AttrBlastDropoff, GetBombFloat(stack, "blastDropoff", DefaultBlastDropoff));
        WatchedAttributes.SetFloat(AttrBlastPowerLoss, GetBombFloat(stack, "blastPowerLoss", DefaultBlastPowerLoss));
        WatchedAttributes.SetString(AttrBlastShape, GetBombString(stack, "blastShape", DefaultBlastShape));
        WatchedAttributes.SetFloat(AttrBlastVerticalRadius, GetBombFloat(stack, "blastVerticalRadius", blastRadius));
        WatchedAttributes.SetFloat(AttrOreDestroyChance, Clamp01(GetBombFloat(stack, "oreDestroyChance", 1f)));
        WatchedAttributes.SetFloat(AttrCrystalDestroyChance, Clamp01(GetBombFloat(stack, "crystalDestroyChance", 1f)));
        WatchedAttributes.SetInt(AttrBlockBlastType, (int)ParseBlockBlastType(GetBombString(stack, "blastType", "rock")));
        WatchedAttributes.SetString(AttrBombTier, GetBombString(stack, "tier", stack.Collectible?.Code?.Path ?? "unknown"));
        WatchedAttributes.SetBool(AttrIsSticky, stack.Collectible?.Attributes?[BombAttrRoot]?["isSticky"].AsBool(false) ?? false);
    }

    public void StartFuse()
    {
        StartFuseWithRemainingSeconds(GetConfigFloat(AttrFuseSeconds, DefaultFuseSeconds));
    }

    public void StartFuseWithRemainingSeconds(float remainingSeconds)
    {
        if (World.Side != EnumAppSide.Server) return;

        float clampedRemaining = Math.Max(0f, remainingSeconds);
        long endMs = World.ElapsedMilliseconds + (long)(clampedRemaining * 1000);

        WatchedAttributes.SetBool(AttrLit, true);
        WatchedAttributes.SetFloat(AttrFuseRemainingSeconds, clampedRemaining);
        // Kept for compatibility with existing entities from older saves.
        WatchedAttributes.SetLong(AttrFuseEndMs, endMs);
    }

    public void SetHeldBy(EntityAgent holder)
    {
        holderId = holder.EntityId;
        TeleportToHolder(holder);
    }

    public void SetThrower(EntityAgent thrower)
    {
        if (World.Side != EnumAppSide.Server) return;
        if (thrower is EntityPlayer player)
        {
            WatchedAttributes.SetString(AttrThrowerPlayerUid, player.PlayerUID);
        }
    }

public void Release(EntityAgent holder)
{
    holderId = -1;
    attachedToBlock = false;
    attachedToEntity = false;
    attachedEntityId = -1;
    hasPreferredAttach = false;

    Vec3f dir = holder.SidedPos.GetViewVector().Normalize();

    double forwardStrength = GetConfigFloat(AttrThrowForwardStrength, DefaultThrowForwardStrength);
    double upwardBoost = GetConfigFloat(AttrThrowUpwardBoost, DefaultThrowUpwardBoost);

    // Apply rotation so model faces forward consistently
    ServerPos.Yaw = holder.SidedPos.Yaw;
    Pos.Yaw = ServerPos.Yaw;

    ServerPos.Motion.Set(
        dir.X * forwardStrength,
        dir.Y * forwardStrength + upwardBoost,
        dir.Z * forwardStrength
    );

    Pos.Motion.Set(
        ServerPos.Motion.X,
        ServerPos.Motion.Y,
        ServerPos.Motion.Z
    );
}

    public void SetPreferredAttach(BlockPos pos, BlockFacing face)
    {
        if (pos == null) return;
        hasPreferredAttach = true;
        preferredAttachPos.Set(pos);
        preferredAttachFace = face ?? BlockFacing.UP;
    }

    public void AttachToBlock(BlockPos pos, BlockFacing face)
    {
        if (World.Side != EnumAppSide.Server) return;

        attachedToEntity = false;
        attachedEntityId = -1;
        attachedToBlock = true;
        attachedBlockPos.Set(pos);
        attachedFace = face ?? BlockFacing.UP;
        hasPreferredAttach = false;
        ServerPos.Motion.Set(0, 0, 0);
        Pos.Motion.Set(0, 0, 0);
        lastMotionX = 0;
        lastMotionY = 0;
        lastMotionZ = 0;
        SnapToAttachedFace();
    }

    private bool IsStaticStickyEntity()
    {
        string path = Properties?.Code?.Path ?? "";
        return path == DefaultStaticStickyEntityCode || path.StartsWith("bombstuck-");
    }

    private void CopyFuseAndConfigTo(EntityBomb other)
    {
        other.WatchedAttributes.SetBool(AttrLit, WatchedAttributes.GetBool(AttrLit));
        other.WatchedAttributes.SetFloat(AttrFuseRemainingSeconds, WatchedAttributes.GetFloat(AttrFuseRemainingSeconds, GetConfigFloat(AttrFuseSeconds, DefaultFuseSeconds)));
        other.WatchedAttributes.SetLong(AttrFuseEndMs, WatchedAttributes.GetLong(AttrFuseEndMs, 0));
        other.WatchedAttributes.SetFloat(AttrFuseSeconds, GetConfigFloat(AttrFuseSeconds, DefaultFuseSeconds));
        other.WatchedAttributes.SetFloat(AttrThrowForwardStrength, GetConfigFloat(AttrThrowForwardStrength, DefaultThrowForwardStrength));
        other.WatchedAttributes.SetFloat(AttrThrowUpwardBoost, GetConfigFloat(AttrThrowUpwardBoost, DefaultThrowUpwardBoost));
        other.WatchedAttributes.SetFloat(AttrThrownFuseVolume, GetConfigFloat(AttrThrownFuseVolume, DefaultThrownFuseVolume));
        other.WatchedAttributes.SetFloat(AttrRockBlastRadius, GetConfigFloat(AttrRockBlastRadius, DefaultBlastRadius));
        other.WatchedAttributes.SetFloat(AttrOreBlastRadius, GetConfigFloat(AttrOreBlastRadius, DefaultBlastRadius));
        other.WatchedAttributes.SetFloat(AttrEntityBlastRadius, GetConfigFloat(AttrEntityBlastRadius, DefaultBlastRadius));
        other.WatchedAttributes.SetFloat(AttrBlastDropoff, GetConfigFloat(AttrBlastDropoff, DefaultBlastDropoff));
        other.WatchedAttributes.SetFloat(AttrBlastPowerLoss, GetConfigFloat(AttrBlastPowerLoss, DefaultBlastPowerLoss));
        other.WatchedAttributes.SetString(AttrBlastShape, WatchedAttributes.GetString(AttrBlastShape, DefaultBlastShape));
        other.WatchedAttributes.SetFloat(AttrBlastVerticalRadius, GetConfigFloat(AttrBlastVerticalRadius, DefaultBlastRadius));
        other.WatchedAttributes.SetInt(AttrBlockBlastType, WatchedAttributes.GetInt(AttrBlockBlastType, (int)EnumBlastType.RockBlast));
        other.WatchedAttributes.SetString(AttrBombTier, WatchedAttributes.GetString(AttrBombTier, "unknown"));
        other.WatchedAttributes.SetBool(AttrIsSticky, GetConfigBool(AttrIsSticky, false));
        other.WatchedAttributes.SetFloat(AttrOreDestroyChance, GetConfigFloat(AttrOreDestroyChance, 1f));
        other.WatchedAttributes.SetFloat(AttrCrystalDestroyChance, GetConfigFloat(AttrCrystalDestroyChance, 1f));
        other.WatchedAttributes.SetString(AttrThrowerPlayerUid, WatchedAttributes.GetString(AttrThrowerPlayerUid));
    }

    private bool TrySwitchToStaticSticky(BlockPos pos, BlockFacing face)
    {
        if (World.Side != EnumAppSide.Server) return false;
        if (IsStaticStickyEntity()) return false;

        string currentCode = Properties?.Code?.Path ?? "";
        string staticCode = DefaultStaticStickyEntityCode;
        if (currentCode.StartsWith("bomb-sticky-"))
        {
            staticCode = "bombstuck-sticky-" + currentCode["bomb-sticky-".Length..];
        }

        EntityProperties staticType = World.GetEntityType(new AssetLocation("vsdemolitionist", staticCode));
        if (staticType == null) return false;

        Entity entity = World.ClassRegistry.CreateEntity(staticType);
        if (entity is not EntityBomb stuckBomb) return false;

        stuckBomb.ServerPos.SetPos(ServerPos.X, ServerPos.Y, ServerPos.Z);
        stuckBomb.ServerPos.Motion.Set(0, 0, 0);
        stuckBomb.Pos.SetPos(Pos.X, Pos.Y, Pos.Z);
        stuckBomb.Pos.Motion.Set(0, 0, 0);
        stuckBomb.ServerPos.Yaw = ServerPos.Yaw;
        stuckBomb.Pos.Yaw = Pos.Yaw;
        stuckBomb.ServerPos.Pitch = ServerPos.Pitch;
        stuckBomb.Pos.Pitch = Pos.Pitch;

        CopyFuseAndConfigTo(stuckBomb);

        World.SpawnEntity(stuckBomb);

        stuckBomb.AttachToBlock(pos, face);
        Die(EnumDespawnReason.Removed);
        return true;
    }

    private void TeleportToHolder(EntityAgent holder)
    {
        Vec3f dir = holder.SidedPos.GetViewVector().Normalize();
        double forwardOffset = 0.25;
        double verticalOffset = holder.LocalEyePos.Y - 0.45;

        double x = holder.ServerPos.X + dir.X * forwardOffset;
        double y = holder.ServerPos.Y + verticalOffset;
        double z = holder.ServerPos.Z + dir.Z * forwardOffset;

        ServerPos.SetPos(x, y, z);
        ServerPos.Motion.Set(0, 0, 0);

        Pos.SetPos(x, y, z);
        Pos.Motion.Set(0, 0, 0);
    }

    public override void OnGameTick(float dt)
    {
        // If already attached, zero out velocity before physics update to reduce micro-bounce/pulsing.
        if (attachedToBlock)
        {
            ServerPos.Motion.Set(0, 0, 0);
            Pos.Motion.Set(0, 0, 0);
        }
        
        base.OnGameTick(dt);

        // SERVER: stay attached while held
        if (World.Side == EnumAppSide.Server && holderId != -1)
        {
            Entity e = World.GetEntityById(holderId);
            if (e is EntityAgent holder)
            {
                TeleportToHolder(holder);
            }
        }

        if (World.Side == EnumAppSide.Server)
        {
            if (holderId == -1 && GetConfigBool(AttrIsSticky, false) && WatchedAttributes.GetBool(AttrLit))
            {
                if (attachedToEntity)
                {
                    if (!UpdateAttachedToEntity()) attachedToEntity = false;
                }

                if (attachedToBlock)
                {
                    if (!UpdateAttachedToBlock())
                    {
                        attachedToBlock = false;
                    }
                }
            }

            lastMotionX = ServerPos.Motion.X;
            lastMotionY = ServerPos.Motion.Y;
            lastMotionZ = ServerPos.Motion.Z;
        }

        // Final lock pass after tick to prevent interpolation drift while attached.
        if (attachedToBlock)
        {
            SnapToAttachedFace();
        }

        // SERVER: explode when fuse ends
        if (World.Side == EnumAppSide.Server && WatchedAttributes.GetBool(AttrLit))
        {
            if (!impactPlayedServer && holderId == -1)
            {
                double speedSqServer = ServerPos.Motion.X * ServerPos.Motion.X + ServerPos.Motion.Y * ServerPos.Motion.Y + ServerPos.Motion.Z * ServerPos.Motion.Z;

                if (!airborneServer && speedSqServer > 0.02 * 0.02)
                {
                    airborneServer = true;
                }
                else if (airborneServer && speedSqServer < 0.004 * 0.004)
                {
                    impactPlayedServer = true;
                    World.PlaySoundAt(
                        new AssetLocation("survival:arrow-impact"),
                        ServerPos.X, ServerPos.Y, ServerPos.Z,
                        null,
                        false,
                        24f,
                        1.0f
                    );
                }
            }

            // Persist-safe fuse countdown:
            // - primary source is remaining seconds in watched attrs
            // - fallback migrates once from legacy fuseEndMs
            float remainingFuse = WatchedAttributes.GetFloat(AttrFuseRemainingSeconds, -1f);
            if (remainingFuse < 0f)
            {
                long fuseEnd = WatchedAttributes.GetLong(AttrFuseEndMs, 0);
                if (fuseEnd > 0)
                {
                    remainingFuse = (float)Math.Max(0, (fuseEnd - World.ElapsedMilliseconds) / 1000.0);
                }
                else
                {
                    remainingFuse = GetConfigFloat(AttrFuseSeconds, DefaultFuseSeconds);
                }
            }

            remainingFuse = Math.Max(0f, remainingFuse - dt);
            WatchedAttributes.SetFloat(AttrFuseRemainingSeconds, remainingFuse);

            if (remainingFuse <= 0f)
            {
                Explode();
                return;
            }
        }

        // CLIENT: handle thrown fuse sound/visuals/impacts
        if (World.Side == EnumAppSide.Client)
        {
            ICoreClientAPI? capi = Api as ICoreClientAPI;
            if (capi == null) return;
            bool isLit = WatchedAttributes.GetBool(AttrLit);

            if (isLit && holderId == -1 && !soundStarted)
            {
                soundStarted = true;

                fuseSound = capi.World.LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("vsdemolitionist", "sounds/fuse"),
                    ShouldLoop = true,
                    DisposeOnFinish = false,
                    RelativePosition = false,
                    Range = 32f,
                    Volume = GetConfigFloat(AttrThrownFuseVolume, DefaultThrownFuseVolume),
                    Position = Pos.XYZ.ToVec3f()
                });

                fuseSound?.Start();
            }

            if (isLit && holderId == -1 && fuseSound != null)
            {
                fuseSound.SetPosition(Pos.XYZ.ToVec3f());
                fuseSound.SetVolume(GetConfigFloat(AttrThrownFuseVolume, DefaultThrownFuseVolume));
            }
            else if (fuseSound != null)
            {
                // Defensive cleanup to avoid lingering loop if state changes while entity remains loaded.
                fuseSound.Stop();
                fuseSound.Dispose();
                fuseSound = null;
                soundStarted = false;
            }

            double speedSqClient = Pos.Motion.X * Pos.Motion.X + Pos.Motion.Y * Pos.Motion.Y + Pos.Motion.Z * Pos.Motion.Z;

            if (isLit && holderId == -1)
            {
                SpawnFuseSparks(capi, dt);
            }

            // CLIENT: play one-shot impact when a thrown bomb first contacts a surface.
            if (isLit && !impactPlayedClient && holderId == -1)
            {
                if (!airborneClient && speedSqClient > 0.02 * 0.02)
                {
                    airborneClient = true;
                }
                else if (airborneClient && speedSqClient < 0.004 * 0.004)
                {
                    impactPlayedClient = true;
                    PlayClientOneShot(capi, "survival:arrow-impact", Pos.XYZ.ToVec3f(), 0.95f);
                }
            }
        }
    }

    public override void OnCollided()
    {
        base.OnCollided();

        if (World.Side != EnumAppSide.Server) return;
        if (holderId != -1) return;
        if (!WatchedAttributes.GetBool(AttrLit)) return;
        if (!GetConfigBool(AttrIsSticky, false)) return;
        if (attachedToBlock || attachedToEntity) return;

        // Hard-stop bounce on first sticky contact.
        ServerPos.Motion.Set(0, 0, 0);
        Pos.Motion.Set(0, 0, 0);
        lastMotionX = 0;
        lastMotionY = 0;
        lastMotionZ = 0;

        if (hasPreferredAttach)
        {
            Block targetBlock = World.BlockAccessor.GetBlock(preferredAttachPos);
            if (targetBlock != null && targetBlock.BlockId != 0 && targetBlock.Replaceable < 6000)
            {
                if (!TrySwitchToStaticSticky(preferredAttachPos, preferredAttachFace))
                {
                    AttachToBlock(preferredAttachPos, preferredAttachFace);
                }
                return;
            }

            hasPreferredAttach = false;
        }

        if (!TryAttachToEntity())
        {
            AttachToSurface();
        }
    }

    private void Explode()
    {
        if (World.Side != EnumAppSide.Server) return;

        ICoreServerAPI sapi = (ICoreServerAPI)World.Api;

        float rockRadius = GetConfigFloat(AttrRockBlastRadius, DefaultBlastRadius);
        float oreRadius = GetConfigFloat(AttrOreBlastRadius, DefaultBlastRadius);
        float entityRadius = GetConfigFloat(AttrEntityBlastRadius, DefaultBlastRadius);
        float powerLoss = GetConfigFloat(AttrBlastPowerLoss, DefaultBlastPowerLoss);
        float verticalRadius = Math.Max(0.1f, GetConfigFloat(AttrBlastVerticalRadius, rockRadius));
        string blastShape = WatchedAttributes.GetString(AttrBlastShape, DefaultBlastShape)?.Trim().ToLowerInvariant() ?? DefaultBlastShape;
        bool isDiscBlast = blastShape == "disc" || blastShape == "flat";
        EnumBlastType blockBlastType = (EnumBlastType)WatchedAttributes.GetInt(AttrBlockBlastType, (int)EnumBlastType.RockBlast);
        string ignitedByPlayerUid = WatchedAttributes.GetString(AttrThrowerPlayerUid);
        IServerPlayer? throwerPlayer = null;
        if (!string.IsNullOrWhiteSpace(ignitedByPlayerUid))
        {
            throwerPlayer = sapi.World.PlayerByUid(ignitedByPlayerUid) as IServerPlayer;
        }
        bool debugEnabled = throwerPlayer != null && IsDebugBlastReportEnabled(throwerPlayer.PlayerUID);
        List<TrackedSolidBlock>? preSnapshot = null;
        if (debugEnabled)
        {
            preSnapshot = isDiscBlast
                ? CaptureSolidBlocksEllipsoid(rockRadius, verticalRadius)
                : CaptureSolidBlocksSphere(rockRadius);
        }

        List<TrackedResourceBlock>? trackedResources = null;
        if (oreRadius > 0f)
        {
            trackedResources = isDiscBlast
                ? CollectResourceBlocksEllipsoid(oreRadius, verticalRadius)
                : CollectResourceBlocksSphere(oreRadius);
        }

        if (isDiscBlast && rockRadius > 0f)
        {
            ClearNonResourceBlocksEllipsoid(rockRadius, verticalRadius);

            // Keep one explosion event for damage/sound, but no spherical terrain crater.
            sapi.World.CreateExplosion(
                Pos.AsBlockPos,
                EnumBlastType.EntityBlast,
                0,
                entityRadius,
                powerLoss,
                string.IsNullOrWhiteSpace(ignitedByPlayerUid) ? null : ignitedByPlayerUid
            );
        }
        else if (rockRadius > 0f)
        {
            sapi.World.CreateExplosion(
                Pos.AsBlockPos,
                blockBlastType,
                rockRadius,
                entityRadius,
                powerLoss,
                string.IsNullOrWhiteSpace(ignitedByPlayerUid) ? null : ignitedByPlayerUid
            );
        }

        ResourceDestructionStats? stats = null;
        if (trackedResources != null && trackedResources.Count > 0)
        {
            stats = ApplyResourceDestruction(
                trackedResources,
                throwerPlayer,
                Clamp01(GetConfigFloat(AttrOreDestroyChance, 1f)),
                Clamp01(GetConfigFloat(AttrCrystalDestroyChance, 1f))
            );
        }

        if (throwerPlayer != null && debugEnabled)
        {
            int destroyedBlocks = preSnapshot == null ? -1 : CountDestroyedBlocks(preSnapshot);
            SendBlastReport(throwerPlayer, blockBlastType, rockRadius, oreRadius, entityRadius, stats, destroyedBlocks);
            LogBlastReport(throwerPlayer, blockBlastType, rockRadius, oreRadius, entityRadius, stats, destroyedBlocks);
        }

        Die(EnumDespawnReason.Removed);
    }

    private bool TryAttachToEntity()
    {
        if (attachedToEntity || attachedToBlock) return false;

        Entity? candidate = World.GetNearestEntity(Pos.XYZ, 0.65f, 0.65f, (entity) =>
        {
            if (entity == null) return false;
            if (entity.EntityId == EntityId) return false;
            if (entity.EntityId == holderId) return false;
            if (entity is EntityBomb) return false;
            return true;
        });

        if (candidate == null) return false;

        attachedToEntity = true;
        attachedEntityId = candidate.EntityId;
        attachedEntityOffset.Set(
            Pos.X - candidate.Pos.X,
            Pos.Y - candidate.Pos.Y,
            Pos.Z - candidate.Pos.Z
        );
        SnapToEntity(candidate);
        return true;
    }

    private bool UpdateAttachedToEntity()
    {
        Entity? entity = World.GetEntityById(attachedEntityId);
        if (entity == null) return false;

        SnapToEntity(entity);
        return true;
    }

    private void SnapToEntity(Entity entity)
    {
        double x = entity.Pos.X + attachedEntityOffset.X;
        double y = entity.Pos.Y + attachedEntityOffset.Y;
        double z = entity.Pos.Z + attachedEntityOffset.Z;
        ServerPos.SetPos(x, y, z);
        Pos.SetPos(x, y, z);
        ServerPos.Motion.Set(0, 0, 0);
        Pos.Motion.Set(0, 0, 0);
    }

    private bool UpdateAttachedToBlock()
    {
        Block block = World.BlockAccessor.GetBlock(attachedBlockPos);
        if (block == null || block.BlockId == 0 || block.Replaceable >= 6000)
        {
            return false;
        }

        SnapToAttachedFace();
        return true;
    }

    private void AttachToSurface()
    {
        BlockPos? bestPos = null;
        double bestDistSq = double.MaxValue;
        BlockPos center = Pos.AsBlockPos;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    BlockPos pos = new(center.X + dx, center.Y + dy, center.Z + dz);
                    Block block = World.BlockAccessor.GetBlock(pos);
                    if (block == null || block.BlockId == 0 || block.Replaceable >= 6000) continue;

                    double cx = pos.X + 0.5;
                    double cy = pos.Y + 0.5;
                    double cz = pos.Z + 0.5;
                    double distSq = (Pos.X - cx) * (Pos.X - cx) + (Pos.Y - cy) * (Pos.Y - cy) + (Pos.Z - cz) * (Pos.Z - cz);
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestPos = pos;
                    }
                }
            }
        }

        if (bestPos == null) return;

        attachedBlockPos.Set(bestPos.X, bestPos.Y, bestPos.Z);
        attachedFace = EstimateImpactFace(attachedBlockPos);
        if (!TrySwitchToStaticSticky(attachedBlockPos, attachedFace))
        {
            attachedToBlock = true;
            SnapToAttachedFace();
        }
    }

    private BlockFacing EstimateImpactFace(BlockPos blockPos)
    {
        double dx = Pos.X - (blockPos.X + 0.5);
        double dy = Pos.Y - (blockPos.Y + 0.5);
        double dz = Pos.Z - (blockPos.Z + 0.5);

        if (Math.Abs(lastMotionX) + Math.Abs(lastMotionY) + Math.Abs(lastMotionZ) > 0.0001)
        {
            dx = -lastMotionX;
            dy = -lastMotionY;
            dz = -lastMotionZ;
        }

        double ax = Math.Abs(dx);
        double ay = Math.Abs(dy);
        double az = Math.Abs(dz);

        if (ax >= ay && ax >= az) return dx >= 0 ? BlockFacing.EAST : BlockFacing.WEST;
        if (ay >= ax && ay >= az) return dy >= 0 ? BlockFacing.UP : BlockFacing.DOWN;
        return dz >= 0 ? BlockFacing.SOUTH : BlockFacing.NORTH;
    }

    private void SnapToAttachedFace()
    {
        Vec3i n = attachedFace.Normali;
        // Place almost on the touched face so it looks mounted instead of hovering.
        const double offset = 0.60;
        double x = attachedBlockPos.X + 0.5 + n.X * offset;
        double y = attachedBlockPos.Y + 0.5 + n.Y * offset;
        double z = attachedBlockPos.Z + 0.5 + n.Z * offset;

        ServerPos.SetPos(x, y, z);
        Pos.SetPos(x, y, z);
        ServerPos.Motion.Set(0, 0, 0);
        Pos.Motion.Set(0, 0, 0);

        // Keep the stick orientation stable while attached so the base doesn't face out randomly.
        float yaw = ServerPos.Yaw;
        if (attachedFace == BlockFacing.NORTH) yaw = 0f;
        else if (attachedFace == BlockFacing.SOUTH) yaw = GameMath.PI;
        else if (attachedFace == BlockFacing.WEST) yaw = GameMath.PIHALF;
        else if (attachedFace == BlockFacing.EAST) yaw = -GameMath.PIHALF;

        ServerPos.Yaw = yaw;
        Pos.Yaw = yaw;
        ServerPos.Pitch = 0f;
        Pos.Pitch = 0f;
    }

    private List<TrackedResourceBlock> CollectResourceBlocksSphere(float radius)
    {
        IBlockAccessor blockAccessor = World.BlockAccessor;
        BlockPos center = Pos.AsBlockPos;
        int r = (int)Math.Ceiling(radius);
        double radiusSq = radius * radius;
        List<TrackedResourceBlock> tracked = new();

        BlockPos tmp = center.Copy();

        for (int dx = -r; dx <= r; dx++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    double distSq = dx * dx + dy * dy + dz * dz;
                    if (distSq > radiusSq) continue;

                    tmp.Set(center.X + dx, center.Y + dy, center.Z + dz);
                    Block block = blockAccessor.GetBlock(tmp);
                    if (block == null || block.BlockId == 0) continue;

                    bool isCrystal = IsCrystalBlock(block);
                    bool isOre = !isCrystal && IsOreBlock(block);
                    if (!isOre && !isCrystal) continue;

                    tracked.Add(new TrackedResourceBlock(tmp.Copy(), block.BlockId, isCrystal));
                }
            }
        }

        return tracked;
    }

    private List<TrackedResourceBlock> CollectResourceBlocksEllipsoid(float horizontalRadius, float verticalRadius)
    {
        IBlockAccessor blockAccessor = World.BlockAccessor;
        BlockPos center = Pos.AsBlockPos;
        int hr = (int)Math.Ceiling(horizontalRadius);
        int vr = (int)Math.Ceiling(verticalRadius);
        List<TrackedResourceBlock> tracked = new();
        BlockPos tmp = center.Copy();

        double invHSq = 1.0 / Math.Max(0.0001, horizontalRadius * horizontalRadius);
        double invVSq = 1.0 / Math.Max(0.0001, verticalRadius * verticalRadius);

        for (int dx = -hr; dx <= hr; dx++)
        {
            for (int dy = -vr; dy <= vr; dy++)
            {
                for (int dz = -hr; dz <= hr; dz++)
                {
                    double norm = (dx * dx + dz * dz) * invHSq + (dy * dy) * invVSq;
                    if (norm > 1.0) continue;

                    tmp.Set(center.X + dx, center.Y + dy, center.Z + dz);
                    Block block = blockAccessor.GetBlock(tmp);
                    if (block == null || block.BlockId == 0) continue;

                    bool isCrystal = IsCrystalBlock(block);
                    bool isOre = !isCrystal && IsOreBlock(block);
                    if (!isOre && !isCrystal) continue;

                    tracked.Add(new TrackedResourceBlock(tmp.Copy(), block.BlockId, isCrystal));
                }
            }
        }

        return tracked;
    }

    private void ClearNonResourceBlocksEllipsoid(float horizontalRadius, float verticalRadius)
    {
        IBlockAccessor blockAccessor = World.BlockAccessor;
        BlockPos center = Pos.AsBlockPos;
        int hr = (int)Math.Ceiling(horizontalRadius);
        int vr = (int)Math.Ceiling(verticalRadius);
        BlockPos tmp = center.Copy();
        int cleared = 0;

        double invHSq = 1.0 / Math.Max(0.0001, horizontalRadius * horizontalRadius);
        double invVSq = 1.0 / Math.Max(0.0001, verticalRadius * verticalRadius);

        for (int dx = -hr; dx <= hr; dx++)
        {
            for (int dy = -vr; dy <= vr; dy++)
            {
                for (int dz = -hr; dz <= hr; dz++)
                {
                    if (cleared >= MaxManualBlockChanges) return;

                    double norm = (dx * dx + dz * dz) * invHSq + (dy * dy) * invVSq;
                    if (norm > 1.0) continue;

                    tmp.Set(center.X + dx, center.Y + dy, center.Z + dz);
                    Block block = blockAccessor.GetBlock(tmp);
                    if (block == null || block.BlockId == 0) continue;
                    if (IsOreBlock(block) || IsCrystalBlock(block)) continue;

                    blockAccessor.SetBlock(0, tmp);
                    cleared++;
                }
            }
        }
    }

    private ResourceDestructionStats ApplyResourceDestruction(List<TrackedResourceBlock> trackedResources, IPlayer? byPlayer, float oreDestroyChance, float crystalDestroyChance)
    {
        IBlockAccessor blockAccessor = World.BlockAccessor;
        int oreTotal = 0;
        int oreDestroyed = 0;
        int orePreserved = 0;
        int crystalTotal = 0;
        int crystalDestroyed = 0;
        int crystalPreserved = 0;

        foreach (TrackedResourceBlock tracked in trackedResources)
        {
            if (tracked.IsCrystal) crystalTotal++;
            else oreTotal++;

            float chance = tracked.IsCrystal ? crystalDestroyChance : oreDestroyChance;
            bool shouldDestroy = World.Rand.NextDouble() <= chance;
            if (shouldDestroy)
            {
                // Vaporize: remove block with no drops.
                blockAccessor.SetBlock(0, tracked.Pos);
                if (tracked.IsCrystal) crystalDestroyed++;
                else oreDestroyed++;
            }
            else
            {
                // Preserve with drops: ensure original ore/crystal exists, then break it normally.
                Block current = blockAccessor.GetBlock(tracked.Pos);
                if (current == null || current.BlockId != tracked.BlockId)
                {
                    blockAccessor.SetBlock(tracked.BlockId, tracked.Pos);
                }

                blockAccessor.BreakBlock(tracked.Pos, byPlayer);
                if (tracked.IsCrystal) crystalPreserved++;
                else orePreserved++;
            }
        }

        return new ResourceDestructionStats(
            oreTotal,
            oreDestroyed,
            orePreserved,
            crystalTotal,
            crystalDestroyed,
            crystalPreserved
        );
    }

    private void SendBlastReport(
        IServerPlayer byPlayer,
        EnumBlastType blockBlastType,
        float rockRadius,
        float oreRadius,
        float entityRadius,
        ResourceDestructionStats? stats,
        int destroyedBlocks)
    {
        ResourceDestructionStats s = stats ?? new ResourceDestructionStats(0, 0, 0, 0, 0, 0);
        float orePct = s.OreTotal > 0 ? (100f * s.OreDestroyed / s.OreTotal) : 0f;
        float crystalPct = s.CrystalTotal > 0 ? (100f * s.CrystalDestroyed / s.CrystalTotal) : 0f;
        string bombTier = WatchedAttributes.GetString(AttrBombTier, "unknown");
        string destroyedStr = destroyedBlocks >= 0 ? destroyedBlocks.ToString() : "?";
        string msg = $"[VSD] {bombTier} blast={blockBlastType} blocksDestroyed={destroyedStr} rock={rockRadius:0.0} ore={oreRadius:0.0} entity={entityRadius:0.0} | " +
                     $"Ore total={s.OreTotal} destroyed={s.OreDestroyed} ({orePct:0.#}%) preserved={s.OrePreserved} | " +
                     $"Crystal total={s.CrystalTotal} destroyed={s.CrystalDestroyed} ({crystalPct:0.#}%) preserved={s.CrystalPreserved}";

        byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.Notification);
    }

    private void LogBlastReport(
        IServerPlayer byPlayer,
        EnumBlastType blockBlastType,
        float rockRadius,
        float oreRadius,
        float entityRadius,
        ResourceDestructionStats? stats,
        int destroyedBlocks)
    {
        ResourceDestructionStats s = stats ?? new ResourceDestructionStats(0, 0, 0, 0, 0, 0);
        float orePct = s.OreTotal > 0 ? (100f * s.OreDestroyed / s.OreTotal) : 0f;
        float crystalPct = s.CrystalTotal > 0 ? (100f * s.CrystalDestroyed / s.CrystalTotal) : 0f;
        string bombTier = WatchedAttributes.GetString(AttrBombTier, "unknown");
        string destroyedStr = destroyedBlocks >= 0 ? destroyedBlocks.ToString() : "?";
        string msg = $"[VSD-DEBUG] player={byPlayer.PlayerName} uid={byPlayer.PlayerUID} bomb={bombTier} blast={blockBlastType} " +
                     $"blocksDestroyed={destroyedStr} rock={rockRadius:0.0} ore={oreRadius:0.0} entity={entityRadius:0.0} " +
                     $"oreDestroyed={s.OreDestroyed}/{s.OreTotal} ({orePct:0.#}%) crystalDestroyed={s.CrystalDestroyed}/{s.CrystalTotal} ({crystalPct:0.#}%)";

        World.Api?.Logger?.Notification(msg);
    }

    public static bool IsDebugBlastReportEnabled(string playerUid)
    {
        if (string.IsNullOrWhiteSpace(playerUid)) return false;
        lock (DebugBlastReportUids)
        {
            return DebugBlastReportUids.Contains(playerUid);
        }
    }

    private List<TrackedSolidBlock> CaptureSolidBlocksSphere(float radius)
    {
        IBlockAccessor blockAccessor = World.BlockAccessor;
        BlockPos center = Pos.AsBlockPos;
        int r = (int)Math.Ceiling(radius);
        double radiusSq = radius * radius;
        List<TrackedSolidBlock> tracked = new();
        BlockPos tmp = center.Copy();

        for (int dx = -r; dx <= r; dx++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    if (tracked.Count >= MaxManualBlockChanges) return tracked;

                    double distSq = dx * dx + dy * dy + dz * dz;
                    if (distSq > radiusSq) continue;

                    tmp.Set(center.X + dx, center.Y + dy, center.Z + dz);
                    Block block = blockAccessor.GetBlock(tmp);
                    if (block == null || block.BlockId == 0) continue;
                    tracked.Add(new TrackedSolidBlock(tmp.Copy(), block.BlockId));
                }
            }
        }

        return tracked;
    }

    private List<TrackedSolidBlock> CaptureSolidBlocksEllipsoid(float horizontalRadius, float verticalRadius)
    {
        IBlockAccessor blockAccessor = World.BlockAccessor;
        BlockPos center = Pos.AsBlockPos;
        int hr = (int)Math.Ceiling(horizontalRadius);
        int vr = (int)Math.Ceiling(verticalRadius);
        List<TrackedSolidBlock> tracked = new();
        BlockPos tmp = center.Copy();

        double invHSq = 1.0 / Math.Max(0.0001, horizontalRadius * horizontalRadius);
        double invVSq = 1.0 / Math.Max(0.0001, verticalRadius * verticalRadius);

        for (int dx = -hr; dx <= hr; dx++)
        {
            for (int dy = -vr; dy <= vr; dy++)
            {
                for (int dz = -hr; dz <= hr; dz++)
                {
                    if (tracked.Count >= MaxManualBlockChanges) return tracked;

                    double norm = (dx * dx + dz * dz) * invHSq + (dy * dy) * invVSq;
                    if (norm > 1.0) continue;

                    tmp.Set(center.X + dx, center.Y + dy, center.Z + dz);
                    Block block = blockAccessor.GetBlock(tmp);
                    if (block == null || block.BlockId == 0) continue;
                    tracked.Add(new TrackedSolidBlock(tmp.Copy(), block.BlockId));
                }
            }
        }

        return tracked;
    }

    private int CountDestroyedBlocks(List<TrackedSolidBlock> preSnapshot)
    {
        IBlockAccessor blockAccessor = World.BlockAccessor;
        int destroyed = 0;

        foreach (TrackedSolidBlock tracked in preSnapshot)
        {
            Block block = blockAccessor.GetBlock(tracked.Pos);
            if (block == null || block.BlockId == 0)
            {
                destroyed++;
            }
        }

        return destroyed;
    }

    public static bool SetDebugBlastReportEnabled(string playerUid, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(playerUid)) return false;
        lock (DebugBlastReportUids)
        {
            if (enabled) DebugBlastReportUids.Add(playerUid);
            else DebugBlastReportUids.Remove(playerUid);
            return enabled;
        }
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
        if (World.Side == EnumAppSide.Client)
        {
            fuseSound?.Stop();
            fuseSound?.Dispose();
            fuseSound = null;
            soundStarted = false;
        }

        base.OnEntityDespawn(despawn);
    }

    private static void PlayClientOneShot(ICoreClientAPI capi, string soundPath, Vec3f atPos, float volume)
    {
        ILoadedSound? oneshot = capi.World.LoadSound(new SoundParams()
        {
            Location = new AssetLocation(soundPath),
            ShouldLoop = false,
            DisposeOnFinish = true,
            RelativePosition = false,
            Position = atPos,
            Range = 24f,
            Volume = volume
        });

        oneshot?.Start();
    }

    private void SpawnFuseSparks(ICoreClientAPI capi, float dt)
    {
        sparkAccum += dt;
        smokeAccum += dt;
        // Keep fuse visuals lightweight to avoid overloading client render pipeline,
        // especially when multiple particle-heavy mods are active.
        bool spawnSparks = sparkAccum >= 0.06f;
        bool spawnSmoke = smokeAccum >= 0.11f;
        if (!spawnSparks && !spawnSmoke) return;

        // Fuse tip anchor derived from model element "cube":
        // center = [6.5, 2.5, -1.5], rotationOrigin = [8, 0, 8].
        // Then adjusted by user request: +1 cube up, -1 cube forward.
        double rightOffset = -0.2825;
        double upOffset = 0.0425;
        double forwardOffset = 0.02;

        Vec3d localFuseOffset = new Vec3d(rightOffset, upOffset, forwardOffset);
        Vec3d rotatedOffset = RotateLocalOffset(localFuseOffset, Pos.Yaw, Pos.Pitch, Pos.Roll);
        Vec3d fusePos = Pos.XYZ.AddCopy(rotatedOffset.X, rotatedOffset.Y, rotatedOffset.Z);

        if (spawnSparks)
        {
            sparkAccum = 0f;
            Vec3d sparkPos = fusePos.AddCopy(0, 0.03125, 0);
            capi.World.SpawnParticles(
                0.35f,
                unchecked((int)0xFFFFB347),
                sparkPos.AddCopy(-0.004, -0.004, -0.004),
                sparkPos.AddCopy(0.004, 0.004, 0.004),
                new Vec3f(-0.03f, 0.02f, -0.03f),
                new Vec3f(0.03f, 0.14f, 0.03f),
                0.12f,
                0f,
                0.03f,
                EnumParticleModel.Quad,
                null
            );
        }

        if (spawnSmoke)
        {
            smokeAccum = 0f;
            capi.World.SpawnParticles(
                0.45f,
                unchecked((int)0xEE666666),
                fusePos.AddCopy(-0.005, -0.005, -0.005),
                fusePos.AddCopy(0.005, 0.005, 0.005),
                new Vec3f(-0.015f, 0.04f, -0.015f),
                new Vec3f(0.015f, 0.11f, 0.015f),
                0.36f,
                -0.01f,
                0.05f,
                EnumParticleModel.Quad,
                null
            );
        }
    }

    private static Vec3d RotateLocalOffset(Vec3d local, float yaw, float pitch, float roll)
    {
        double x = local.X;
        double y = local.Y;
        double z = local.Z;

        double cy = Math.Cos(yaw);
        double sy = Math.Sin(yaw);
        double cp = Math.Cos(pitch);
        double sp = Math.Sin(pitch);
        double cr = Math.Cos(roll);
        double sr = Math.Sin(roll);

        // Yaw around Y
        double x1 = x * cy + z * sy;
        double y1 = y;
        double z1 = -x * sy + z * cy;

        // Pitch around X
        double x2 = x1;
        double y2 = y1 * cp - z1 * sp;
        double z2 = y1 * sp + z1 * cp;

        // Roll around Z
        double x3 = x2 * cr - y2 * sr;
        double y3 = x2 * sr + y2 * cr;
        double z3 = z2;

        return new Vec3d(x3, y3, z3);
    }
}
