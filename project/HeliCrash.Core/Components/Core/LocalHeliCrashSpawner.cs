using System.Threading;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Models;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using UnityEngine;
using UnityEngine.AI;
using Location = SamSWAT.HeliCrash.ArysReloaded.Models.Location;
using Logger = SamSWAT.HeliCrash.ArysReloaded.Utils.Logger;
using Object = UnityEngine.Object;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class LocalHeliCrashSpawner : HeliCrashSpawner
{
    private readonly ConfigurationService _configService;
    private readonly Logger _logger;
    private readonly HeliCrashLocationService _locationService;
    private readonly LootContainerFactory _lootContainerFactory;

    public Location SpawnLocation { get; private set; }
    public int[] DoorNetIds { get; private set; }
    public Item ContainerItem { get; private set; }
    public int ContainerNetId { get; private set; }

    public LocalHeliCrashSpawner(
        ConfigurationService configService,
        Logger logger,
        HeliCrashLocationService locationService,
        LootContainerFactory lootContainerFactory
    )
        : base(configService, logger, locationService)
    {
        _configService = configService;
        _logger = logger;
        _locationService = locationService;
        _lootContainerFactory = lootContainerFactory;
    }

    protected override async UniTask SpawnCrashSite(CancellationToken cancellationToken = default)
    {
        LocationList crashLocations = _locationService.GetCrashLocations(
            Singleton<GameWorld>.Instance.LocationId
        );

        if (_configService.SpawnAllCrashSites.Value)
        {
            await CreateAllCrashSites(crashLocations, cancellationToken);
        }
        else
        {
            await CreateCrashSite(crashLocations, cancellationToken);
        }
    }

    private async UniTask CreateAllCrashSites(
        LocationList crashLocations,
        CancellationToken cancellationToken = default
    )
    {
        AsyncInstantiateOperation<GameObject> asyncOperation = Object.InstantiateAsync(
            heliPrefab,
            crashLocations.Count,
            crashLocations.Positions,
            crashLocations.Rotations
        );

        while (!asyncOperation.isDone)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(cancellationToken);
        }

        GameObject[] choppas = asyncOperation.Result;
        for (var i = 0; i < choppas.Length; i++)
        {
            choppas[i].SetActive(true);
        }

        if (_configService.LoggingEnabled.Value)
        {
            _logger.LogInfo("Successfully spawned all heli crash sites");
        }
    }

    private async UniTask CreateCrashSite(
        LocationList crashLocations,
        CancellationToken cancellationToken = default
    )
    {
        SpawnLocation = crashLocations.SelectRandom();

        GameObject choppa = await InstantiateCrashSiteObject(
            SpawnLocation.Position,
            SpawnLocation.Rotation,
            cancellationToken
        );

        if (!_configService.SpawnAllCrashSites.Value)
        {
            CarveNavMesh(choppa);
        }

        GameWorld gameWorld = Singleton<GameWorld>.Instance;

        if (gameWorld.World_0 != null)
        {
            Door[] doors = choppa.GetComponentsInChildren<Door>();

            int doorCount = doors.Length;

            DoorNetIds = new int[doorCount];

            for (var i = 0; i < doorCount; i++)
            {
                DoorNetIds[i] = doors[i].NetId;
                gameWorld.RegisterWorldInteractionObject(doors[i]);
            }
        }

        bool spawnWithLoot =
            !SpawnLocation.Unreachable && BlessRNG.RngBool(_configService.CrashHasLootChance.Value);

        var container = choppa.GetComponentInChildren<LootableContainer>();

        if (spawnWithLoot)
        {
            if (_configService.LoggingEnabled.Value)
            {
                _logger.LogInfo("Spawning with loot!");
            }

            ContainerItem = await _lootContainerFactory.RequestContainerItem(cancellationToken);

            ContainerNetId = await _lootContainerFactory.CreateContainer(
                container,
                ContainerItem,
                cancellationToken
            );
        }
        else
        {
            if (_configService.LoggingEnabled.Value)
            {
                _logger.LogInfo(
                    $"Not spawning with loot! Unreachable={SpawnLocation.Unreachable.ToString()}"
                );
            }

            // Disable the loot crate game object
            container.transform.parent.gameObject.SetActive(false);
        }

        if (_configService.LoggingEnabled.Value)
        {
            _logger.LogWarning($"Heli crash site spawned at {SpawnLocation.Position.ToString()}");
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
}
