using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace VSDemolitionist;

public class EntityBomb : Entity
{
    private const float DefaultFuseSeconds = 4f;
    private const float DefaultThrowForwardStrength = 0.40f;
    private const float DefaultThrowUpwardBoost = 0.10f;
    private const float DefaultThrownFuseVolume = 1.8f;
    private const float DefaultBlastRadius = 4f;
    private const float DefaultBlastDropoff = 1f;
    private const float DefaultBlastPowerLoss = 0f;
    private const string BombAttrRoot = "bomb";

    private const string AttrLit = "vsd_lit";
    private const string AttrFuseEndMs = "vsd_fuseEndMs";
    private const string AttrFuseSeconds = "vsd_fuseSeconds";
    private const string AttrThrowForwardStrength = "vsd_throwForwardStrength";
    private const string AttrThrowUpwardBoost = "vsd_throwUpwardBoost";
    private const string AttrThrownFuseVolume = "vsd_thrownFuseVolume";
    private const string AttrRockBlastRadius = "vsd_rockBlastRadius";
    private const string AttrOreBlastRadius = "vsd_oreBlastRadius";
    private const string AttrEntityBlastRadius = "vsd_entityBlastRadius";
    private const string AttrBlastDropoff = "vsd_blastDropoff";
    private const string AttrBlastPowerLoss = "vsd_blastPowerLoss";
    private const string AttrOreDestroyChance = "vsd_oreDestroyChance";
    private const string AttrCrystalDestroyChance = "vsd_crystalDestroyChance";

    private long holderId = -1;

    private ILoadedSound? fuseSound;
    private bool soundStarted = false;
    private bool airborneClient = false;
    private bool impactPlayedClient = false;
    private bool airborneServer = false;
    private bool impactPlayedServer = false;
    private float sparkAccum;
    private float smokeAccum;

    private float GetConfigFloat(string key, float defaultValue)
    {
        return WatchedAttributes.GetFloat(key, defaultValue);
    }

    private static float GetBombFloat(ItemStack? stack, string key, float defaultValue)
    {
        return stack?.Collectible?.Attributes?[BombAttrRoot]?[key].AsFloat(defaultValue) ?? defaultValue;
    }

    private static float Clamp01(float value)
    {
        if (value < 0f) return 0f;
        if (value > 1f) return 1f;
        return value;
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
        WatchedAttributes.SetFloat(AttrOreDestroyChance, Clamp01(GetBombFloat(stack, "oreDestroyChance", 1f)));
        WatchedAttributes.SetFloat(AttrCrystalDestroyChance, Clamp01(GetBombFloat(stack, "crystalDestroyChance", 1f)));
    }

    public void StartFuse()
    {
        StartFuseWithRemainingSeconds(GetConfigFloat(AttrFuseSeconds, DefaultFuseSeconds));
    }

    public void StartFuseWithRemainingSeconds(float remainingSeconds)
    {
        if (World.Side != EnumAppSide.Server) return;

        long endMs = World.ElapsedMilliseconds + (long)(Math.Max(0f, remainingSeconds) * 1000);

        WatchedAttributes.SetBool(AttrLit, true);
        WatchedAttributes.SetLong(AttrFuseEndMs, endMs);
    }

    public void SetHeldBy(EntityAgent holder)
    {
        holderId = holder.EntityId;
        TeleportToHolder(holder);
    }

public void Release(EntityAgent holder)
{
    holderId = -1;

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
        World.Logger.Warning("[VSD] EntityBomb tick running");
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

            long fuseEnd = WatchedAttributes.GetLong(AttrFuseEndMs, 0);
            if (fuseEnd > 0 && World.ElapsedMilliseconds >= fuseEnd)
            {
                World.Logger.Warning("[VSD] Explode() called at " + World.ElapsedMilliseconds);
                Explode();
                return;
            }
        }

