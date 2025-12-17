using System;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using HarmonyLib;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Models;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using SPT.Reflection.Patching;
using VContainer.Unity;
using ZLinq;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class RaidLoadScreenPatch : ModulePatch
{
    private static ConfigurationService s_configService;
    private static Logger s_logger;
    private static RaidLifetimeScopeController s_raidLifetimeScopeController;
    private static HeliCrashLocationService s_crashLocationService;

    public RaidLoadScreenPatch(
        ConfigurationService configService,
        Logger logger,
        RaidLifetimeScopeController raidLifetimeScopeController,
        HeliCrashLocationService crashLocationService
    )
    {
        s_configService = configService;
        s_logger = logger;
        s_crashLocationService = crashLocationService;
        s_raidLifetimeScopeController = raidLifetimeScopeController;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(BaseLocalGame<EftGamePlayerOwner>),
            nameof(BaseLocalGame<>.method_12)
        );
    }

    [PatchPostfix]
    private static void PatchPostfix(ref Task __result)
    {
        try
        {
            if (Singleton<GameWorld>.Instance is HideoutGameWorld)
            {
                return;
            }

            string location = Singleton<GameWorld>.Instance.LocationId;
            LocationList crashLocationList = s_crashLocationService.GetCrashLocations(location);
            if (crashLocationList == null)
            {
                if (s_configService.LoggingEnabled.Value)
                {
                    s_logger.LogInfo(
                        $"HeliCrashLocations.json does not contain data on map '{location}'. Aborting spawn of heli crash site!"
                    );
                }
                return;
            }

            bool crashAvailable = crashLocationList.AsValueEnumerable().Any();

            bool shouldSpawnCrash =
                s_configService.SpawnAllCrashSites.Value
                || BlessRNG.RngBool(s_configService.HeliCrashChance.Value);

            if (crashAvailable && shouldSpawnCrash)
            {
                if (s_configService.LoggingEnabled.Value)
                {
                    s_logger.LogInfo("Can spawn heli crash site, creating RaidLifetimeScope...");
                }

                LifetimeScope scope = s_raidLifetimeScopeController.CreateScope();
                scope.Build();

                var spawner = (HeliCrashSpawner)scope.Container.Resolve(typeof(HeliCrashSpawner));
                __result = spawner.StartAsync(__result).AsTask();
            }
        }
        catch (Exception ex)
        {
            s_logger.LogError(ex.ToString());
        }
    }
}
