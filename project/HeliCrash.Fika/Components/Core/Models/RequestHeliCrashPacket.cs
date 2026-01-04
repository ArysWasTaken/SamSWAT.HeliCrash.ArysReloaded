using EFT.InventoryLogic;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.LiteNetLib.Utils;
using Fika.Core.Networking.Packets;
using SamSWAT.HeliCrash.ArysReloaded.Models;
using UnityEngine;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika.Models;

public class RequestHeliCrashPacket : INetSerializable, IRequestPacket
{
    public enum PacketType
    {
        Request,
        Response,
    }

    public PacketType packetType;
    public bool shouldSpawn;
    public Vector3 position;
    public Vector3 rotation;
    public int[] doorNetIds;
    public bool hasLoot;
    public Item containerItem;
    public int containerNetId;

    public RequestHeliCrashPacket()
    {
        packetType = PacketType.Request;
    }

    private RequestHeliCrashPacket(
        bool shouldSpawn,
        Vector3 position = default,
        Vector3 rotation = default,
        int[] doorNetIds = null,
        bool hasLoot = false,
        Item containerItem = null,
        int containerNetId = -1
    )
    {
        packetType = PacketType.Response;
        this.shouldSpawn = shouldSpawn;
        this.position = position;
        this.rotation = rotation;
        this.doorNetIds = doorNetIds;
        this.hasLoot = hasLoot;
        this.containerItem = containerItem;
        this.containerNetId = containerNetId;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutEnum(packetType);
        if (packetType == PacketType.Request)
        {
            return;
        }
        writer.Put(shouldSpawn);
        if (shouldSpawn)
        {
            writer.PutUnmanaged(position);
            writer.PutUnmanaged(rotation);
            writer.PutArray(doorNetIds);
            writer.Put(hasLoot);
            if (hasLoot)
            {
                writer.PutItem(containerItem);
                writer.Put(containerNetId);
            }
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        packetType = reader.GetEnum<PacketType>();
        if (packetType == PacketType.Request)
        {
            return;
        }
        shouldSpawn = reader.GetBool();
        if (shouldSpawn)
        {
            position = reader.GetUnmanaged<Vector3>();
            rotation = reader.GetUnmanaged<Vector3>();
            doorNetIds = reader.GetIntArray();
            hasLoot = reader.GetBool();
            if (hasLoot)
            {
                containerItem = reader.GetItem();
                containerNetId = reader.GetInt();
            }
        }
    }

    public void HandleRequest(NetPeer peer, FikaServer server)
    {
        var requestEvent = HeliCrashRequestEvent.Create(
            (spawner, logger) =>
            {
                RequestHeliCrashPacket responsePacket;

                if (spawner.ShouldSpawn!.Value)
                {
                    Location spawnLocation = spawner.SpawnLocation;
                    Item item = spawner.ContainerItem;

                    responsePacket = new RequestHeliCrashPacket(
                        spawner.ShouldSpawn.Value,
                        spawnLocation.Position,
                        spawnLocation.Rotation,
                        spawner.DoorNetIds,
                        item != null,
                        item,
                        spawner.ContainerNetId
                    );
                }
                else
                {
                    responsePacket = new RequestHeliCrashPacket(spawner.ShouldSpawn.Value);
                }

                if (logger != null)
                {
                    logger.LogInfo($"HeliCrash response packet: {responsePacket}");
                    logger.LogInfo(
                        $"Sending HeliCrash response to Fika Client {peer.Id.ToString()}"
                    );
                }

                server.SendDataToPeer(ref responsePacket, DeliveryMethod.ReliableOrdered, peer);
            }
        );
        EventDispatcher<HeliCrashRequestEvent>.Dispatch(ref requestEvent);
    }

    public void HandleResponse()
    {
        var responseEvent = HeliCrashResponseEvent.Create(this);
        EventDispatcher<HeliCrashResponseEvent>.Dispatch(ref responseEvent);
    }

    public override string ToString()
    {
        return string.Format(
            "PacketType={0}, Position={1}, Rotation={2}, HasLoot={3}, ContainerItem={4}, NetId={5}",
            packetType.ToString(),
            position.ToString(),
            rotation.ToString(),
            hasLoot.ToString(),
            containerItem,
            containerNetId.ToString()
        );
    }
}
