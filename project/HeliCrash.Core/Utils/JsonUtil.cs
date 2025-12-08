using System.IO;
using Newtonsoft.Json;

namespace SamSWAT.HeliCrash.ArysReloaded.Utils;

internal static class JsonUtil
{
    public static T LoadJson<T>(string path)
    {
        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json);
    }
}
