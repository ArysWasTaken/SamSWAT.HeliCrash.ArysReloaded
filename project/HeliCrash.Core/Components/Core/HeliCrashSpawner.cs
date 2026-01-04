using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Models;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using UnityEngine;
using ZLinq;
using Logger = SamSWAT.HeliCrash.ArysReloaded.Utils.Logger;
using Object = UnityEngine.Object;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public abstract class HeliCrashSpawner(
    ConfigurationService configService,
    Logger logger,
    HeliCrashLocationService locationService
) : IDisposable
{
    protected GameObject heliPrefab;
    private AssetBundle _heliBundle;

    public bool? ShouldSpawn { get; private set; }

    public async UniTask StartAsync(Task loadScreenTask)
    {
        try
        {
            await loadScreenTask;

            ShouldSpawn = await ShouldSpawnCrashSite();
            if (!ShouldSpawn.HasValue || (ShouldSpawn.HasValue && !ShouldSpawn.Value))
            {
                return;
            }

            if (configService.LoggingEnabled.Value)
            {
                logger.LogInfo("Spawning heli crash site");
            }

            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            CancellationToken cancellationToken = gameWorld.destroyCancellationToken;

            await UniTask.SwitchToMainThread(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            string heliBundlePath = Path.Combine(
                FileUtil.Directory,
                "sikorsky_uh60_blackhawk.bundle"
            );
            heliPrefab = await LoadPrefabAsync(heliBundlePath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            await SpawnCrashSite(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"Failed to spawn heli crash site(s): {ex.Message}\n{ex.StackTrace}");
        }
    }

    public virtual void Dispose()
    {
        if (_heliBundle != null)
        {
            if (configService.LoggingEnabled.Value)
            {
                logger.LogInfo("Disposing HeliCrash bundle");
            }
            _heliBundle.Unload(true);
        }
    }

    protected abstract UniTask SpawnCrashSite(CancellationToken cancellationToken = default);

    protected virtual UniTask<bool> ShouldSpawnCrashSite(
        CancellationToken cancellationToken = default
    )
    {
        if (!IsCrashAvailable())
        {
            return UniTask.FromResult(false);
        }

        return UniTask.FromResult(
            configService.SpawnAllCrashSites.Value
                || BlessRNG.RngBool(configService.HeliCrashChance.Value)
        );
    }

    protected async UniTask<GameObject> InstantiateCrashSiteObject(
        Vector3 position = default,
        Vector3 rotation = default,
        CancellationToken cancellationToken = default
    )
    {
        AsyncInstantiateOperation<GameObject> asyncOperation = Object.InstantiateAsync(
            heliPrefab,
            position,
            Quaternion.Euler(rotation)
        );

        while (!asyncOperation.isDone)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(cancellationToken);
        }

        GameObject choppa = asyncOperation.Result[0];
        return choppa;
    }

    private bool IsCrashAvailable()
    {
        string location = Singleton<GameWorld>.Instance.LocationId;
        LocationList crashLocationList = locationService.GetCrashLocations(location);

        if (crashLocationList == null)
        {
            if (configService.LoggingEnabled.Value)
            {
                logger.LogInfo(
                    $"HeliCrashLocations.json does not contain data on map '{location}'. Aborting spawn of heli crash site!"
                );
            }

            return false;
        }

        return crashLocationList.AsValueEnumerable().Any();
    }

    private async UniTask<GameObject> LoadPrefabAsync(
        string bundlePath,
        CancellationToken cancellationToken = default
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

        _heliBundle = bundle;
        requestedGo.SetActive(false);

        return requestedGo;
    }
}
