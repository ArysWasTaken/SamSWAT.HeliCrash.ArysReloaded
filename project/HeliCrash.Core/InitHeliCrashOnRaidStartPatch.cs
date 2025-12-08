using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace SamSWAT.HeliCrash.ArysReloaded;

internal class InitHeliCrashOnRaidStartPatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
	}
	
	[PatchPostfix]
	private static void PatchPostfix(GameWorld __instance)
	{
		HeliCrashManager.TryCreate(__instance);
	}
}