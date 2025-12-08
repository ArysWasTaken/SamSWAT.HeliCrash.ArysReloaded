using System.Collections.Generic;
using UnityEngine;

namespace SamSWAT.HeliCrash.ArysReloaded.Utils;

internal static class BlessRNG
{
	public static T SelectRandom<T>(this IReadOnlyList<T> list)
	{
		if (list.Count == 0)
		{
			return default;
		}
		
		int index = Random.Range(0, list.Count);
		return list[index];
	}
	
	public static bool RngBool(int chanceInPercent = 50)
	{
		return Random.Range(1, 101) <= chanceInPercent;
	}
}