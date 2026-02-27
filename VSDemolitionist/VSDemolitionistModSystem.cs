using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace VSDemolitionist;

public class VSDemolitionistModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        api.RegisterItemClass("ItemBomb", typeof(ItemBomb));
        api.RegisterEntity("EntityBomb", typeof(EntityBomb));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Event.OnEntitySpawn += entity =>
        {
            if (entity?.Code?.Path != "bomb") return;

            api.Event.RegisterCallback(dt =>
            {
                if (!entity.Alive) return;

                api.World.CreateExplosion(
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