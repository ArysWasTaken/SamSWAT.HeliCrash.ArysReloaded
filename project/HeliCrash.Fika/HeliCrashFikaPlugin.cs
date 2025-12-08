using BepInEx;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using System;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika;

[BepInPlugin("com.SamSWAT.HeliCrash.ArysReloaded.Fika", "SamSWAT's HeliCrash: Arys Reloaded - Fika", "2.3.0")]
[BepInDependency("com.SamSWAT.HeliCrash.ArysReloaded", "2.3.0")]
[BepInDependency("com.fika.core")]
public class HeliCrashFikaPlugin : BaseUnityPlugin
{
	private void Awake()
	{
		FikaEventDispatcher.SubscribeEvent<GameWorldStartedEvent>(OnGameWorldStarted_EventHandler);
		FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(FikaNetworkManagerCreated_EventHandler);
	}
	
	private static async void OnGameWorldStarted_EventHandler(GameWorldStartedEvent e)
	{
		if (FikaBackendUtils.IsClient)
		{
			return;
		}
		
		HeliCrashManager.TryCreate(e.GameWorld);
	}
	
	private static void FikaNetworkManagerCreated_EventHandler(FikaNetworkManagerCreatedEvent e)
	{
		if (FikaBackendUtils.IsServer)
		{
			e.Manager.CoopHandler.
		}
	}
}