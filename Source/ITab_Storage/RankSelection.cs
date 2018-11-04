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
	[HarmonyPatch(typeof(ITab_Storage), "TopAreaHeight", MethodType.Getter)]
	static class TopAreaHeight
	{
		//private float TopAreaHeight
		public static void Postfix(ref float __result)
		{
			__result += 24;
		}
	}

	[HarmonyPatch(typeof(ITab_Storage), "FillTab")]
	static class FillTab
	{
		//private float TopAreaHeight
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo GetTopAreaHeight = AccessTools.Property(typeof(ITab_Storage), "TopAreaHeight").GetGetMethod(true);

			bool firstTopAreaHeight = true;
			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				if (firstTopAreaHeight && 
					i.opcode == OpCodes.Call && i.operand == GetTopAreaHeight)
				{
					firstTopAreaHeight = false;
					yield return new CodeInstruction(OpCodes.Ldc_R4, 24f);
					yield return new CodeInstruction(OpCodes.Sub);
				}
			}
		}
	}
}
