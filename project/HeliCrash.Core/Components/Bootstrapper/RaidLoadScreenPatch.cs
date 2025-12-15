using System;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using EFT.Airdrop;
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
            nameof(BaseLocalGame<>.method_7)
        );
    }

    [PatchPostfix]
    private static void PatchPostfix(ref Task __result)
    {
        try
        {
            string location = Singleton<GameWorld>.Instance.LocationId;
            LocationList crashLocationList = s_crashLocationService.GetCrashLocations(location);

            bool crashAvailable =
                crashLocationList.AsValueEnumerable().Any()
                || location.ToLower() == "sandbox"
                || LocationScene.GetAll<AirdropPoint>().AsValueEnumerable().Any();

            bool shouldSpawnCrash =
                s_configService.SpawnAllCrashSites.Value
                || BlessRNG.RngBool(s_configService.HeliCrashChance.Value);

            if (crashAvailable && shouldSpawnCrash)
            {
                if (s_configService.LoggingEnabled.Value)
                {
                    s_logger.LogInfo("Can spawn heli crash site, creating RaidLifetimeScope");
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
