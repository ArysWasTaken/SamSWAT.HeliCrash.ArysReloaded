using Comfort.Common;
using EFT;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using VContainer.Unity;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class RaidLifetimeScopeController
{
    private ApplicationLifetimeScope _appLifetimeScope;

    public void Initialize(ApplicationLifetimeScope appLifetimeScope)
    {
        _appLifetimeScope = appLifetimeScope;
    }

    public LifetimeScope CreateScope()
    {
        var raidLifetimeScope = _appLifetimeScope.CreateChild<RaidLifetimeScope>(
            Singleton<GameWorld>.Instance.transform,
            childScopeName: "RaidLifetimeScope"
        );

        return raidLifetimeScope;
    }
}
