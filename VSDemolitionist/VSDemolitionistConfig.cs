using System.Collections.Generic;

namespace VSDemolitionist;

public class VSDemolitionistConfig
{
    public int SchemaVersion { get; set; } = 5;
    public bool Use3DIcons { get; set; } = true;
    public bool CustomBlastSoundsEnabled { get; set; } = true;
    public float CustomBlastSoundsVolume { get; set; } = 1.0f;
    public float FuseVolume { get; set; } = 0.7f;
    public float BundleRockRubbleChance { get; set; } = 0.15f;
    public bool PlayerTriggersLandmines { get; set; } = true;
    public bool OwnerTriggersLandmines { get; set; } = true;
    public float LandmineEntityDamage { get; set; } = 6.0f;
    public float LandmineInteractRange { get; set; } = 3.0f;
    public float LandmineOwnerGraceSeconds { get; set; } = 1.5f;

    public float CopperStickFuseSeconds { get; set; } = 4.0f;
    public float CopperStickRockBlastRadius { get; set; } = 4.0f;
    public float CopperStickOreBlastRadius { get; set; } = 4.0f;
    public float CopperStickEntityBlastRadius { get; set; } = 3.4f;
    public float CopperStickOreDestroyChance { get; set; } = 0.5f;
    public float CopperStickCrystalDestroyChance { get; set; } = 0.8f;

    public float BronzeStickFuseSeconds { get; set; } = 4.0f;
    public float BronzeStickRockBlastRadius { get; set; } = 4.4f;
    public float BronzeStickOreBlastRadius { get; set; } = 4.4f;
    public float BronzeStickEntityBlastRadius { get; set; } = 3.8f;
    public float BronzeStickOreDestroyChance { get; set; } = 0.4f;
    public float BronzeStickCrystalDestroyChance { get; set; } = 0.6f;

    public float IronStickFuseSeconds { get; set; } = 4.0f;
    public float IronStickRockBlastRadius { get; set; } = 5.5f;
    public float IronStickOreBlastRadius { get; set; } = 5.5f;
    public float IronStickEntityBlastRadius { get; set; } = 4.7f;
    public float IronStickOreDestroyChance { get; set; } = 0.2f;
    public float IronStickCrystalDestroyChance { get; set; } = 0.4f;

    public float SteelStickFuseSeconds { get; set; } = 4.0f;
    public float SteelStickRockBlastRadius { get; set; } = 5.5f;
    public float SteelStickOreBlastRadius { get; set; } = 5.5f;
    public float SteelStickEntityBlastRadius { get; set; } = 4.7f;
    public float SteelStickOreDestroyChance { get; set; } = 0.1f;
    public float SteelStickCrystalDestroyChance { get; set; } = 0.2f;

    public float TitaniumStickFuseSeconds { get; set; } = 4.0f;
    public float TitaniumStickRockBlastRadius { get; set; } = 5.5f;
    public float TitaniumStickOreBlastRadius { get; set; } = 5.5f;
    public float TitaniumStickEntityBlastRadius { get; set; } = 4.7f;
    public float TitaniumStickOreDestroyChance { get; set; } = 0.05f;
    public float TitaniumStickCrystalDestroyChance { get; set; } = 0.1f;

    public float CopperBundleFuseSeconds { get; set; } = 4.0f;
    public float CopperBundleRockBlastRadius { get; set; } = 7.0f;
    public float CopperBundleOreBlastRadius { get; set; } = 7.0f;
    public float CopperBundleEntityBlastRadius { get; set; } = 6.0f;
    public float CopperBundleOreDestroyChance { get; set; } = 0.6f;
    public float CopperBundleCrystalDestroyChance { get; set; } = 1.0f;

