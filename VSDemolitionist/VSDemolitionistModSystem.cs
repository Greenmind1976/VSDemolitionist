using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace VSDemolitionist;

public class VSDemolitionistModSystem : ModSystem
{
    private ICoreServerAPI sapi;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        api.RegisterItemClass("ItemBomb", typeof(ItemBomb));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        api.Event.OnEntitySpawn += entity =>
        {
            if (entity?.Code?.Path != "bomb") return;

            sapi.Event.RegisterCallback(dt =>
            {
                if (!entity.Alive) return;

                sapi.World.CreateExplosion(
                    entity.Pos.AsBlockPos,
                    EnumBlastType.EntityBlast,
                    4f,
                    4f
                );

                entity.Die();

            }, 1500);
        };
    }
}