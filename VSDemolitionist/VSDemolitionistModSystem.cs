using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Config;

namespace VSDemolitionist;

public class VSDemolitionistModSystem : ModSystem
{
    private const string ConfigFileName = "VSDemolitionistConfig.json";
    private ICoreServerAPI? sapi;
    private VSDemolitionistConfig config = new();

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        config = api.LoadModConfig<VSDemolitionistConfig>(ConfigFileName) ?? new VSDemolitionistConfig();
        api.StoreModConfig(config, ConfigFileName);
        EntityBomb.SetCustomBlastSoundsEnabled(config.CustomBlastSoundsEnabled);

        api.RegisterItemClass("ItemBomb", typeof(ItemBomb));
        api.RegisterItemClass("ItemDetonator", typeof(ItemDetonator));
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

        api.RegisterCommand(
            "dynamitesounds",
            "Toggle VSD custom blast sounds",
            "/dynamitesounds [on|off]",
            OnDynamiteSoundsCommand,
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

    private void OnDynamiteSoundsCommand(IServerPlayer byPlayer, int groupId, CmdArgs args)
    {
        bool currentlyEnabled = EntityBomb.IsCustomBlastSoundsEnabled();
        string? mode = args.PopWord();

        bool enable = mode switch
        {
            "on" => true,
            "off" => false,
            _ => !currentlyEnabled
        };

        config.CustomBlastSoundsEnabled = enable;
        sapi?.StoreModConfig(config, ConfigFileName);
        EntityBomb.SetCustomBlastSoundsEnabled(enable);

        string state = enable ? "ON" : "OFF";
        byPlayer.SendMessage(groupId, $"[VSD] dynamitesounds {state}", EnumChatType.Notification);
    }
}