    public float BronzeBundleFuseSeconds { get; set; } = 4.0f;
    public float BronzeBundleRockBlastRadius { get; set; } = 7.5f;
    public float BronzeBundleOreBlastRadius { get; set; } = 7.5f;
    public float BronzeBundleEntityBlastRadius { get; set; } = 6.5f;
    public float BronzeBundleOreDestroyChance { get; set; } = 0.5f;
    public float BronzeBundleCrystalDestroyChance { get; set; } = 0.8f;

    public float IronBundleFuseSeconds { get; set; } = 4.0f;
    public float IronBundleRockBlastRadius { get; set; } = 8.0f;
    public float IronBundleOreBlastRadius { get; set; } = 8.0f;
    public float IronBundleEntityBlastRadius { get; set; } = 7.0f;
    public float IronBundleOreDestroyChance { get; set; } = 0.3f;
    public float IronBundleCrystalDestroyChance { get; set; } = 0.6f;

    public float SteelBundleFuseSeconds { get; set; } = 4.0f;
    public float SteelBundleRockBlastRadius { get; set; } = 8.0f;
    public float SteelBundleOreBlastRadius { get; set; } = 8.0f;
    public float SteelBundleEntityBlastRadius { get; set; } = 7.0f;
    public float SteelBundleOreDestroyChance { get; set; } = 0.2f;
    public float SteelBundleCrystalDestroyChance { get; set; } = 0.4f;

    public float TitaniumBundleFuseSeconds { get; set; } = 4.0f;
    public float TitaniumBundleRockBlastRadius { get; set; } = 8.0f;
    public float TitaniumBundleOreBlastRadius { get; set; } = 8.0f;
    public float TitaniumBundleEntityBlastRadius { get; set; } = 7.0f;
    public float TitaniumBundleOreDestroyChance { get; set; } = 0.15f;
    public float TitaniumBundleCrystalDestroyChance { get; set; } = 0.3f;

    public float ChargeFuseSeconds { get; set; } = 1.0f;
    public int ChargeInwardDepth { get; set; } = 5;
    public int ChargeOutwardDepth { get; set; } = 1;
    public int ChargeLinkRange { get; set; } = 7;
    public float DetonatorRadius { get; set; } = 24.0f;

    public Dictionary<string, BombBalanceConfig> BombOverrides { get; set; } = CreateDefaultBombOverrides();

    public void EnsureDefaults()
    {
        BombOverrides ??= CreateDefaultBombOverrides();

        if (SchemaVersion < 2)
        {
            MigrateFromBombOverrides();
        }

        if (SchemaVersion < 3)
        {
            MigrateDetonatorRadiusDefault();
            SchemaVersion = 3;
        }

        if (SchemaVersion < 4)
        {
            MigrateFuseVolumeDefault();
            SchemaVersion = 4;
        }

        if (SchemaVersion < 5)
        {
            MigrateLandmineDamageDefault();
            SchemaVersion = 5;
        }

        ApplyFlatOverridesToBombOverrides();
    }

    private void MigrateDetonatorRadiusDefault()
    {
        // Old configs were seeded with 24. Update untouched installs to the new intended default of 12.
        if (DetonatorRadius == 24.0f)
        {
            DetonatorRadius = 12.0f;
        }
    }

    private void MigrateFuseVolumeDefault()
    {
        // Old configs were seeded with 1.0. Update untouched installs to the new intended default of 0.7.
        if (FuseVolume == 1.0f)
        {
            FuseVolume = 0.7f;
        }
    }

    private void MigrateLandmineDamageDefault()
    {
        // Old configs were seeded with 8. Update untouched installs to the new intended default of 6.
        if (LandmineEntityDamage == 8.0f)
        {
            LandmineEntityDamage = 6.0f;
        }
    }

