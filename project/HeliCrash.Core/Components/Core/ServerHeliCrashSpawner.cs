using System.Threading;
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

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public sealed class ServerHeliCrashSpawner : HeliCrashSpawner
{
    private readonly ConfigurationService _configService;
    private readonly Logger _logger;
    private readonly HeliCrashLocationService _locationService;
    private readonly ServerLootContainerFactory _lootContainerFactory;

    public Location SpawnLocation { get; private set; }
    public Item ContainerItem { get; private set; }

    public ServerHeliCrashSpawner(
        ConfigurationService configService,
        Logger logger,
        HeliCrashLocationService locationService,
        ServerLootContainerFactory lootContainerFactory
    )
        : base(configService, logger)
    {
        _configService = configService;
        _logger = logger;
        _locationService = locationService;
        _lootContainerFactory = lootContainerFactory;
    }

    protected override async UniTask SpawnCrashSite(
        GameWorld gameWorld,
        CancellationToken cancellationToken = default
    )
    {
        LocationList crashLocations = _locationService.GetCrashLocations(
            gameWorld.MainPlayer.Location
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

        _logger.LogInfo("Successfully spawned all heli crash sites");
    }

    private async UniTask CreateCrashSite(
        LocationList crashLocations,
        CancellationToken cancellationToken = default
    )
    {
        SpawnLocation = crashLocations.SelectRandom();

        AsyncInstantiateOperation<GameObject> asyncOperation = Object.InstantiateAsync(
            heliPrefab,
            SpawnLocation.Position,
            Quaternion.Euler(SpawnLocation.Rotation)
        );

        while (!asyncOperation.isDone)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(cancellationToken);
        }

        GameObject choppa = asyncOperation.Result[0];

        if (!_configService.SpawnAllCrashSites.Value)
        {
            CarveNavMesh(choppa);
        }

        bool spawnWithLoot =
            !SpawnLocation.Unreachable && BlessRNG.RngBool(_configService.CrashHasLootChance.Value);

        var container = choppa.GetComponentInChildren<LootableContainer>();

        if (spawnWithLoot)
        {
            ContainerItem = await _lootContainerFactory.RequestContainerItem(cancellationToken);

            await _lootContainerFactory.CreateContainer(
                container,
                ContainerItem,
                cancellationToken
            );
        }
        else
        {
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
