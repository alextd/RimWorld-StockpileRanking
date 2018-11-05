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
		public static readonly Texture2D Plus = ContentFinder<Texture2D>.Get("UI/Buttons/Plus", true);
		public static readonly Texture2D DeleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete", true);
		public static readonly Texture2D ArrowLeft = ContentFinder<Texture2D>.Get("ArrowLeftColor", true);
		public static readonly Texture2D ArrowRight = ContentFinder<Texture2D>.Get("ArrowRightColor", true);
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

			//class Verse.ThingFilter RimWorld.StorageSettings::'filter'
			FieldInfo filterInfo = AccessTools.Field(typeof(StorageSettings), "filter");
			MethodInfo DoThingFilterConfigWindowInfo = AccessTools.Method(typeof(ThingFilterUI), "DoThingFilterConfigWindow");

			bool firstTopAreaHeight = true;
			List<CodeInstruction> instList = instructions.ToList();
			for(int i=0;i<instList.Count;i++)
			{
				CodeInstruction inst = instList[i];

				if (inst.opcode == OpCodes.Ldfld && inst.operand == filterInfo &&
					instList[i + 8].opcode == OpCodes.Call && instList[i + 8].operand == DoThingFilterConfigWindowInfo)
				{
					//instead of settings.filter, do RankComp.GetFilter(settings, curRank)
					yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(FillTab), "curRank"));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RankComp), "GetFilter"));
				}
				else
					yield return inst;

				if (firstTopAreaHeight && 
					inst.opcode == OpCodes.Call && inst.operand == GetTopAreaHeight)
				{
					firstTopAreaHeight = false;
					yield return new CodeInstruction(OpCodes.Ldc_R4, TopAreaHeight.rankHeight);
					yield return new CodeInstruction(OpCodes.Sub);
				}

				if(inst.opcode == OpCodes.Call && inst.operand == BeginGroupInfo)
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
			if (storeSettingsParent == null) return;
			StorageSettings settings = storeSettingsParent.GetStoreSettings();
			if (settings == null) return;

			int count = RankComp.CountExtraFilters(settings);
			if (curRank > count) curRank = count;

			float buttonMargin = TopAreaHeight.rankHeight + 2;

			//ITab_Storage.WinSize = 300
			Rect rect = new Rect(0f, (float)GetTopAreaHeight.Invoke(tab, new object[] { }) - TopAreaHeight.rankHeight - 2, 280, TopAreaHeight.rankHeight);

			//Left Arrow
			Rect leftButtonRect = rect.LeftPartPixels(TopAreaHeight.rankHeight);
			if (curRank > 0)
			{
				if (Widgets.ButtonImage(leftButtonRect, Tex.ArrowLeft))
				{
					curRank--;
				}
			}

			//Right Arrow
			Rect rightButtonRect = rect.RightPartPixels(TopAreaHeight.rankHeight);
			if (curRank == count)
			{
				if (Widgets.ButtonImage(rightButtonRect, Tex.Plus))
				{
					ThingFilter newFilter = new ThingFilter();
					newFilter.CopyAllowancesFrom(RankComp.GetFilter(settings, curRank++));
					RankComp.AddFilter(settings, newFilter);
				}
			}
			else
			{
				if (Widgets.ButtonImage(rightButtonRect, Tex.ArrowRight))
				{
					curRank++;
				}
			}

			//Delete rank button
			rightButtonRect.x -= buttonMargin;
			if (curRank > 0)
			{
				if (Widgets.ButtonImage(rightButtonRect, Tex.DeleteX))
				{
					RankComp.RemoveFilter(settings, curRank--);
				}
			}

			//Label
			rect.x += buttonMargin;
			rect.width -= buttonMargin * 3;
			Text.Font = GameFont.Small;
			if(count == 0)
				Widgets.Label(rect, "Add filter ranking:");
			else
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
