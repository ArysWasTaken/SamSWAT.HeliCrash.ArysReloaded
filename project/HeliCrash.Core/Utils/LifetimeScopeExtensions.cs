using System;
using Comfort.Common;
using EFT;
using UnityEngine;
using VContainer.Unity;

namespace SamSWAT.HeliCrash.ArysReloaded.Utils;

public static class LifetimeScopeExtensions
{
    public static TScope CreateChild<TScope>(
        this LifetimeScope parentScope,
        Transform parent,
        string childScopeName = null
    )
        where TScope : LifetimeScope
    {
        var gameObject = new GameObject(childScopeName ?? "LifetimeScope (Child)");
        gameObject.SetActive(false);
        gameObject.transform.SetParent(parent, false);

        var child = gameObject.AddComponent<TScope>();
        child.parentReference.Object = parentScope;
        gameObject.SetActive(true);
        return child;
    }

    public static RaidLifetimeScope GetRaidLifetimeScope()
    {
        GameWorld gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld == null)
        {
            throw new NullReferenceException(
                $"GameWorld is null. Please only invoke {nameof(GetRaidLifetimeScope)} during a raid!"
            );
        }

        var raidLifetimeScope = gameWorld.GetComponentInChildren<RaidLifetimeScope>();
        if (raidLifetimeScope == null)
        {
            throw new NullReferenceException(
                $"{nameof(RaidLifetimeScope)} doesn't exist when it should!"
            );
        }

        return raidLifetimeScope;
    }
}
