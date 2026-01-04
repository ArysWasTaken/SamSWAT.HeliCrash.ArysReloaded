using System;
using System.Threading;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using EFT.Interactive;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using JetBrains.Annotations;
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

    private UniTaskCompletionSource<RequestHeliCrashPacket> _currentRequest;
    private RequestHeliCrashPacket _cachedPacket;

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

        EventDispatcher<HeliCrashResponseEvent>.Subscribe(OnReceiveResponse);
    }

    public override void Dispose()
    {
        EventDispatcher<HeliCrashResponseEvent>.UnsubscribeAll();
        base.Dispose();
    }

    protected override async UniTask<bool> ShouldSpawnCrashSite(
        CancellationToken cancellationToken = default
    )
    {
        _cachedPacket = await RequestDataFromServer(cancellationToken);
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

    private async UniTask<RequestHeliCrashPacket> RequestDataFromServer(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _currentRequest = new UniTaskCompletionSource<RequestHeliCrashPacket>();

            var requestPacket = new RequestHeliCrashPacket();

            if (_configService.LoggingEnabled.Value)
            {
                _logger.LogInfo("Sending HeliCrash request to Fika Server...");
            }

            Singleton<FikaClient>.Instance.SendData(
                ref requestPacket,
                DeliveryMethod.ReliableOrdered
            );

            (bool isTimeout, RequestHeliCrashPacket responsePacket) =
                await _currentRequest.Task.TimeoutWithoutException(
                    TimeSpan.FromSeconds(20),
                    DelayType.Realtime,
                    taskCancellationTokenSource: cts
                );

            if (isTimeout)
            {
                var timeoutException = new TimeoutException(
                    "HeliCrash Fika Client request timed out waiting for response from the Fika Server!"
                );
                _currentRequest.TrySetException(timeoutException);
                return await _currentRequest.Task;
            }

            return responsePacket;
        }
        finally
        {
            _currentRequest = null;
        }
    }

    private void OnReceiveResponse(ref HeliCrashResponseEvent responseEvent)
    {
        if (!_currentRequest.TrySetResult(responseEvent.packet))
        {
            _logger.LogError("Failed to set UniTaskCompletionSource result!");
            _currentRequest.TrySetException(new InvalidPacketException(""));
            return;
        }

        if (_configService.LoggingEnabled.Value)
        {
            _logger.LogInfo(
                $"Setting UniTaskCompletionSource result = RequestHeliCrashPacket ({responseEvent.packet})"
            );
        }
    }
}
