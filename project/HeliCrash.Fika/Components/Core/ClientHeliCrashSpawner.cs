using System;
using System.Threading;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using EFT.Interactive;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Fika.Events;
using SamSWAT.HeliCrash.ArysReloaded.Fika.Models;
using UnityEngine;
using Logger = SamSWAT.HeliCrash.ArysReloaded.Utils.Logger;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika;

[UsedImplicitly]
public sealed class ClientHeliCrashSpawner : HeliCrashSpawner
{
    private readonly ConfigurationService _configService;
    private readonly Logger _logger;
    private readonly LootContainerFactory _lootContainerFactory;

    private HeliCrashDataPacket _cachedPacket;

    public ClientHeliCrashSpawner(
        ConfigurationService configService,
        Logger logger,
        HeliCrashLocationService locationService,
        LootContainerFactory lootContainerFactory
    )
        : base(configService, logger, locationService)
    {
        _configService = configService;
        _logger = logger;
        _lootContainerFactory = lootContainerFactory;
    }

    protected override async UniTask<bool> ShouldSpawnCrashSite(
        CancellationToken cancellationToken = default
    )
    {
        using var requestHandler = new RequestHandler(_configService, _logger);

        _cachedPacket = await requestHandler.HandleRequest(
            timeoutSeconds: 300,
            cancellationToken: cancellationToken
        );

        return _cachedPacket.shouldSpawn;
    }

    protected override async UniTask SpawnCrashSite(CancellationToken cancellationToken = default)
    {
        GameObject choppa = await InstantiateCrashSiteObject(cancellationToken: cancellationToken);

        Door[] doors = choppa.GetComponentsInChildren<Door>();

        for (var i = 0; i < doors.Length; i++)
        {
            doors[i].NetId = _cachedPacket.doorNetIds[i];
            Singleton<GameWorld>.Instance.RegisterWorldInteractionObject(doors[i]);
        }

        var container = choppa.GetComponentInChildren<LootableContainer>();

        if (_cachedPacket.hasLoot)
        {
            container.NetId = _cachedPacket.containerNetId;

            await _lootContainerFactory.CreateContainer(
                container,
                _cachedPacket.containerItem,
                cancellationToken
            );
        }
        else
        {
            // Disable the container game object
            container.transform.parent.gameObject.SetActive(false);
        }

        choppa.transform.SetPositionAndRotation(
            _cachedPacket.position,
            Quaternion.Euler(_cachedPacket.rotation)
        );

        if (_configService.LoggingEnabled.Value)
        {
            _logger.LogWarning($"Heli crash site spawned at {_cachedPacket.position.ToString()}");
        }

        choppa.SetActive(true);
    }

    private class RequestHandler : IDisposable
    {
        private readonly ConfigurationService _configService;
        private readonly Logger _logger;

        private UniTaskCompletionSource<HeliCrashDataPacket> _tcs;

        public RequestHandler(ConfigurationService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;

            _tcs = new UniTaskCompletionSource<HeliCrashDataPacket>();

            EventDispatcher<HeliCrashResponseEvent>.Subscribe(OnReceiveResponse);
        }

        public void Dispose()
        {
            EventDispatcher<HeliCrashResponseEvent>.Unsubscribe(OnReceiveResponse);

            UniTaskCompletionSource<HeliCrashDataPacket> tcs = Interlocked.Exchange(ref _tcs, null);

            tcs?.TrySetCanceled();
        }

        public async UniTask<HeliCrashDataPacket> HandleRequest(
            int timeoutSeconds,
            CancellationToken cancellationToken = default
        )
        {
            UniTaskCompletionSource<HeliCrashDataPacket> tcs = _tcs;

            using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );
            using IDisposable timeoutTimer = requestCts.CancelAfterSlim(
                TimeSpan.FromSeconds(timeoutSeconds),
                DelayType.Realtime
            );

            var requestPacket = new HeliCrashDataPacket();

            while (!requestCts.Token.IsCancellationRequested)
            {
                if (_configService.LoggingEnabled.Value)
                {
                    _logger.LogInfo("Sending HeliCrash request to Fika Server...");
                }

                Singleton<FikaClient>.Instance.SendData(
                    ref requestPacket,
                    DeliveryMethod.ReliableOrdered
                );

                (bool isRetryTimeout, HeliCrashDataPacket responsePacket) =
                    await tcs.Task.TimeoutWithoutException(
                        TimeSpan.FromSeconds(5),
                        DelayType.Realtime
                    );

                requestCts.Token.ThrowIfCancellationRequested();

                if (!isRetryTimeout)
                {
                    return responsePacket;
                }
            }

            throw new TimeoutException(
                "Timed out while waiting for HeliCrash request from Fika Server! No helicopter crash site will be spawned!"
            );
        }

        private void OnReceiveResponse(ref HeliCrashResponseEvent responseEvent)
        {
            UniTaskCompletionSource<HeliCrashDataPacket> tcs = _tcs;

            if (tcs == null)
            {
                throw new Exception(
                    "Received HeliCrash response from Fika Server but the requesting CompletionSource is now invalid! Please report this error to the mod developer!"
                );
            }

            if (!tcs.TrySetResult(responseEvent.packet))
            {
                tcs.TrySetException(
                    new Exception(
                        "Failed to set result for HeliCrash CompletionSource! Setting exception for CompletionSource!"
                    )
                );
            }

            if (_configService.LoggingEnabled.Value)
            {
                _logger.LogInfo(
                    $"Received HeliCrash response from Fika Server: ({responseEvent.packet})"
                );
            }
        }
    }
}
