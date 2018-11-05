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
			MethodInfo allowsInfo = AccessTools.Method(typeof(ThingFilter), "Allows", new Type[] { typeof(Thing)});

			foreach (CodeInstruction i in instructions)
			{
				//instead of this.filter.Allows(t)
				//this.Allows(t)
				//so the ilcodes are this, filter, t, Allows
				// remove filter and change Allows
				if (i.opcode == OpCodes.Ldfld && i.operand == filterInfo)
				{
					continue;
				}
				else if(i.opcode == OpCodes.Callvirt && i.operand == allowsInfo)
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AllowedToAccept_Thing), nameof(StorageAllows)));
				}
				else
					yield return i;
			}
		}

		public static bool StorageAllows(StorageSettings settings, Thing thing)
		{
			if (settings.filter.Allows(thing)) return true;

			if (!RankComp.HasRanks(settings))
				return false;

			//Find map
			Map map = null;
			if (settings.owner is IHaulDestination haulDestination)
				map = haulDestination.Map;
			//that should be good enough
			//else if (settings.owner is ISlotGroupParent slotGroupParent)
			//	map = slotGroupParent.Map;

			if (map == null)
				return false;

			List<ThingFilter> ranks = RankComp.GetRanks(settings);
			ThingFilter bestFilter = settings.filter;
			
			//Find haulables that are in lower priority storage
			//Don't check if they are valid for that storage, since that would call this again and cause a stack overflow!
			List<Thing> haulables = map.listerHaulables.ThingsPotentiallyNeedingHauling().
				FindAll(t => (StoreUtility.CurrentHaulDestinationOf(t)?.GetStoreSettings().Priority ?? StoragePriority.Unstored) < settings.Priority);

			for (int i = 0; i < ranks.Count; i++)
			{
				//if any higher-ranking item is available, don't look at lower ranks
				if (haulables.Any(t => bestFilter.Allows(t)))
					return false;

				//Nothing to fit higher rank, see if next rank works:

				bestFilter = ranks[i];
				if (bestFilter.Allows(thing))
					return true;
			}
			return false;
		}
	}
}
