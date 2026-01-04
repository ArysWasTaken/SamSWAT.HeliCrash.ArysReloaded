using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using Comfort.Common;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using SamSWAT.HeliCrash.ArysReloaded.Fika.Bootstrapper;
using SamSWAT.HeliCrash.ArysReloaded.Fika.Models;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika;

[BepInPlugin(
    "com.samswat.helicrash.arysreloaded.fika",
    "SamSWAT's HeliCrash: Arys Reloaded - Fika Sync",
    ModMetadata.VERSION
)]
[BepInDependency("com.SPT.core", ModMetadata.TARGET_SPT_VERSION)]
[BepInDependency("com.arys.unitytoolkit", "2.0.1")]
[BepInDependency("com.fika.core")]
public class HeliCrashFikaPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        ValidateCoreModVersion();

        HeliCrashPlugin.PostAwake += Initialize;
    }

    private static void ValidateCoreModVersion()
    {
        string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        string[] assemblies = Directory.GetFiles(directory, "*.dll");

        string coreModPath = assemblies.FirstOrDefault(assembly =>
            assembly.Contains("SamSWAT.HeliCrash.ArysReloaded.Core")
        );

        if (coreModPath == null)
        {
            throw new FileNotFoundException("Could not find HeliCrash Core mod! Disabling mod.");
        }

        var coreModVersion = new Version(FileVersionInfo.GetVersionInfo(coreModPath).FileVersion);
        var targetCoreModVersion = new Version(ModMetadata.TARGET_CORE_MOD_VERSION);

        if (coreModVersion < targetCoreModVersion)
        {
            throw new Exception(
                "Outdated HeliCrash Core mod version! Please update your HeliCrash Core mod! Disabling mod."
            );
        }
    }

    private static void Initialize()
    {
        new OverrideRaidLifetimeScopePatch().Enable();
        new HeadlessRaidLoadScreenPatch().Enable();

        FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(
            OnFikaNetworkManagerCreated
        );
    }

    private static void OnFikaNetworkManagerCreated(FikaNetworkManagerCreatedEvent @event)
    {
        switch (@event.Manager)
        {
            case FikaServer server:
#if DEBUG
                FikaGlobals.LogInfo("Registering RequestHeliCrashPacket on Fika Server");
#endif
                server.RegisterPacket<RequestHeliCrashPacket, NetPeer>(OnHeliCrashRequest);
                break;
            case FikaClient client:
#if DEBUG
                FikaGlobals.LogInfo("Registering RequestHeliCrashPacket on Fika Client");
#endif
                client.RegisterPacket<RequestHeliCrashPacket>(OnHeliCrashResponse);
                break;
        }
    }

    private static void OnHeliCrashRequest(RequestHeliCrashPacket packet, NetPeer peer)
    {
#if DEBUG
        FikaGlobals.LogInfo(
            $"Received HeliCrash request from Fika Client {peer.Id.ToString()}. Handling request..."
        );
#endif
        packet.HandleRequest(peer, Singleton<FikaServer>.Instance);
    }

    private static void OnHeliCrashResponse(RequestHeliCrashPacket packet)
    {
#if DEBUG
        FikaGlobals.LogInfo("Received HeliCrash response from Fika Server. Handling response...");
#endif
        packet.HandleResponse();
    }
}
