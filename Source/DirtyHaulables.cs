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
	[HarmonyPatch(typeof(ListerHaulables), "Check")]
	class DirtyHaulables
	{
		//private void Check(Thing t)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo AddInfo = AccessTools.Method(typeof(List<Thing>), "Add");
			MethodInfo RemoveInfo = AccessTools.Method(typeof(List<Thing>), "Remove");
			Log.Message($"AddInfo is {AddInfo}");
			Log.Message($"RemoveInfo is {RemoveInfo}");

			foreach (CodeInstruction i in instructions)
			{
				yield return i;

				if (i.operand == AddInfo || i.operand == RemoveInfo)
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RankComp), nameof(RankComp.Get)));//RankComp.Get()
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);//true
					yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(RankComp), nameof(RankComp.dirty)));//RankComp.Get().dirty = true;
				}
			}
		}
	}

	[HarmonyPatch(typeof(ListerHaulables), "CheckAdd")]
	class DirtyHaulablesAdd
	{
		//private void CheckAdd(Thing t)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return DirtyHaulables.Transpiler(instructions);
		}
	}

	[HarmonyPatch(typeof(ListerHaulables), "TryRemove")]
	class DirtyHaulablesRemove
	{
		//private void TryRemove(Thing t)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return DirtyHaulables.Transpiler(instructions);
		}
	}
}
