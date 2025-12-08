using System;
using System.Collections.Generic;
using System.IO;

namespace SamSWAT.HeliCrash.ArysReloaded.Utils;

internal static class LocalizationService
{
    private static Dictionary<string, string> s_mappings;

    public static void LoadMappings(string locale)
    {
        if (s_mappings != null)
        {
            return;
        }

        string path = Path.Combine(HeliCrashPlugin.Directory, "LocalizationMappings.jsonc");

        var locales = HeliCrashPlugin.LoadJson<Dictionary<string, Dictionary<string, string>>>(
            path
        );

        s_mappings = locales[locale];
    }

    public static string GetString(string key)
    {
        if (s_mappings == null)
        {
            throw new InvalidOperationException(
                "[SamSWAT.HeliCrash.ArysReloaded] Localization mappings not yet loaded! Load it first with LocalizationService.LoadMappings()"
            );
        }

        return s_mappings[key];
    }
}
