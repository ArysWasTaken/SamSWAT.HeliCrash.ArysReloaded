using System;
using System.Linq;
using System.Threading;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using ZLinq;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public abstract class LootContainerFactory(Logger logger, LocalizationService localizationService)
{
    public virtual async UniTask CreateContainer(
        LootableContainer container,
        Item containerItem,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (containerItem == null)
            {
                throw new NullReferenceException("Container item is null!");
            }

            LootItem.CreateLootContainer(
                container,
                containerItem,
                localizationService.Localize("containerName"),
                Singleton<GameWorld>.Instance
            );

            ResourceKey[] resourceKeys = GetResourceKeys(containerItem);

            await LoadItemBundles(resourceKeys, cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            logger.LogError(
                $"Failed to create helicrash loot crate! {ex.Message}\n{ex.StackTrace}"
            );
        }
    }

    protected abstract UniTask LoadItemBundles(
        ResourceKey[] resourceKeys,
        CancellationToken cancellationToken = default
    );

    private static ResourceKey[] GetResourceKeys(Item containerItem)
    {
        ResourceKey[] resourceKeys;
        if (containerItem is ContainerData container)
        {
            resourceKeys = container
                .GetAllItemsFromCollection()
                .AsValueEnumerable()
                .SelectMany(item => item.Template.AllResources)
                .ToArray();
        }
        else
        {
            resourceKeys = containerItem.Template.AllResources.ToArray();
        }

        return resourceKeys;
    }
}