        // CLIENT: handle thrown fuse sound/visuals/impacts
        if (World.Side == EnumAppSide.Client && WatchedAttributes.GetBool(AttrLit))
        {
            ICoreClientAPI? capi = Api as ICoreClientAPI;
            if (capi == null) return;

            if (holderId == -1 && !soundStarted)
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

            if (holderId == -1 && fuseSound != null)
            {
                fuseSound.SetPosition(Pos.XYZ.ToVec3f());
                fuseSound.SetVolume(GetConfigFloat(AttrThrownFuseVolume, DefaultThrownFuseVolume));
            }

            double speedSqClient = Pos.Motion.X * Pos.Motion.X + Pos.Motion.Y * Pos.Motion.Y + Pos.Motion.Z * Pos.Motion.Z;

            if (holderId == -1)
            {
                SpawnFuseSparks(capi, dt);
            }

            // CLIENT: play one-shot impact when a thrown bomb first contacts a surface.
            if (!impactPlayedClient && holderId == -1)
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

    private void Explode()
    {
        if (World.Side != EnumAppSide.Server) return;

        ICoreServerAPI sapi = (ICoreServerAPI)World.Api;

        float rockRadius = GetConfigFloat(AttrRockBlastRadius, DefaultBlastRadius);
        float oreRadius = GetConfigFloat(AttrOreBlastRadius, DefaultBlastRadius);
        float entityRadius = GetConfigFloat(AttrEntityBlastRadius, DefaultBlastRadius);
        float dropoff = GetConfigFloat(AttrBlastDropoff, DefaultBlastDropoff);
        float powerLoss = GetConfigFloat(AttrBlastPowerLoss, DefaultBlastPowerLoss);

        if (rockRadius > 0f)
        {
            sapi.World.CreateExplosion(Pos.AsBlockPos, EnumBlastType.RockBlast, rockRadius, dropoff, powerLoss, "explosion");
        }

        if (oreRadius > 0f)
        {
            ProcessOreAndCrystalDestruction(
                oreRadius,
                Clamp01(GetConfigFloat(AttrOreDestroyChance, 1f)),
                Clamp01(GetConfigFloat(AttrCrystalDestroyChance, 1f))
            );
        }

        if (entityRadius > 0f)
        {
            sapi.World.CreateExplosion(Pos.AsBlockPos, EnumBlastType.EntityBlast, entityRadius, dropoff, powerLoss, "explosion");
        }

        Die(EnumDespawnReason.Removed);
    }

    private void ProcessOreAndCrystalDestruction(float radius, float oreDestroyChance, float crystalDestroyChance)
    {
        IBlockAccessor blockAccessor = World.BlockAccessor;
        BlockPos center = Pos.AsBlockPos;
        int r = (int)Math.Ceiling(radius);
        double radiusSq = radius * radius;

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

                    float chance = isCrystal ? crystalDestroyChance : oreDestroyChance;
                    if (World.Rand.NextDouble() > chance) continue;

                    blockAccessor.SetBlock(0, tmp);
                }
            }
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
        bool spawnSparks = sparkAccum >= 0.02f;
        bool spawnSmoke = smokeAccum >= 0.03f;
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
                1.4f,
                unchecked((int)0xFFFFB347),
                sparkPos.AddCopy(-0.004, -0.004, -0.004),
                sparkPos.AddCopy(0.004, 0.004, 0.004),
                new Vec3f(-0.03f, 0.02f, -0.03f),
                new Vec3f(0.03f, 0.14f, 0.03f),
                0.18f,
                0f,
                0.06f,
                EnumParticleModel.Quad,
                null
            );
        }

        if (spawnSmoke)
        {
            smokeAccum = 0f;
            capi.World.SpawnParticles(
                2.2f,
                unchecked((int)0xEE666666),
                fusePos.AddCopy(-0.005, -0.005, -0.005),
                fusePos.AddCopy(0.005, 0.005, 0.005),
                new Vec3f(-0.015f, 0.04f, -0.015f),
                new Vec3f(0.015f, 0.11f, 0.015f),
                0.7f,
                -0.01f,
                0.14f,
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
