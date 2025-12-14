using System.Reflection;
using BepInEx;
using CustomPlayerLoopSystem;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SamSWAT.HeliCrash.ArysReloaded;

public class InitializeApplicationLifetimeScopePatch : ModulePatch
{
    private static BaseUnityPlugin s_unityPlugin;
    private static GameObject s_bepinexManager;

    public InitializeApplicationLifetimeScopePatch(
        BaseUnityPlugin unityPlugin,
        GameObject bepinexManager
    )
    {
        s_unityPlugin = unityPlugin;
        s_bepinexManager = bepinexManager;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(CustomPlayerLoopSystemsInjector),
            nameof(CustomPlayerLoopSystemsInjector.Injection)
        );
    }

    [PatchPostfix]
    private static void PatchPostfix()
    {
        var go = new GameObject("HeliCrash_AppLifetimeScope");
        go.transform.SetParent(s_bepinexManager.transform);
        var appLifetimeScope = go.AddComponent<ApplicationLifetimeScope>();
        appLifetimeScope.Initialize(s_unityPlugin, Logger);
        appLifetimeScope.Build();
    }
}
