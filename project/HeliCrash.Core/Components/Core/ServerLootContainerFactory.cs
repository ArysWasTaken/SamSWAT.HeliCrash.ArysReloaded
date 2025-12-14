using System;
using System.Threading;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using SPT.Reflection.Utils;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public sealed class ServerLootContainerFactory : LootContainerFactory
{
    private readonly Logger _logger;

    public ServerLootContainerFactory(Logger logger, LocalizationService localizationService)
        : base(logger, localizationService)
    {
        _logger = logger;
    }

    public override async UniTask CreateContainer(
        LootableContainer container,
        Item containerItem,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            containerItem ??= await RequestContainerItem(cancellationToken);
            await base.CreateContainer(container, containerItem, cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(
                $"Failed to create helicrash loot crate! {ex.Message}\n{ex.StackTrace}"
            );
        }
    }

    public async UniTask<Item> RequestContainerItem(CancellationToken cancellationToken = default)
    {
        try
        {
            AirdropLootResponse lootResponse = (
                await (
                    (ProfileEndpointFactoryAbstractClass)
                        ClientAppUtils.GetClientApp().GetClientBackEndSession()
                ).LoadLootContainerData(null)
            ).Value;

            cancellationToken.ThrowIfCancellationRequested();

            if (lootResponse?.data == null)
            {
                return await UniTask.FromException<Item>(
                    new NullReferenceException("Heli crash loot response is null")
                );
            }

            Item containerItem = Singleton<ItemFactoryClass>
                .Instance.FlatItemsToTree(lootResponse.data)
                .Items[lootResponse.data[0]._id];

            return containerItem;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"Request failed trying to create container item! {ex.Message}\n{ex.StackTrace}"
            );
            return null;
        }
    }

    protected override async UniTask LoadItemBundles(
        ResourceKey[] resourceKeys,
        CancellationToken cancellationToken = default
    )
    {
        await Singleton<PoolManagerClass>.Instance.LoadBundlesAndCreatePools(
            PoolManagerClass.PoolsCategory.Raid,
            PoolManagerClass.AssemblyType.Online,
            resourceKeys,
            JobPriorityClass.Immediate,
            ct: cancellationToken
        );
    }
}
