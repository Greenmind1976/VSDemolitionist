using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace VSDemolitionist;

public class EntityBomb : Entity
{
    private const float FuseSeconds = 4f;

    private const string AttrLit = "vsd_lit";
    private const string AttrFuseEndMs = "vsd_fuseEndMs";

    private long holderId = -1;

    private ILoadedSound? fuseSound;
    private bool soundStarted = false;

    public void StartFuse()
    {
        if (World.Side != EnumAppSide.Server) return;

        long endMs = World.ElapsedMilliseconds + (long)(FuseSeconds * 1000);

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

    double forwardStrength = 0.40;
    double upwardBoost = 0.10;

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
            long fuseEnd = WatchedAttributes.GetLong(AttrFuseEndMs, 0);
            if (fuseEnd > 0 && World.ElapsedMilliseconds >= fuseEnd)
            {
                World.Logger.Warning("[VSD] Explode() called at " + World.ElapsedMilliseconds);
                Explode();
                return;
            }
        }

        // CLIENT: handle fuse sound
        if (World.Side == EnumAppSide.Client && WatchedAttributes.GetBool(AttrLit))
        {
            ICoreClientAPI? capi = Api as ICoreClientAPI;
            if (capi == null) return;

            if (!soundStarted)
            {
                soundStarted = true;

                fuseSound = capi.World.LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("vsdemolitionist", "sounds/fuse"),
                    ShouldLoop = true,
                    DisposeOnFinish = false,
                    RelativePosition = false,
                    Range = 8f,
                    Volume = 2.0f,
                    Position = Pos.XYZ.ToVec3f()
                });

                fuseSound?.Start();
            }

            if (fuseSound != null && capi.World.Player?.Entity != null)
            {
                // ✅ THIS WAS MISSING
                fuseSound.SetPosition(Pos.XYZ.ToVec3f());

                double distance = Pos.DistanceTo(capi.World.Player.Entity.Pos.XYZ);

                float maxDistance = 12f;
                float minVolume = 0.05f;

                float t = (float)Math.Min(distance / maxDistance, 1f);

                float volume = 1f - (t * t * t);
                volume = Math.Max(minVolume, volume);

                fuseSound.SetVolume(volume);
            }
        }
    }

    private void Explode()
    {
        if (World.Side != EnumAppSide.Server) return;

        ICoreServerAPI sapi = (ICoreServerAPI)World.Api;

        EnumBlastType[] blastTypes =
        {
            EnumBlastType.RockBlast,
            EnumBlastType.OreBlast,
            EnumBlastType.EntityBlast
        };

        foreach (EnumBlastType blastType in blastTypes)
        {
            sapi.World.CreateExplosion(
                Pos.AsBlockPos,
                blastType,
                4f,
                1f,
                0f,
                "explosion"
            );
        }

        Die(EnumDespawnReason.Removed);
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
        if (World.Side == EnumAppSide.Client)
        {
            fuseSound?.Stop();
            fuseSound?.Dispose();
            fuseSound = null;
        }

        base.OnEntityDespawn(despawn);
    }
}