    private void MigrateFromBombOverrides()
    {
        if (BombOverrides.TryGetValue("bomb-copper", out BombBalanceConfig? copperStick) && copperStick != null)
        {
            CopperStickFuseSeconds = copperStick.FuseSeconds ?? CopperStickFuseSeconds;
            CopperStickRockBlastRadius = copperStick.RockBlastRadius ?? CopperStickRockBlastRadius;
            CopperStickOreBlastRadius = copperStick.OreBlastRadius ?? CopperStickOreBlastRadius;
            CopperStickEntityBlastRadius = copperStick.EntityBlastRadius ?? CopperStickEntityBlastRadius;
            CopperStickOreDestroyChance = copperStick.OreDestroyChance ?? CopperStickOreDestroyChance;
            CopperStickCrystalDestroyChance = copperStick.CrystalDestroyChance ?? CopperStickCrystalDestroyChance;
        }

        if (BombOverrides.TryGetValue("bomb-bronze", out BombBalanceConfig? bronzeStick) && bronzeStick != null)
        {
            BronzeStickFuseSeconds = bronzeStick.FuseSeconds ?? BronzeStickFuseSeconds;
            BronzeStickRockBlastRadius = bronzeStick.RockBlastRadius ?? BronzeStickRockBlastRadius;
            BronzeStickOreBlastRadius = bronzeStick.OreBlastRadius ?? BronzeStickOreBlastRadius;
            BronzeStickEntityBlastRadius = bronzeStick.EntityBlastRadius ?? BronzeStickEntityBlastRadius;
            BronzeStickOreDestroyChance = bronzeStick.OreDestroyChance ?? BronzeStickOreDestroyChance;
            BronzeStickCrystalDestroyChance = bronzeStick.CrystalDestroyChance ?? BronzeStickCrystalDestroyChance;
        }

        if (BombOverrides.TryGetValue("bomb-iron", out BombBalanceConfig? ironStick) && ironStick != null)
        {
            IronStickFuseSeconds = ironStick.FuseSeconds ?? IronStickFuseSeconds;
            IronStickRockBlastRadius = ironStick.RockBlastRadius ?? IronStickRockBlastRadius;
            IronStickOreBlastRadius = ironStick.OreBlastRadius ?? IronStickOreBlastRadius;
            IronStickEntityBlastRadius = ironStick.EntityBlastRadius ?? IronStickEntityBlastRadius;
            IronStickOreDestroyChance = ironStick.OreDestroyChance ?? IronStickOreDestroyChance;
            IronStickCrystalDestroyChance = ironStick.CrystalDestroyChance ?? IronStickCrystalDestroyChance;
        }

        if (BombOverrides.TryGetValue("bomb-steel", out BombBalanceConfig? steelStick) && steelStick != null)
        {
            SteelStickFuseSeconds = steelStick.FuseSeconds ?? SteelStickFuseSeconds;
            SteelStickRockBlastRadius = steelStick.RockBlastRadius ?? SteelStickRockBlastRadius;
            SteelStickOreBlastRadius = steelStick.OreBlastRadius ?? SteelStickOreBlastRadius;
            SteelStickEntityBlastRadius = steelStick.EntityBlastRadius ?? SteelStickEntityBlastRadius;
            SteelStickOreDestroyChance = steelStick.OreDestroyChance ?? SteelStickOreDestroyChance;
            SteelStickCrystalDestroyChance = steelStick.CrystalDestroyChance ?? SteelStickCrystalDestroyChance;
        }

        if (BombOverrides.TryGetValue("bomb-titanium", out BombBalanceConfig? titaniumStick) && titaniumStick != null)
        {
            TitaniumStickFuseSeconds = titaniumStick.FuseSeconds ?? TitaniumStickFuseSeconds;
            TitaniumStickRockBlastRadius = titaniumStick.RockBlastRadius ?? TitaniumStickRockBlastRadius;
            TitaniumStickOreBlastRadius = titaniumStick.OreBlastRadius ?? TitaniumStickOreBlastRadius;
            TitaniumStickEntityBlastRadius = titaniumStick.EntityBlastRadius ?? TitaniumStickEntityBlastRadius;
            TitaniumStickOreDestroyChance = titaniumStick.OreDestroyChance ?? TitaniumStickOreDestroyChance;
            TitaniumStickCrystalDestroyChance = titaniumStick.CrystalDestroyChance ?? TitaniumStickCrystalDestroyChance;
        }

        if (BombOverrides.TryGetValue("bomb-bundle-copper", out BombBalanceConfig? copperBundle) && copperBundle != null)
        {
            CopperBundleFuseSeconds = copperBundle.FuseSeconds ?? CopperBundleFuseSeconds;
            CopperBundleRockBlastRadius = copperBundle.RockBlastRadius ?? CopperBundleRockBlastRadius;
            CopperBundleOreBlastRadius = copperBundle.OreBlastRadius ?? CopperBundleOreBlastRadius;
            CopperBundleEntityBlastRadius = copperBundle.EntityBlastRadius ?? CopperBundleEntityBlastRadius;
            CopperBundleOreDestroyChance = copperBundle.OreDestroyChance ?? CopperBundleOreDestroyChance;
            CopperBundleCrystalDestroyChance = copperBundle.CrystalDestroyChance ?? CopperBundleCrystalDestroyChance;
        }

        if (BombOverrides.TryGetValue("bomb-bundle-bronze", out BombBalanceConfig? bronzeBundle) && bronzeBundle != null)
        {
            BronzeBundleFuseSeconds = bronzeBundle.FuseSeconds ?? BronzeBundleFuseSeconds;
            BronzeBundleRockBlastRadius = bronzeBundle.RockBlastRadius ?? BronzeBundleRockBlastRadius;
            BronzeBundleOreBlastRadius = bronzeBundle.OreBlastRadius ?? BronzeBundleOreBlastRadius;
            BronzeBundleEntityBlastRadius = bronzeBundle.EntityBlastRadius ?? BronzeBundleEntityBlastRadius;
            BronzeBundleOreDestroyChance = bronzeBundle.OreDestroyChance ?? BronzeBundleOreDestroyChance;
            BronzeBundleCrystalDestroyChance = bronzeBundle.CrystalDestroyChance ?? BronzeBundleCrystalDestroyChance;
        }

        if (BombOverrides.TryGetValue("bomb-bundle-iron", out BombBalanceConfig? ironBundle) && ironBundle != null)
        {
            IronBundleFuseSeconds = ironBundle.FuseSeconds ?? IronBundleFuseSeconds;
            IronBundleRockBlastRadius = ironBundle.RockBlastRadius ?? IronBundleRockBlastRadius;
            IronBundleOreBlastRadius = ironBundle.OreBlastRadius ?? IronBundleOreBlastRadius;
            IronBundleEntityBlastRadius = ironBundle.EntityBlastRadius ?? IronBundleEntityBlastRadius;
            IronBundleOreDestroyChance = ironBundle.OreDestroyChance ?? IronBundleOreDestroyChance;
            IronBundleCrystalDestroyChance = ironBundle.CrystalDestroyChance ?? IronBundleCrystalDestroyChance;
        }

        if (BombOverrides.TryGetValue("bomb-bundle-steel", out BombBalanceConfig? steelBundle) && steelBundle != null)
        {
            SteelBundleFuseSeconds = steelBundle.FuseSeconds ?? SteelBundleFuseSeconds;
            SteelBundleRockBlastRadius = steelBundle.RockBlastRadius ?? SteelBundleRockBlastRadius;
            SteelBundleOreBlastRadius = steelBundle.OreBlastRadius ?? SteelBundleOreBlastRadius;
            SteelBundleEntityBlastRadius = steelBundle.EntityBlastRadius ?? SteelBundleEntityBlastRadius;
            SteelBundleOreDestroyChance = steelBundle.OreDestroyChance ?? SteelBundleOreDestroyChance;
            SteelBundleCrystalDestroyChance = steelBundle.CrystalDestroyChance ?? SteelBundleCrystalDestroyChance;
        }

        if (BombOverrides.TryGetValue("bomb-bundle-titanium", out BombBalanceConfig? titaniumBundle) && titaniumBundle != null)
        {
            TitaniumBundleFuseSeconds = titaniumBundle.FuseSeconds ?? TitaniumBundleFuseSeconds;
            TitaniumBundleRockBlastRadius = titaniumBundle.RockBlastRadius ?? TitaniumBundleRockBlastRadius;
            TitaniumBundleOreBlastRadius = titaniumBundle.OreBlastRadius ?? TitaniumBundleOreBlastRadius;
            TitaniumBundleEntityBlastRadius = titaniumBundle.EntityBlastRadius ?? TitaniumBundleEntityBlastRadius;
            TitaniumBundleOreDestroyChance = titaniumBundle.OreDestroyChance ?? TitaniumBundleOreDestroyChance;
            TitaniumBundleCrystalDestroyChance = titaniumBundle.CrystalDestroyChance ?? TitaniumBundleCrystalDestroyChance;
        }

        if (BombOverrides.TryGetValue("blasting-charge", out BombBalanceConfig? charge) && charge != null)
        {
            ChargeFuseSeconds = charge.FuseSeconds ?? ChargeFuseSeconds;
            ChargeInwardDepth = charge.InwardDepth ?? ChargeInwardDepth;
            ChargeOutwardDepth = charge.OutwardDepth ?? ChargeOutwardDepth;
            ChargeLinkRange = charge.InwardLinkRange ?? ChargeLinkRange;
        }
    }

