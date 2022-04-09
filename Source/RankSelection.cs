using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.Sound;
using RimWorld;
using HarmonyLib;
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
			MethodInfo BeginGroupInfo = AccessTools.Method(typeof(Widgets), nameof(Widgets.BeginGroup), new Type[] { typeof(Rect) });

			//class Verse.ThingFilter RimWorld.StorageSettings::'filter'
			FieldInfo filterInfo = AccessTools.Field(typeof(StorageSettings), "filter");
			int count = 0;

			bool firstTopAreaHeight = true;
			List<CodeInstruction> instList = instructions.ToList();
			for(int i=0;i<instList.Count;i++)
			{
				CodeInstruction inst = instList[i];

				//replace
				//  settings.filter
				//with
				//  RankComp.GetFilter(settings.filter, FillTab.curRank)
				//but there's another .filter that is Parent filter so check for .settings

				//IL_01f0: ldfld        class RimWorld.StorageSettings RimWorld.ITab_Storage/'<>c__DisplayClass12_0'::settings
				//IL_01f5: ldfld        class Verse.ThingFilter RimWorld.StorageSettings::'filter'

				//What why the ITab_Storage.\u003C\u003Ec__DisplayClass12_0 really

				//Soo I guess just skip it for the first call to filter in the method
				if (inst.LoadsField(filterInfo) && count++ > 0)
				{
					//instead of settings.filter, do RankComp.GetFilter(settings, curRank)
					yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(FillTab), "curRank"));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RankComp), "GetFilter"));
				}
				else
					yield return inst;

				if (firstTopAreaHeight && 
					inst.Calls(GetTopAreaHeight))
				{
					firstTopAreaHeight = false;
					yield return new CodeInstruction(OpCodes.Ldc_R4, TopAreaHeight.rankHeight);
					yield return new CodeInstruction(OpCodes.Sub);
				}

				if(inst.Calls(BeginGroupInfo))
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

		public delegate IStoreSettingsParent SelStoreSettingsParentDel(ITab_Storage tab);
		public static SelStoreSettingsParentDel SelStoreSettingsParent = AccessTools.MethodDelegate<SelStoreSettingsParentDel>(
			AccessTools.PropertyGetter(typeof(ITab_Storage), "SelStoreSettingsParent"));

		public static void DrawRanking(ITab_Storage tab)
		{
			IHaulDestination haulDestination = SelStoreSettingsParent(tab) as IHaulDestination;
			if (haulDestination == null) return;
			StorageSettings settings = haulDestination.GetStoreSettings();
			if (settings == null) return;

			var comp = RankComp.Get();
			int count = comp.CountExtraFilters(settings);
			if (curRank > count) curRank = count;

			float buttonMargin = TopAreaHeight.rankHeight + 4;

			//ITab_Storage.WinSize = 300
			Rect rect = new Rect(0f, (float)GetTopAreaHeight.Invoke(tab, new object[] { }) - TopAreaHeight.rankHeight - 2, 280, TopAreaHeight.rankHeight);

			//Left Arrow
			Rect leftButtonRect = rect.LeftPartPixels(TopAreaHeight.rankHeight);
			if (curRank > 0)
			{
				if (Widgets.ButtonImage(leftButtonRect, Tex.ArrowLeft))
				{
					SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
					curRank--;
				}
			}

			//Right Arrow
			Rect rightButtonRect = rect.RightPartPixels(TopAreaHeight.rankHeight);
			if (curRank == count)
			{
				if (Widgets.ButtonImage(rightButtonRect, Tex.Plus))
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					comp.AddFilter(settings);
					curRank++;
				}
			}
			else
			{
				if (Widgets.ButtonImage(rightButtonRect, Tex.ArrowRight))
				{
					SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
					curRank++;
				}
			}

			//Delete rank button
			rightButtonRect.x -= buttonMargin;
			if (curRank > 0)
			{
				if (Widgets.ButtonImage(rightButtonRect, Tex.DeleteX))
				{
					SoundDefOf.Crunch.PlayOneShotOnCamera();
					comp.RemoveFilter(settings, curRank--);
				}
			}

			//Label
			rect.x += buttonMargin;
			rect.width -= buttonMargin * 3;
			Text.Font = GameFont.Small;
			if(count == 0)
				Widgets.Label(rect, "TD.AddFilter".Translate());
			else
				Widgets.Label(rect, "TD.RankNum".Translate(curRank + 1));
		}
	}

	//[HarmonyPatch(typeof(InspectTabBase), "OnOpen")]
	//static class ResetCurRank
	//{
	//	//public virtual void OnOpen()
	//	public static void Postfix(InspectTabBase __instance)
	//	{
	//		if (__instance is ITab_Storage)
	//			FillTab.curRank = 0;
	//	}
	//}
}
