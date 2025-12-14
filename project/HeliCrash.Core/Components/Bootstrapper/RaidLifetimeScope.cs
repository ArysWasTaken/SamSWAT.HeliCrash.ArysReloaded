using VContainer;
using VContainer.Unity;

namespace SamSWAT.HeliCrash.ArysReloaded;

public class RaidLifetimeScope : LifetimeScope
{
    protected override void Awake()
    {
        autoRun = false;

        base.Awake();
    }

    protected override void Configure(IContainerBuilder builder)
    {
        builder
            .Register<ServerLootContainerFactory>(Lifetime.Scoped)
            .As<LootContainerFactory>()
            .AsSelf();
        builder.Register<ServerHeliCrashSpawner>(Lifetime.Scoped).As<HeliCrashSpawner>().AsSelf();
    }
}
