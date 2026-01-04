using System;
using System.Threading;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Fika.Models;
using SamSWAT.HeliCrash.ArysReloaded.Utils;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika;

[UsedImplicitly]
public class ServerHeliCrashSpawner : LocalHeliCrashSpawner
{
    private readonly ConfigurationService _configService;
    private readonly Logger _logger;

    private bool _finishedSpawning;

    public ServerHeliCrashSpawner(
        ConfigurationService configService,
        Logger logger,
        HeliCrashLocationService locationService,
        LootContainerFactory lootContainerFactory
    )
        : base(configService, logger, locationService, lootContainerFactory)
    {
        _configService = configService;
        _logger = logger;

        EventDispatcher<HeliCrashRequestEvent>.Subscribe(OnReceiveRequest);
    }

    public override void Dispose()
    {
        EventDispatcher<HeliCrashRequestEvent>.UnsubscribeAll();
        base.Dispose();
    }

    protected override async UniTask SpawnCrashSite(CancellationToken cancellationToken = default)
    {
        await base.SpawnCrashSite(cancellationToken);
        _finishedSpawning = true;
    }

    private void OnReceiveRequest(ref HeliCrashRequestEvent requestEvent)
    {
        InvokeAfterHeliCrashSpawned(requestEvent.callback).Forget();
        return;

        async UniTaskVoid InvokeAfterHeliCrashSpawned(
            Action<ServerHeliCrashSpawner, Logger> callback
        )
        {
            CancellationToken cancellationToken = Singleton<GameWorld>
                .Instance
                .destroyCancellationToken;

            Logger logger = _configService.LoggingEnabled.Value ? _logger : null;

            while (!ShouldSpawn.HasValue)
            {
                await UniTask.Yield(cancellationToken);
            }

            if (!ShouldSpawn.Value)
            {
                callback?.Invoke(this, logger);
                return;
            }

            while (!_finishedSpawning)
            {
                await UniTask.Yield(cancellationToken);
            }

            callback?.Invoke(this, logger);
        }
    }
}
