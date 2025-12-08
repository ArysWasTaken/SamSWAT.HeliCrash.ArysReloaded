using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using SamSWAT.HeliCrash.ArysReloaded.Models;
using SamSWAT.HeliCrash.ArysReloaded.Utils;

namespace SamSWAT.HeliCrash.ArysReloaded;

[BepInPlugin(
    "com.SamSWAT.HeliCrash.ArysReloaded",
    "SamSWAT's HeliCrash: Arys Reloaded - Core",
    ModMetadata.VERSION
)]
[BepInDependency("com.SPT.core", ModMetadata.TARGET_SPT_VERSION)]
[BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
public class HeliCrashPlugin : BaseUnityPlugin
{
    internal static HeliCrashPlugin Instance { get; private set; }

    internal static ConfigEntry<bool> LoggingEnabled { get; private set; }
    internal static ConfigEntry<bool> SpawnAllCrashSites { get; private set; }
    internal static ConfigEntry<int> HeliCrashChance { get; private set; }
    internal static ConfigEntry<int> CrashHasLootChance { get; private set; }

    public static bool FikaEnabled { get; private set; }
    internal static string Directory { get; private set; }
    internal static HeliCrashLocations HeliCrashLocations { get; private set; }

    public static event Action AwakeEvent;

    private void Awake()
    {
        Instance = this;

        Utils.Logger.Initialize(Logger);
        Directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        string crashSitesJsonPath = Path.Combine(Directory, "HeliCrashLocations.json");
        HeliCrashLocations = JsonUtil.LoadJson<HeliCrashLocations>(crashSitesJsonPath);

        FikaEnabled = Chainloader.PluginInfos.ContainsKey("com.fika.core");
        if (!FikaEnabled) { }

        AwakeEvent?.Invoke();
    }

    internal static void SetupDebugConfigBindings()
    {
        LoggingEnabled = Instance.Config.Bind(
            LocalizationService.GetString("debugSettings"),
            LocalizationService.GetString("enableLogging"),
            false
        );

        SpawnAllCrashSites = Instance.Config.Bind(
            LocalizationService.GetString("debugSettings"),
            LocalizationService.GetString("spawnAllCrashSites"),
            false,
            LocalizationService.GetString("spawnAllCrashSites_desc")
        );
    }

    internal static void SetupMainConfigBindings()
    {
        HeliCrashChance = Instance.Config.Bind(
            LocalizationService.GetString("mainSettings"),
            LocalizationService.GetString("crashSiteSpawnChance"),
            10,
            new ConfigDescription(
                LocalizationService.GetString("crashSiteSpawnChance_desc"),
                new AcceptableValueRange<int>(0, 100)
            )
        );

        CrashHasLootChance = Instance.Config.Bind(
            LocalizationService.GetString("mainSettings"),
            LocalizationService.GetString("crashHasLootChance"),
            100,
            new ConfigDescription(
                LocalizationService.GetString("crashHasLootChance_desc"),
                new AcceptableValueRange<int>(0, 100)
            )
        );
    }

    internal static T LoadJson<T>(string path)
    {
        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json);
    }
}
