using System;
using SamSWAT.HeliCrash.ArysReloaded.Fika.Models;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika.Events;

public readonly struct HeliCrashResponseEvent : IEvent
{
    public readonly HeliCrashDataPacket packet;

    [Obsolete("Use the static Create method instead")]
    public HeliCrashResponseEvent()
    {
        throw new InvalidOperationException("Please use the static Create method instead!");
    }

    private HeliCrashResponseEvent(HeliCrashDataPacket packet)
    {
        this.packet = packet;
    }

    public static HeliCrashResponseEvent Create(HeliCrashDataPacket packet)
    {
        return new HeliCrashResponseEvent(packet);
    }
}
