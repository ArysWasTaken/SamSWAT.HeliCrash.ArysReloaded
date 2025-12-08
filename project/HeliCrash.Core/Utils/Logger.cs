using BepInEx.Logging;
using EFT.UI;

namespace SamSWAT.HeliCrash.ArysReloaded.Utils;

internal static class Logger
{
	private static ManualLogSource s_logSource;
	
	public static void Initialize(ManualLogSource logSource)
	{
		s_logSource = logSource;
	}
	
	public static void LogInfo(string message)
	{
		s_logSource.LogInfo(message);
		if (HeliCrashPlugin.LoggingEnabled.Value)
		{
			ConsoleScreen.Log(message);
		}
	}
	
	public static void LogWarning(string message)
	{
		s_logSource.LogWarning(message);
		if (HeliCrashPlugin.LoggingEnabled.Value)
		{
			ConsoleScreen.LogWarning(message);
		}
	}
	
	public static void LogError(string message)
	{
		s_logSource.LogError(message);
		if (HeliCrashPlugin.LoggingEnabled.Value)
		{
			ConsoleScreen.LogError(message);
		}
	}
}