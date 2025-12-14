using System;
using BepInEx;
using BepInEx.Bootstrap;

namespace SamSWAT.HeliCrash.ArysReloaded;

[BepInPlugin(
    "com.samswat.helicrash.arysreloaded",
    "SamSWAT's HeliCrash: Arys Reloaded - Core",
    ModMetadata.VERSION
)]
[BepInDependency("com.SPT.core", ModMetadata.TARGET_SPT_VERSION)]
[BepInDependency("com.arys.unitytoolkit", "2.0.1")]
[BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(
    "com.samswat.helicrash.arysreloaded.fika",
    BepInDependency.DependencyFlags.SoftDependency
)]
public class HeliCrashPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        DetectFikaAddon();

        new InitializeApplicationLifetimeScopePatch(this, gameObject).Enable();

        PostAwake?.Invoke();
        PostAwake = null;
    }

    private static void DetectFikaAddon()
    {
        bool fikaDetected = Chainloader.PluginInfos.ContainsKey("com.fika.core");
        bool fikaAddonDetected = Chainloader.PluginInfos.ContainsKey(
            "com.samswat.helicrash.arysreloaded.fika"
        );
        if (fikaDetected && !fikaAddonDetected)
        {
            throw new DllNotFoundException(
                "Fika is detected but HeliCrash's Fika Addon is not installed. Please install the Fika Addon!"
            );
        }
    }

    public static event Action PostAwake;
}
