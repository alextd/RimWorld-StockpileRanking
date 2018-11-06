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
		public static void Prefix(StorageSettings __instance, ref Action __state)
		{
			__state = RankComp.settingsChangedCallbackInfo.GetValue(__instance.filter) as Action;
		}
		public static void Postfix(StorageSettings __instance, Action __state)
		{
			//BUG FIX TIME
			//public void ExposeData() in StorageSettings would re-assign the filter, meaning the action passed to its ctor was lost
			//so workaround, save it before ExposeData and re-assign it
			RankComp.settingsChangedCallbackInfo.SetValue(__instance.filter, __state);

			//Save/load the ranked filters in a list
			var comp = RankComp.Get();
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				List<ThingFilter> ranks = comp.GetRanks(__instance, false);
				if (ranks == null) return;

				Scribe_Collections.Look(ref ranks, "ranks", LookMode.Deep);
			}
			else if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				List<ThingFilter> loadRanks = null;
				Scribe_Collections.Look(ref loadRanks, "ranks", LookMode.Deep);
				comp.SetRanks(__instance, loadRanks);
			}
		}
	}

	[HarmonyPatch(typeof(StorageSettings), "CopyFrom")]
	class CopyFrom
	{
		//public void CopyFrom(StorageSettings other)

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo TryNotifyChangedInfo = AccessTools.Method(typeof(StorageSettings), "TryNotifyChanged");

			foreach (CodeInstruction i in instructions)
			{
				if(i.opcode == OpCodes.Call && i.operand == TryNotifyChangedInfo)
				{
					//RankComp.Get().CopyFrom(__instance, other);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RankComp), nameof(RankComp.Get)));
					yield return new CodeInstruction(OpCodes.Ldarg_0);//this
					yield return new CodeInstruction(OpCodes.Ldarg_1);//other
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RankComp), nameof(RankComp.CopyFrom)));
				}
				yield return i;
			}
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
			RankComp.Get().CascadeDown(__instance);
		}
	}
}
