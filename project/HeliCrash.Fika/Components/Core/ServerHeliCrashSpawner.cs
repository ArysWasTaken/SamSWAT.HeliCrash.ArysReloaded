using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Fika.Events;
using SamSWAT.HeliCrash.ArysReloaded.Fika.Models;
using SamSWAT.HeliCrash.ArysReloaded.Utils;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika;

[UsedImplicitly]
public class ServerHeliCrashSpawner : LocalHeliCrashSpawner
{
    private readonly ConfigurationService _configService;
    private readonly Logger _logger;

    private HeliCrashDataPacket _cachedResponsePacket;

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
        EventDispatcher<HeliCrashRequestEvent>.Unsubscribe(OnReceiveRequest);
        base.Dispose();
    }

    public HeliCrashDataPacket GetCachedResponse()
    {
        if (_cachedResponsePacket != null)
        {
            return _cachedResponsePacket;
        }

        if (ShouldSpawn!.Value)
        {
            _cachedResponsePacket = new HeliCrashDataPacket(
                ShouldSpawn.Value,
                SpawnLocation.Position,
                SpawnLocation.Rotation,
                DoorNetIds,
                ContainerItem != null,
                ContainerItem,
                ContainerNetId
            );
        }
        else
        {
            _cachedResponsePacket = new HeliCrashDataPacket(ShouldSpawn.Value);
        }

        return _cachedResponsePacket;
    }

    private void OnReceiveRequest(ref HeliCrashRequestEvent requestEvent)
    {
        if (FinishedSpawning)
        {
            Logger logger = _configService.LoggingEnabled.Value ? _logger : null;

            requestEvent.callback.Invoke(this, logger);
        }
    }
}
