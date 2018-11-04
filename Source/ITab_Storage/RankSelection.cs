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

		//-----------------------------------------------
		//Here's the meat
		//-----------------------------------------------
		public static int curRank = 0;
		public static PropertyInfo SelStoreInfo = AccessTools.Property(typeof(ITab_Storage), "SelStoreSettingsParent");
		public static void DrawRanking(ITab_Storage tab)
		{
			IStoreSettingsParent storeSettingsParent = SelStoreInfo.GetValue(tab, null) as IStoreSettingsParent;
			StorageSettings settings = storeSettingsParent.GetStoreSettings();
			int count = RankComp.Count(settings);
			if (curRank >= count) curRank = count - 1;

			//ITab_Storage.WinSize = 300
			Rect rect = new Rect(0f, (float)GetTopAreaHeight.Invoke(tab, new object[] { }) - TopAreaHeight.rankHeight - 2, 280, TopAreaHeight.rankHeight);

			//Left Arrow
			if (curRank > 0)
			{
				if (Widgets.ButtonImage(rect.LeftPartPixels(TopAreaHeight.rankHeight), TexUI.ArrowTexLeft))
				{
					curRank--;
				}
			}

			//Right Arrow
			if (curRank == count - 1)
			{
				if (Widgets.ButtonImage(rect.RightPartPixels(TopAreaHeight.rankHeight), Tex.Plus))
				{
					curRank++;
					RankComp.Set(settings, curRank, settings.filter);
				}
			}
			else
			{
				if (Widgets.ButtonImage(rect.RightPartPixels(TopAreaHeight.rankHeight), TexUI.ArrowTexRight))
				{
					curRank++;
				}
			}

			//Label
			rect.x += TopAreaHeight.rankHeight + 2;
			Text.Font = GameFont.Small;
			Widgets.Label(rect, $"Rank {curRank+1}");
		}
	}

	[HarmonyPatch(typeof(InspectTabBase), "OnOpen")]
	static class ResetCurRank
	{
		//public virtual void OnOpen()
		public static void Postfix(InspectTabBase __instance)
		{
			if (__instance is ITab_Storage)
				FillTab.curRank = 0;
		}
	}
}
