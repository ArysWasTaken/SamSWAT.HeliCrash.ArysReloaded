using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using EFT.Airdrop;
using SamSWAT.HeliCrash.ArysReloaded.Models;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace SamSWAT.HeliCrash.ArysReloaded;

public class HeliCrashManager : MonoBehaviourSingleton<HeliCrashManager>
{
    private LootContainerFactory _lootContainerFactory;
    private GameObject _heliPrefab;

    public static void TryCreate(GameWorld gameWorld)
    {
        string location = gameWorld.MainPlayer.Location;
        bool crashAvailable =
            location.ToLower() == "sandbox" || LocationScene.GetAll<AirdropPoint>().Any();
        bool shouldSpawnCrash =
            HeliCrashPlugin.SpawnAllCrashSites.Value
            || BlessRNG.RngBool(HeliCrashPlugin.HeliCrashChance.Value);

        if (crashAvailable && shouldSpawnCrash)
        {
            gameWorld.gameObject.AddComponent<HeliCrashManager>();
        }
    }

    private async UniTaskVoid Start()
    {
        try
        {
            await Initialize(Singleton<GameWorld>.Instance.MainPlayer.Location);
        }
        catch (Exception ex)
        {
            Utils.Logger.LogError(
                $"Failed to initialize heli crash site: {ex.Message}\n{ex.StackTrace}"
            );
        }
    }

    private async UniTask Initialize(string location)
    {
        _lootContainerFactory = new LootContainerFactory();

        List<Location> heliLocations = HeliCrashLocations.GetCrashSiteLocations(location);
        if (heliLocations == null)
        {
            Utils.Logger.LogError(
                "Invalid map or crash location data, aborting heli crash initialization!"
            );
            return;
        }

        string heliBundlePath = Path.Combine(
            HeliCrashPlugin.Directory,
            "sikorsky_uh60_blackhawk.bundle"
        );
        _heliPrefab = await LoadPrefabAsync(heliBundlePath);

        if (HeliCrashPlugin.SpawnAllCrashSites.Value)
        {
            // Enable crash site objects in batches to avoid freezes/crashing
            await BatchEnable(heliLocations);
        }
        else
        {
            Location chosenLocation = heliLocations.SelectRandom();
            bool spawnWithLoot =
                !chosenLocation.Unreachable
                && BlessRNG.RngBool(HeliCrashPlugin.CrashHasLootChance.Value);

            GameObject heli = await CreateCrashSite(chosenLocation, spawnWithLoot);
            heli.SetActive(true);
        }
    }

    private async UniTask<GameObject> CreateCrashSite(
        Location location,
        bool withLoot = false,
        bool carveMesh = true
    )
    {
        GameObject choppa = Instantiate(
            _heliPrefab,
            location.Position,
            Quaternion.Euler(location.Rotation)
        );

        if (carveMesh)
        {
            CarveNavMesh(choppa);
        }

        var container = choppa.GetComponentInChildren<EFT.Interactive.LootableContainer>();
        if (withLoot)
        {
            await _lootContainerFactory.CreateContainer(container);
        }
        else
        {
            // Disable the container game object
            container.transform.parent.gameObject.SetActive(false);
        }

        if (HeliCrashPlugin.LoggingEnabled.Value)
        {
            var logMessage = $"Heli crash site spawned at {location.Position.ToString()}";
            Utils.Logger.LogWarning(logMessage);
        }

        return choppa;
    }

    private static void CarveNavMesh(GameObject choppa)
    {
        var navMeshObstacle = choppa
            .transform.GetChild(0)
            .GetChild(5)
            .gameObject.AddComponent<NavMeshObstacle>();

        navMeshObstacle.shape = NavMeshObstacleShape.Box;
        navMeshObstacle.center = new Vector3(1.99000001f, 2.23000002f, -0.75999999f);
        navMeshObstacle.size = new Vector3(4.01499987f, 2.45000005f, 10f);
        navMeshObstacle.carving = true;
    }

    private async UniTask BatchEnable(List<Location> heliLocations, int batchSize = 10)
    {
        int locationCount = heliLocations.Count;
        var count = 0;

        for (var i = 0; i < locationCount; i++)
        {
            Location location = heliLocations[i];

            GameObject choppa = Instantiate(
                _heliPrefab,
                location.Position,
                Quaternion.Euler(location.Rotation)
            );

            choppa.SetActive(true);

            if (i >= locationCount - 1)
            {
                break;
            }

            if (++count >= batchSize)
            {
                count = 0;
                await UniTask.NextFrame();
            }
        }
    }

    private static async UniTask<GameObject> LoadPrefabAsync(string bundlePath)
    {
        AssetBundleCreateRequest bundleLoadRequest = AssetBundle.LoadFromFileAsync(bundlePath);
        while (!bundleLoadRequest.isDone)
        {
            await UniTask.Yield();
        }

        AssetBundle bundle = bundleLoadRequest.assetBundle;
        if (bundle == null)
        {
            Utils.Logger.LogError("Failed to load UH-60 Blackhawk bundle");
            return null;
        }

        AssetBundleRequest assetLoadRequest = bundle.LoadAllAssetsAsync<GameObject>();
        while (!assetLoadRequest.isDone)
        {
            await UniTask.Yield();
        }

        var requestedGo = (GameObject)assetLoadRequest.allAssets[0];
        if (requestedGo == null)
        {
            Utils.Logger.LogError("Failed to load UH-60 Blackhawk asset");
            return null;
        }

        requestedGo.SetActive(false);
        bundle.Unload(false);

        return requestedGo;
    }
}
