using System;
using System.IO;
using System.Threading;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Models;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using UnityEngine;
using UnityEngine.AI;
using VContainer.Unity;
using Logger = SamSWAT.HeliCrash.ArysReloaded.Utils.Logger;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class HeliCrashSpawner(
    ConfigurationService configService,
    Logger logger,
    HeliCrashLocationService locationService,
    LootContainerFactory lootContainerFactory
) : IAsyncStartable
{
    private GameObject _heliPrefab;

    public async UniTask StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (configService.LoggingEnabled.Value)
            {
                logger.LogInfo("Spawning heli crash site(s)");
            }

            string heliBundlePath = Path.Combine(
                FileUtil.Directory,
                "sikorsky_uh60_blackhawk.bundle"
            );
            _heliPrefab = await LoadPrefabAsync(heliBundlePath, cancellationToken);

            await SpawnCrashSite(
                Singleton<GameWorld>.Instance.MainPlayer.Location,
                cancellationToken
            );
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"Failed to spawn heli crash site(s): {ex.Message}\n{ex.StackTrace}");
        }
    }

    private async UniTask SpawnCrashSite(string map, CancellationToken cancellationToken)
    {
        LocationList crashLocations = locationService.GetCrashLocations(map);
        if (crashLocations == null)
        {
            throw new NullReferenceException(
                "Invalid map or crash location data, aborting heli crash initialization!"
            );
        }

        if (configService.SpawnAllCrashSites.Value)
        {
            await CreateAllCrashSites(crashLocations, cancellationToken);
        }
        else
        {
            Location chosenLocation = crashLocations.SelectRandom();

            bool spawnWithLoot =
                !chosenLocation.Unreachable
                && BlessRNG.RngBool(configService.CrashHasLootChance.Value);

            await CreateCrashSite(
                chosenLocation,
                spawnWithLoot,
                cancellationToken: cancellationToken
            );
        }
    }

    private async UniTask CreateCrashSite(
        Location location,
        bool withLoot = false,
        bool carveMesh = true,
        CancellationToken cancellationToken = default
    )
    {
        AsyncInstantiateOperation<GameObject> asyncOperation = UnityEngine.Object.InstantiateAsync(
            _heliPrefab,
            location.Position,
            Quaternion.Euler(location.Rotation)
        );

        while (!asyncOperation.isDone)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(cancellationToken);
        }

        GameObject choppa = asyncOperation.Result[0];

        if (carveMesh)
        {
            CarveNavMesh(choppa);
        }

        var container = choppa.GetComponentInChildren<EFT.Interactive.LootableContainer>();
        if (withLoot)
        {
            await lootContainerFactory.CreateContainer(
                container,
                cancellationToken: cancellationToken
            );
        }
        else
        {
            // Disable the container game object
            container.transform.parent.gameObject.SetActive(false);
        }

        if (configService.LoggingEnabled.Value)
        {
            logger.LogWarning($"Heli crash site spawned at {location.Position.ToString()}");
        }

        choppa.SetActive(true);
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

    private async UniTask CreateAllCrashSites(
        LocationList heliLocations,
        CancellationToken cancellationToken = default
    )
    {
        AsyncInstantiateOperation<GameObject> asyncOperation = UnityEngine.Object.InstantiateAsync(
            _heliPrefab,
            heliLocations.Count,
            heliLocations.Positions,
            heliLocations.Rotations
        );

        while (!asyncOperation.isDone)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // TODO: Update loading screen status with progress
            await UniTask.Yield(cancellationToken);
        }

        GameObject[] choppas = asyncOperation.Result;
        for (var i = 0; i < choppas.Length; i++)
        {
            choppas[i].SetActive(true);
        }

        logger.LogInfo("Successfully spawned all heli crash sites");
    }

    private async UniTask<GameObject> LoadPrefabAsync(
        string bundlePath,
        CancellationToken cancellationToken
    )
    {
        AssetBundleCreateRequest bundleLoadRequest = AssetBundle.LoadFromFileAsync(bundlePath);
        while (!bundleLoadRequest.isDone)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(cancellationToken);
        }

        AssetBundle bundle = bundleLoadRequest.assetBundle;
        if (bundle == null)
        {
            logger.LogError("Failed to load UH-60 Blackhawk bundle");
            return null;
        }

        AssetBundleRequest assetLoadRequest = bundle.LoadAllAssetsAsync<GameObject>();
        while (!assetLoadRequest.isDone)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(cancellationToken);
        }

        var requestedGo = (GameObject)assetLoadRequest.allAssets[0];
        if (requestedGo == null)
        {
            logger.LogError("Failed to load UH-60 Blackhawk asset");
            return null;
        }

        requestedGo.SetActive(false);
        bundle.Unload(false);

        return requestedGo;
    }
}
