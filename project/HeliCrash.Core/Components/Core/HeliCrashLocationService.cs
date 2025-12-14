using System;
using System.IO;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Models;
using SamSWAT.HeliCrash.ArysReloaded.Utils;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class HeliCrashLocationService
{
    private readonly LocationsConfig _locationsConfig;

    public HeliCrashLocationService()
    {
        string crashSitesJsonPath = Path.Combine(FileUtil.Directory, "HeliCrashLocations.json");
        _locationsConfig = FileUtil.LoadJson<LocationsConfig>(crashSitesJsonPath);
    }

    public LocationList GetCrashLocations(string map)
    {
        return map.ToLower() switch
        {
            "bigmap" => _locationsConfig.Customs,
            "interchange" => _locationsConfig.Interchange,
            "rezervbase" => _locationsConfig.Rezerv,
            "shoreline" => _locationsConfig.Shoreline,
            "woods" => _locationsConfig.Woods,
            "lighthouse" => _locationsConfig.Lighthouse,
            "tarkovstreets" => _locationsConfig.StreetsOfTarkov,
            "sandbox" => _locationsConfig.GroundZero,
            "develop" => _locationsConfig.Develop,
            _ => throw new NullReferenceException(
                $"Invalid map or no crash location data for map '{map}'!"
            ),
        };
    }
}
