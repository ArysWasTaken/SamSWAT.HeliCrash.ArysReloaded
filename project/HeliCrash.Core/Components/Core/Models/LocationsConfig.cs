using Newtonsoft.Json;

namespace SamSWAT.HeliCrash.ArysReloaded.Models;

[JsonObject]
[method: JsonConstructor]
public class LocationsConfig(
    [JsonProperty("Customs")] LocationList customs,
    [JsonProperty("Woods")] LocationList woods,
    [JsonProperty("Interchange")] LocationList interchange,
    [JsonProperty("Lighthouse")] LocationList lighthouse,
    [JsonProperty("Rezerv")] LocationList reserve,
    [JsonProperty("Shoreline")] LocationList shoreline,
    [JsonProperty("StreetsOfTarkov")] LocationList streets,
    [JsonProperty("GroundZero")] LocationList groundzero,
    [JsonProperty("Develop")] LocationList develop
)
{
    public LocationList Customs { get; } = customs ?? [];
    public LocationList Woods { get; } = woods ?? [];
    public LocationList Interchange { get; } = interchange ?? [];
    public LocationList Lighthouse { get; } = lighthouse ?? [];
    public LocationList Rezerv { get; } = reserve ?? [];
    public LocationList Shoreline { get; } = shoreline ?? [];
    public LocationList StreetsOfTarkov { get; } = streets ?? [];
    public LocationList GroundZero { get; } = groundzero ?? [];
    public LocationList Develop { get; } = develop ?? [];
}
