using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Config;

namespace VSDemolitionist;

public class VSDemolitionistModSystem : ModSystem
{
    private ICoreServerAPI? sapi;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        api.RegisterItemClass("ItemBomb", typeof(ItemBomb));
        api.RegisterItemClass("ItemDemokettle", typeof(ItemDemokettle));
        api.RegisterEntity("EntityBomb", typeof(EntityBomb));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        sapi = api;

        #pragma warning disable CS0618
        api.RegisterCommand(
            "dynamitedebug",
            "Toggle VSD blast debug report in chat",
            "/dynamitedebug [on|off]",
            OnDynamiteDebugCommand,
            Privilege.chat
        );
        #pragma warning restore CS0618
    }

    private void OnDynamiteDebugCommand(IServerPlayer byPlayer, int groupId, CmdArgs args)
    {
        bool currentlyEnabled = EntityBomb.IsDebugBlastReportEnabled(byPlayer.PlayerUID);
        string? mode = args.PopWord();

        bool enable = mode switch
        {
            "on" => true,
            "off" => false,
            _ => !currentlyEnabled
        };

        EntityBomb.SetDebugBlastReportEnabled(byPlayer.PlayerUID, enable);
        string state = enable ? "ON" : "OFF";
        byPlayer.SendMessage(groupId, $"[VSD] dynamitedebug {state}", EnumChatType.Notification);
    }
}
