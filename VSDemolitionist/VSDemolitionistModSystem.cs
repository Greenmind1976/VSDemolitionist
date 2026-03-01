using Vintagestory.API.Common;

namespace VSDemolitionist;

public class VSDemolitionistModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        api.RegisterItemClass("ItemBomb", typeof(ItemBomb));
        api.RegisterEntity("EntityBomb", typeof(EntityBomb));
    }
}