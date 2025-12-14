using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using UnityEngine;
using Logger = SamSWAT.HeliCrash.ArysReloaded.Utils.Logger;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public abstract class HeliCrashSpawner(ConfigurationService configService, Logger logger)
    : IDisposable
{
    protected GameObject heliPrefab;
    private AssetBundle _heliBundle;

    public async UniTask StartAsync(Task loadScreenTask)
    {
        try
        {
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

            await SpawnCrashSite(gameWorld, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            await loadScreenTask;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError($"Failed to spawn heli crash site(s): {ex.Message}\n{ex.StackTrace}");
        }
    }

    protected abstract UniTask SpawnCrashSite(
        GameWorld gameWorld,
        CancellationToken cancellationToken = default
    );

    public void Dispose()
    {
        if (_heliBundle != null)
        {
            _heliBundle.Unload(true);
        }
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
