using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using Harmony;
using UnityEngine;

namespace Stockpile_Ranking
{
	[HarmonyPatch(typeof(ITab_Storage), "TopAreaHeight", MethodType.Getter)]
	static class TopAreaHeight
	{
		public const float rankHeight = 24f;
		//private float TopAreaHeight
		public static void Postfix(ref float __result)
		{
			__result += rankHeight;
		}
	}
	[StaticConstructorOnStartup]
	static class Tex
	{
		public static Texture2D Plus = ContentFinder<Texture2D>.Get("UI/Buttons/Plus", true);
	}

	[HarmonyPatch(typeof(ITab_Storage), "FillTab")]
	static class FillTab
	{
		//protected override void FillTab()
		static MethodInfo GetTopAreaHeight = AccessTools.Property(typeof(ITab_Storage), "TopAreaHeight").GetGetMethod(true);
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//		public static void BeginGroup(Rect position);
			MethodInfo BeginGroupInfo = AccessTools.Method(typeof(GUI), nameof(GUI.BeginGroup), new Type[] { typeof(Rect) });

			bool firstTopAreaHeight = true;
			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				if (firstTopAreaHeight && 
					i.opcode == OpCodes.Call && i.operand == GetTopAreaHeight)
				{
					firstTopAreaHeight = false;
					yield return new CodeInstruction(OpCodes.Ldc_R4, TopAreaHeight.rankHeight);
					yield return new CodeInstruction(OpCodes.Sub);
				}

				if(i.opcode == OpCodes.Call && i.operand == BeginGroupInfo)
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);//ITab_Storage this
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FillTab), nameof(DrawRanking)));
				}
			}
		}
		public static void DrawRanking(ITab_Storage tab)
		{
			//ITab_Storage.WinSize = 300
			Rect rect = new Rect(0f, (float)GetTopAreaHeight.Invoke(tab, new object[] { }) - TopAreaHeight.rankHeight - 2, 280, TopAreaHeight.rankHeight);
			
			//Left Arrow
			Widgets.ButtonImage(rect.LeftPartPixels(TopAreaHeight.rankHeight), TexUI.ArrowTexLeft);

			//Right Arrow
			Widgets.ButtonImage(rect.RightPartPixels(TopAreaHeight.rankHeight), Tex.Plus);

			//Label
			rect.x += TopAreaHeight.rankHeight + 2;
			Text.Font = GameFont.Small;
			Widgets.Label(rect, "Rank 1 example");
		}
	}
}