    private void ApplyFlatOverridesToBombOverrides()
    {
        ApplyOverride("bomb-copper", CopperStickFuseSeconds, CopperStickRockBlastRadius, CopperStickOreBlastRadius, CopperStickEntityBlastRadius, CopperStickOreDestroyChance, CopperStickCrystalDestroyChance);
        ApplyOverride("bomb-bronze", BronzeStickFuseSeconds, BronzeStickRockBlastRadius, BronzeStickOreBlastRadius, BronzeStickEntityBlastRadius, BronzeStickOreDestroyChance, BronzeStickCrystalDestroyChance);
        ApplyOverride("bomb-iron", IronStickFuseSeconds, IronStickRockBlastRadius, IronStickOreBlastRadius, IronStickEntityBlastRadius, IronStickOreDestroyChance, IronStickCrystalDestroyChance);
        ApplyOverride("bomb-steel", SteelStickFuseSeconds, SteelStickRockBlastRadius, SteelStickOreBlastRadius, SteelStickEntityBlastRadius, SteelStickOreDestroyChance, SteelStickCrystalDestroyChance);
        ApplyOverride("bomb-titanium", TitaniumStickFuseSeconds, TitaniumStickRockBlastRadius, TitaniumStickOreBlastRadius, TitaniumStickEntityBlastRadius, TitaniumStickOreDestroyChance, TitaniumStickCrystalDestroyChance);

        ApplyOverride("bomb-bundle-copper", CopperBundleFuseSeconds, CopperBundleRockBlastRadius, CopperBundleOreBlastRadius, CopperBundleEntityBlastRadius, CopperBundleOreDestroyChance, CopperBundleCrystalDestroyChance);
        ApplyOverride("bomb-bundle-bronze", BronzeBundleFuseSeconds, BronzeBundleRockBlastRadius, BronzeBundleOreBlastRadius, BronzeBundleEntityBlastRadius, BronzeBundleOreDestroyChance, BronzeBundleCrystalDestroyChance);
        ApplyOverride("bomb-bundle-iron", IronBundleFuseSeconds, IronBundleRockBlastRadius, IronBundleOreBlastRadius, IronBundleEntityBlastRadius, IronBundleOreDestroyChance, IronBundleCrystalDestroyChance);
        ApplyOverride("bomb-bundle-steel", SteelBundleFuseSeconds, SteelBundleRockBlastRadius, SteelBundleOreBlastRadius, SteelBundleEntityBlastRadius, SteelBundleOreDestroyChance, SteelBundleCrystalDestroyChance);
        ApplyOverride("bomb-bundle-titanium", TitaniumBundleFuseSeconds, TitaniumBundleRockBlastRadius, TitaniumBundleOreBlastRadius, TitaniumBundleEntityBlastRadius, TitaniumBundleOreDestroyChance, TitaniumBundleCrystalDestroyChance);

        if (!BombOverrides.TryGetValue("blasting-charge", out BombBalanceConfig? charge) || charge == null)
        {
            charge = CreateDefaultBombOverrides()["blasting-charge"];
            BombOverrides["blasting-charge"] = charge;
        }

        charge.FuseSeconds = ChargeFuseSeconds;
        charge.InwardDepth = ChargeInwardDepth;
        charge.OutwardDepth = ChargeOutwardDepth;
        charge.InwardLinkRange = ChargeLinkRange;
    }

