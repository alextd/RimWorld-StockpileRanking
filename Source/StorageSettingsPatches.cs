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
			var comp = RankComp.Get();
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				List<ThingFilter> ranks = comp.GetRanks(__instance, false);
				if (ranks == null) return;

				Scribe_Collections.Look(ref ranks, "ranks", LookMode.Deep);
				return;
			}
			List<ThingFilter> loadRanks = null;

			Scribe_Collections.Look(ref loadRanks, "ranks", LookMode.Deep);
			if (loadRanks == null) return;

			foreach (ThingFilter filter in loadRanks)
				comp.AddFilter(__instance, filter);
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

			foreach (CodeInstruction i in instructions)
			{
				//instead of this.filter.Allows(t)
				//RankComp.UsedFilter(this).Allows(t)
				//so the ilcodes are this, filter, t, Allows
				// replace filter with UsedFilter
				if (i.opcode == OpCodes.Ldfld && i.operand == filterInfo)
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RankComp), nameof(RankComp.UsedFilter)));
				}
				else
					yield return i;
			}
		}
	}

	[HarmonyPatch(typeof(StorageSettings), "TryNotifyChanged")]
	class TryNotifyChanged
	{
		//private void TryNotifyChanged()
		public static void Prefix(StorageSettings __instance)
		{
			var comp = RankComp.Get();
			comp.DetermineUsedFilter(__instance, comp.GetRanks(__instance, false));
			//Not just making it dirty, since TryNotifyChanged probably uses the determined filter so it needs to be done now
		}
	}
}
