using BepInEx.Configuration;
using JetBrains.Annotations;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class ConfigurationService(ConfigFile pluginConfig, LocalizationService localizationService)
{
    public ConfigEntry<bool> LoggingEnabled { get; private set; }
    public ConfigEntry<bool> SpawnAllCrashSites { get; private set; }
    public ConfigEntry<int> HeliCrashChance { get; private set; }
    public ConfigEntry<int> CrashHasLootChance { get; private set; }

    public void InitializeBindings()
    {
        var order = 0;

        HeliCrashChance ??= pluginConfig.Bind(
            localizationService.Localize("mainSettings"),
            localizationService.Localize("crashSiteSpawnChance"),
            10,
            new ConfigDescription(
                localizationService.Localize("crashSiteSpawnChance_desc"),
                new AcceptableValueRange<int>(0, 100),
                new ConfigurationManagerAttributes { Order = order-- }
            )
        );

        CrashHasLootChance ??= pluginConfig.Bind(
            localizationService.Localize("mainSettings"),
            localizationService.Localize("crashHasLootChance"),
            100,
            new ConfigDescription(
                localizationService.Localize("crashHasLootChance_desc"),
                new AcceptableValueRange<int>(0, 100),
                new ConfigurationManagerAttributes { Order = order-- }
            )
        );

        LoggingEnabled ??= pluginConfig.Bind(
            localizationService.Localize("debugSettings"),
            localizationService.Localize("enableLogging"),
            false,
            new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = order-- })
        );

        SpawnAllCrashSites ??= pluginConfig.Bind(
            localizationService.Localize("debugSettings"),
            localizationService.Localize("spawnAllCrashSites"),
            false,
            new ConfigDescription(
                localizationService.Localize("spawnAllCrashSites_desc"),
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = order-- }
            )
        );
    }
}
