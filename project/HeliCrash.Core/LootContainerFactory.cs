using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using static SPT.Reflection.Utils.ClientAppUtils;

namespace SamSWAT.HeliCrash.ArysReloaded;

public class LootContainerFactory
{
    private readonly ProfileEndpointFactoryAbstractClass _profileEndpointFactory =
        (ProfileEndpointFactoryAbstractClass)GetClientApp().GetClientBackEndSession();

    private readonly List<ResourceKey> _temporaryResourceList = new(100);

    public async Task CreateContainer(LootableContainer container, string lootTemplateId = null)
    {
        try
        {
            AirdropLootResponse lootResponse = (
                await _profileEndpointFactory.LoadLootContainerData(lootTemplateId)
            ).Value;

            if (lootResponse?.data == null)
            {
                if (HeliCrashPlugin.LoggingEnabled.Value)
                {
                    throw new NullReferenceException("Heli crash site loot response is null");
                }

                return;
            }

            Item containerItem = Singleton<ItemFactoryClass>
                .Instance.FlatItemsToTree(lootResponse.data)
                .Items[lootResponse.data[0]._id];

            LootItem.CreateLootContainer(
                container,
                containerItem,
                LocalizationService.GetString("containerName"),
                Singleton<GameWorld>.Instance
            );

            await AddLoot(containerItem);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                $"Failed to create helicrash loot crate! {ex.Message}\n{ex.StackTrace}"
            );
        }
    }

    private async Task AddLoot(Item containerItem)
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
            JobPriorityClass.Immediate
        );
    }
}
