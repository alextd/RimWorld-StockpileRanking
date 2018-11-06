using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Verse;
using RimWorld;
using Harmony;

namespace Stockpile_Ranking
{
	//Fleshing out vanilla functions: changing Quality and HP filter calls notify method
	[HarmonyPatch(typeof(ThingFilterUI), "DrawHitPointsFilterConfig")]
	static class DrawHitPointsFilterConfig
	{
		//private static void DrawHitPointsFilterConfig(ref float y, float width, ThingFilter filter)
		public static void Prefix(ThingFilter filter, ref FloatRange __state)
		{
			__state = filter.AllowedHitPointsPercents;
		}
		public static void Postfix(ThingFilter filter, FloatRange __state)
		{
			if (__state != filter.AllowedHitPointsPercents)
				(RankComp.settingsChangedCallbackInfo.GetValue(filter) as Action)();
		}
	}

	[HarmonyPatch(typeof(ThingFilterUI), "DrawQualityFilterConfig")]
	static class DrawQualityFilterConfig
	{
		//private static void DrawQualityFilterConfig(ref float y, float width, ThingFilter filter)
		public static void Prefix(ThingFilter filter, ref QualityRange __state)
		{
			__state = filter.AllowedQualityLevels;
		}
		public static void Postfix(ThingFilter filter, QualityRange __state)
		{
			if (__state != filter.AllowedQualityLevels)
				(RankComp.settingsChangedCallbackInfo.GetValue(filter) as Action)();
		}
	}
}
