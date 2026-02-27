using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace VSDemolitionist;

public class EntityBomb : Entity
{
    public override void Initialize(EntityProperties properties, ICoreAPI api, long chunkindex3d)
    {
        base.Initialize(properties, api, chunkindex3d);

        // Keep this log while debugging
        api.Logger.Notification("EntityBomb initialized on " + api.Side);
    }
}