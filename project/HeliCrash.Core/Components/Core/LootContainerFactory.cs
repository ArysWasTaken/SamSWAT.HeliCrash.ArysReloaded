using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Comfort.Common;
using Cysharp.Threading.Tasks;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using static SPT.Reflection.Utils.ClientAppUtils;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class LootContainerFactory(
    ConfigurationService configService,
    Logger logger,
    LocalizationService localizationService
)
{
    private readonly ProfileEndpointFactoryAbstractClass _profileEndpointFactory =
        (ProfileEndpointFactoryAbstractClass)GetClientApp().GetClientBackEndSession();

    private readonly List<ResourceKey> _temporaryResourceList = new(100);

    public async UniTask CreateContainer(
        LootableContainer container,
        string lootTemplateId = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            AirdropLootResponse lootResponse = (
                await _profileEndpointFactory.LoadLootContainerData(lootTemplateId)
            ).Value;

            cancellationToken.ThrowIfCancellationRequested();

            if (lootResponse?.data == null)
            {
                if (configService.LoggingEnabled.Value)
                {
                    throw new NullReferenceException("Heli crash site loot response is null");
                }

                return;
            }

            Item containerItem = Singleton<ItemFactoryClass>
                .Instance.FlatItemsToTree(lootResponse.data)
                .Items[lootResponse.data[0]._id];

            container.Id = containerItem.Id;

            LootItem.CreateLootContainer(
                container,
                containerItem,
                localizationService.Localize("containerName"),
                Singleton<GameWorld>.Instance
            );

            await AddLoot(containerItem, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                $"Failed to create helicrash loot crate! {ex.Message}\n{ex.StackTrace}"
            );
        }
    }

    private async UniTask AddLoot(Item containerItem, CancellationToken cancellationToken = default)
    {
        ResourceKey[] resources;
        if (containerItem is ContainerData container)
        {
            var items = (List<Item>)container.GetAllItemsFromCollection();

            foreach (Item item in items)
            {
                item.SpawnedInSession = true;
                _temporaryResourceList.AddRange(item.Template.AllResources);
            }

            resources = _temporaryResourceList.ToArray();

            _temporaryResourceList.Clear();
        }
        else
        {
            resources = containerItem.Template.AllResources.ToArray();
        }

        await Singleton<PoolManagerClass>.Instance.LoadBundlesAndCreatePools(
            PoolManagerClass.PoolsCategory.Raid,
            PoolManagerClass.AssemblyType.Local,
            resources,
            JobPriorityClass.Immediate,
            ct: cancellationToken
        );
    }
}
