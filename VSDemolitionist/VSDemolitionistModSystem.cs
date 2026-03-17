using System;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace VSDemolitionist;

public class VSDemolitionistModSystem : ModSystem
{
    private const string ConfigFileName = "VSDemolitionistConfig.json";
    private const string Domain = "vsdemolitionist";
    private static VSDemolitionistConfig CurrentConfig { get; set; } = new();
    private ICoreServerAPI? sapi;
    private VSDemolitionistConfig config = new();
    private object? configLibModSystem;
    private bool configLibSubscribed;
    private Delegate? configLibSettingChangedHandler;
    private Delegate? configLibConfigsLoadedHandler;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);

        config = api.LoadModConfig<VSDemolitionistConfig>(ConfigFileName) ?? new VSDemolitionistConfig();
        config.EnsureDefaults();
        api.StoreModConfig(config, ConfigFileName);
        CurrentConfig = config;
        EntityBomb.SetCustomBlastSoundsEnabled(config.CustomBlastSoundsEnabled);
        EntityBomb.SetCustomBlastSoundVolume(config.CustomBlastSoundsVolume);

        api.RegisterItemClass("ItemBomb", typeof(ItemBomb));
        api.RegisterItemClass("ItemDetonator", typeof(ItemDetonator));
        api.RegisterEntity("EntityBomb", typeof(EntityBomb));

        TrySubscribeToConfigLib(api);
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        base.AssetsLoaded(api);
        TrySubscribeToConfigLib(api);
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
        config.EnsureDefaults();
        sapi?.StoreModConfig(config, ConfigFileName);
        CurrentConfig = config;
        EntityBomb.SetCustomBlastSoundsEnabled(enable);
        EntityBomb.SetCustomBlastSoundVolume(config.CustomBlastSoundsVolume);

        string state = enable ? "ON" : "OFF";
        byPlayer.SendMessage(groupId, $"[VSD] dynamitesounds {state}", EnumChatType.Notification);
    }

    public static BombBalanceConfig? GetBombOverride(string? bombCode)
    {
        if (string.IsNullOrWhiteSpace(bombCode)) return null;
        if (CurrentConfig.BombOverrides == null) return null;
        return CurrentConfig.BombOverrides.TryGetValue(bombCode, out BombBalanceConfig? value) ? value : null;
    }

    public static float GetBundleRockRubbleChance()
    {
        return GameMath.Clamp(CurrentConfig.BundleRockRubbleChance, 0f, 1f);
    }

    public static float GetDetonatorRadius()
    {
        return Math.Max(0f, CurrentConfig.DetonatorRadius);
    }

    public static float GetFuseVolume()
    {
        return GameMath.Clamp(CurrentConfig.FuseVolume, 0f, 1f);
    }

    public static bool Use3DIcons()
    {
        return CurrentConfig.Use3DIcons;
    }

    private void TrySubscribeToConfigLib(ICoreAPI api)
    {
        if (configLibSubscribed) return;
        if (!api.ModLoader.IsModEnabled("configlib")) return;

        configLibModSystem = api.ModLoader.GetModSystem("ConfigLib.ConfigLibModSystem");
        if (configLibModSystem == null) return;

        var systemType = configLibModSystem.GetType();

        var settingChangedEvent = systemType.GetEvent("SettingChanged");
        if (settingChangedEvent != null)
        {
            var mi = GetType().GetMethod(nameof(OnConfigLibSettingChanged), BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi != null && configLibSettingChangedHandler == null)
            {
                configLibSettingChangedHandler = Delegate.CreateDelegate(settingChangedEvent.EventHandlerType!, this, mi);
                settingChangedEvent.AddEventHandler(configLibModSystem, configLibSettingChangedHandler);
            }
        }

        var configsLoadedEvent = systemType.GetEvent("ConfigsLoaded");
        if (configsLoadedEvent != null)
        {
            var mi = GetType().GetMethod(nameof(OnConfigLibConfigsLoaded), BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi != null && configLibConfigsLoadedHandler == null)
            {
                configLibConfigsLoadedHandler = Delegate.CreateDelegate(configsLoadedEvent.EventHandlerType!, this, mi);
                configsLoadedEvent.AddEventHandler(configLibModSystem, configLibConfigsLoadedHandler);
            }
        }

        configLibSubscribed = true;
    }

#pragma warning disable IDE0060
    private void OnConfigLibSettingChanged(string domain, object _config, object setting)
    {
        if (!string.Equals(domain, Domain, StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            var assign = setting.GetType().GetMethod("AssignSettingValue", [typeof(object)]);
            assign?.Invoke(setting, [config]);
        }
        catch
        {
        }

        ApplyLiveConfig();
    }
#pragma warning restore IDE0060

    private void OnConfigLibConfigsLoaded()
    {
        try
        {
            if (configLibModSystem == null) return;

            var systemType = configLibModSystem.GetType();
            var getConfig = systemType.GetMethod("GetConfig", [typeof(string)]);
            var cfg = getConfig?.Invoke(configLibModSystem, [Domain]);
            if (cfg == null) return;

            var assignAll = cfg.GetType().GetMethod("AssignSettingsValues", [typeof(object)]);
            assignAll?.Invoke(cfg, [config]);
        }
        catch
        {
        }

        ApplyLiveConfig();
    }

    private void ApplyLiveConfig()
    {
        config.EnsureDefaults();
        CurrentConfig = config;
        EntityBomb.SetCustomBlastSoundsEnabled(config.CustomBlastSoundsEnabled);
        EntityBomb.SetCustomBlastSoundVolume(config.CustomBlastSoundsVolume);
        (sapi as ICoreAPI)?.StoreModConfig(config, ConfigFileName);
    }
}