    private void ApplyOverride(string code, float fuse, float rock, float ore, float entity, float oreDestroy, float crystalDestroy)
    {
        if (!BombOverrides.TryGetValue(code, out BombBalanceConfig? cfg) || cfg == null)
        {
            cfg = CreateDefaultBombOverrides()[code];
            BombOverrides[code] = cfg;
        }

        cfg.FuseSeconds = fuse;
        cfg.RockBlastRadius = rock;
        cfg.OreBlastRadius = ore;
        cfg.EntityBlastRadius = entity;
        cfg.OreDestroyChance = oreDestroy;
        cfg.CrystalDestroyChance = crystalDestroy;
    }

    private static Dictionary<string, BombBalanceConfig> CreateDefaultBombOverrides()
    {
        return new Dictionary<string, BombBalanceConfig>
        {
            ["bomb-copper"] = new() { FuseSeconds = 4.0f, BlastRadius = 4.0f, RockBlastRadius = 4.0f, OreBlastRadius = 4.0f, EntityBlastRadius = 3.4f, OreDestroyChance = 0.5f, CrystalDestroyChance = 0.8f, BlastDropoff = 1.05f, BlastPowerLoss = 0.05f },
            ["bomb-bronze"] = new() { FuseSeconds = 4.0f, BlastRadius = 4.4f, RockBlastRadius = 4.4f, OreBlastRadius = 4.4f, EntityBlastRadius = 3.8f, OreDestroyChance = 0.4f, CrystalDestroyChance = 0.6f, BlastDropoff = 1.0f, BlastPowerLoss = 0.05f },
            ["bomb-iron"] = new() { FuseSeconds = 4.0f, BlastRadius = 5.5f, RockBlastRadius = 5.5f, OreBlastRadius = 5.5f, EntityBlastRadius = 4.7f, OreDestroyChance = 0.2f, CrystalDestroyChance = 0.4f, BlastDropoff = 0.95f, BlastPowerLoss = 0.08f },
            ["bomb-steel"] = new() { FuseSeconds = 4.0f, BlastRadius = 5.5f, RockBlastRadius = 5.5f, OreBlastRadius = 5.5f, EntityBlastRadius = 4.7f, OreDestroyChance = 0.1f, CrystalDestroyChance = 0.2f, BlastDropoff = 0.9f, BlastPowerLoss = 0.1f },
            ["bomb-titanium"] = new() { FuseSeconds = 4.0f, BlastRadius = 5.5f, RockBlastRadius = 5.5f, OreBlastRadius = 5.5f, EntityBlastRadius = 4.7f, OreDestroyChance = 0.05f, CrystalDestroyChance = 0.1f, BlastDropoff = 0.85f, BlastPowerLoss = 0.12f },

            ["bomb-bundle-copper"] = new() { FuseSeconds = 4.0f, BlastRadius = 7.0f, RockBlastRadius = 7.0f, OreBlastRadius = 7.0f, EntityBlastRadius = 6.0f, OreDestroyChance = 0.6f, CrystalDestroyChance = 1.0f, BlastDropoff = 1.0f, BlastPowerLoss = 0.08f, BlastShape = "disc", BlastVerticalRadius = 1.5f },
            ["bomb-bundle-bronze"] = new() { FuseSeconds = 4.0f, BlastRadius = 7.5f, RockBlastRadius = 7.5f, OreBlastRadius = 7.5f, EntityBlastRadius = 6.5f, OreDestroyChance = 0.5f, CrystalDestroyChance = 0.8f, BlastDropoff = 0.95f, BlastPowerLoss = 0.1f, BlastShape = "disc", BlastVerticalRadius = 1.5f },
            ["bomb-bundle-iron"] = new() { FuseSeconds = 4.0f, BlastRadius = 8.0f, RockBlastRadius = 8.0f, OreBlastRadius = 8.0f, EntityBlastRadius = 7.0f, OreDestroyChance = 0.3f, CrystalDestroyChance = 0.6f, BlastDropoff = 0.9f, BlastPowerLoss = 0.12f, BlastShape = "disc", BlastVerticalRadius = 1.5f },
            ["bomb-bundle-steel"] = new() { FuseSeconds = 4.0f, BlastRadius = 8.0f, RockBlastRadius = 8.0f, OreBlastRadius = 8.0f, EntityBlastRadius = 7.0f, OreDestroyChance = 0.2f, CrystalDestroyChance = 0.4f, BlastDropoff = 0.85f, BlastPowerLoss = 0.14f, BlastShape = "disc", BlastVerticalRadius = 1.5f },
            ["bomb-bundle-titanium"] = new() { FuseSeconds = 4.0f, BlastRadius = 8.0f, RockBlastRadius = 8.0f, OreBlastRadius = 8.0f, EntityBlastRadius = 7.0f, OreDestroyChance = 0.15f, CrystalDestroyChance = 0.3f, BlastDropoff = 0.8f, BlastPowerLoss = 0.16f, BlastShape = "disc", BlastVerticalRadius = 1.5f },

            ["blasting-charge"] = new() { FuseSeconds = 1.0f, BlastRadius = 4.0f, RockBlastRadius = 4.0f, OreBlastRadius = 4.0f, EntityBlastRadius = 3.4f, OreDestroyChance = 0.05f, CrystalDestroyChance = 0.1f, BlastDropoff = 1.05f, BlastPowerLoss = 0.05f, InwardWidth = 3, InwardDepth = 5, OutwardDepth = 1, InwardLinkRange = 7 },

            ["bomb-fisherman"] = new() { FuseSeconds = 4.0f, BlastRadius = 0.0f, RockBlastRadius = 0.0f, OreBlastRadius = 0.0f, EntityBlastRadius = 10.0f, OreDestroyChance = 0.0f, CrystalDestroyChance = 0.0f, BlastDropoff = 1.05f, BlastPowerLoss = 0.05f, EntityDamage = 24.0f, FisherSurfaceDestroyChance = 1.0f }
        };
    }
}

public class BombBalanceConfig
{
    public float? FuseSeconds { get; set; }
    public float? BlastRadius { get; set; }
    public float? RockBlastRadius { get; set; }
    public float? OreBlastRadius { get; set; }
    public float? EntityBlastRadius { get; set; }
    public float? OreDestroyChance { get; set; }
    public float? CrystalDestroyChance { get; set; }
    public float? BlastDropoff { get; set; }
    public float? BlastPowerLoss { get; set; }
    public string? BlastShape { get; set; }
    public float? BlastVerticalRadius { get; set; }
    public float? EntityDamage { get; set; }
    public float? FisherSurfaceDestroyChance { get; set; }
    public int? InwardWidth { get; set; }
    public int? InwardDepth { get; set; }
    public int? OutwardDepth { get; set; }
    public int? InwardLinkRange { get; set; }
}
