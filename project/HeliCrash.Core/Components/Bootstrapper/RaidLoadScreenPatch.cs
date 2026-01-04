using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using HarmonyLib;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using SPT.Reflection.Patching;
using VContainer.Unity;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class RaidLoadScreenPatch : ModulePatch
{
    private static ConfigurationService s_configService;
    private static Logger s_logger;
    private static RaidLifetimeScopeController s_raidLifetimeScopeController;

    public RaidLoadScreenPatch(
        ConfigurationService configService,
        Logger logger,
        RaidLifetimeScopeController raidLifetimeScopeController
    )
    {
        s_configService = configService;
        s_logger = logger;
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
        if (Singleton<GameWorld>.Instance is HideoutGameWorld)
        {
            return;
        }

        if (s_configService.LoggingEnabled.Value)
        {
            s_logger.LogInfo("Creating HeliCrash RaidLifetimeScope...");
        }

        LifetimeScope scope = s_raidLifetimeScopeController.CreateScope();
        scope.Build();

        var spawner = (HeliCrashSpawner)scope.Container.Resolve(typeof(HeliCrashSpawner));
        __result = spawner.StartAsync(__result).AsTask();
    }
}
