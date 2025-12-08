using Newtonsoft.Json;
using UnityEngine;

namespace SamSWAT.HeliCrash.ArysReloaded.Models;

[JsonObject]
[method: JsonConstructor]
public class Location(
	[JsonProperty("Position", Required = Required.Always)] Vector3 position,
	[JsonProperty("Rotation", Required = Required.Always)] Vector3 rotation,
	[JsonProperty("Unreachable")] bool unreachable)
{
	public Vector3 Position { get; } = position;
	public Vector3 Rotation { get; } = rotation;
	public bool Unreachable { get; } = unreachable;
}