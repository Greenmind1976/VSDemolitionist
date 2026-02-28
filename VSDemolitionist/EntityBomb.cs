using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace VSDemolitionist;

public class EntityBomb : Entity
{
    private ILoadedSound? fuseSound;

    private bool soundStarted = false;
    private long firstClientTickMs = -1;

    private const float FuseVolume = 1.0f;
    private const float FuseRange = 48f;

    public override void OnGameTick(float dt)
    {
        base.OnGameTick(dt);

        if (World.Side != EnumAppSide.Client) return;

        // Mark the first time we ever tick on the client
        if (firstClientTickMs < 0)
        {
            firstClientTickMs = World.ElapsedMilliseconds;
        }

        // Start fuse sound once, on first tick
        if (!soundStarted)
        {
            soundStarted = true;

            var capi = Api as ICoreClientAPI;
            if (capi != null)
            {
                fuseSound = capi.World.LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("vsdemolitionist", "sounds/fuse"),
                    ShouldLoop = true,
                    DisposeOnFinish = false,
                    RelativePosition = false,
                    Range = FuseRange,
                    Volume = FuseVolume,
                    Position = Pos.XYZ.ToVec3f()
                });

                fuseSound?.Start();
            }
        }

        // Follow bomb
        if (fuseSound != null)
        {
            fuseSound.SetPosition(Pos.XYZ.ToVec3f());
        }
    }

    public override void OnEntityDespawn(EntityDespawnData despawn)
    {
        if (World.Side == EnumAppSide.Client && fuseSound != null)
        {
            fuseSound.Stop();
            fuseSound.Dispose();
            fuseSound = null;
        }

        base.OnEntityDespawn(despawn);
    }
}