using System;
using SamSWAT.HeliCrash.ArysReloaded.Utils;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika.Models;

public readonly struct HeliCrashRequestEvent : IEvent
{
    public readonly Action<ServerHeliCrashSpawner, Logger> callback;

    [Obsolete("Use the static Create method instead")]
    public HeliCrashRequestEvent()
    {
        throw new InvalidOperationException("Please use the static Create method instead!");
    }

    private HeliCrashRequestEvent(Action<ServerHeliCrashSpawner, Logger> callback)
    {
        this.callback = callback;
    }

    public static HeliCrashRequestEvent Create(Action<ServerHeliCrashSpawner, Logger> callback)
    {
        return new HeliCrashRequestEvent(callback);
    }
}
