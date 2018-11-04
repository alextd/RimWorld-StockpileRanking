using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using Harmony;

namespace Stockpile_Ranking
{
	[HarmonyPatch(typeof(StorageSettings), "ExposeData")]
	static class ExposeData
	{
		//public void ExposeData()
		public static void Postfix(StorageSettings __instance)
		{
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				List<ThingFilter> ranks = RankComp.GetRanks(__instance, false);
				if (ranks == null) return;

				Scribe_Collections.Look(ref ranks, "ranks", LookMode.Deep);
				return;
			}
			List<ThingFilter> loadRanks = null;

			Scribe_Collections.Look(ref loadRanks, "ranks", LookMode.Deep);
			if (loadRanks == null) return;

			foreach (ThingFilter rank in loadRanks)
				RankComp.AddFilter(__instance, rank);
		}
	}

	[HarmonyPatch(typeof(StorageSettings), "CopyFrom")]
	class CopyFrom
	{
		//public void CopyFrom(StorageSettings other)
		public static void Prefix(StorageSettings __instance, StorageSettings other)
		{
			RankComp.CopyFrom(__instance, other);
		}
	}

	[HarmonyPatch(typeof(StorageSettings), "AllowedToAccept", new Type[] { typeof(Thing)})]
	class AllowedToAccept_Thing
	{
		//public bool AllowedToAccept(Thing t)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo filterInfo = AccessTools.Field(typeof(StorageSettings), "filter");

			foreach(CodeInstruction i in instructions)
			{
				if(i.opcode == OpCodes.Ldfld && i.operand == filterInfo)
				{
					//instead of this.filter.Allows(t)
					//GetFilter(this).Allows(t)
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AllowedToAccept_Thing), nameof(GetFilter)));
				}
				else
					yield return i;
			}
		}

		public static ThingFilter GetFilter(StorageSettings settings)
		{
			return settings.filter;
		}
	}
}
