using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Verse;
using HarmonyLib;
using RimWorld;

namespace Stockpile_Ranking
{
	static class ThingFilterHelper
	{
		public static FieldInfo specials = AccessTools.Field(typeof(ThingFilter), "disallowedSpecialFilters");
		public static void Add(this ThingFilter filter, ThingFilter other)
		{
			foreach (ThingDef def in other.AllowedThingDefs)
				filter.SetAllow(def, true);

			List<SpecialThingFilterDef> disallowedSpecialFilters = (List <SpecialThingFilterDef> )specials.GetValue(other);
			foreach (SpecialThingFilterDef specDef in disallowedSpecialFilters)
				if (other.Allows(specDef))
					filter.SetAllow(specDef, true);

			QualityRange q = filter.AllowedQualityLevels;
			QualityRange qO = other.AllowedQualityLevels;
			q.max = q.max > qO.max ? q.max : qO.max;
			q.min = q.min < qO.min ? q.min : qO.min;
			filter.AllowedQualityLevels = q;

			FloatRange hp = filter.AllowedHitPointsPercents;
			FloatRange hpO = other.AllowedHitPointsPercents;
			hp.max = Math.Max(hp.max, hpO.max);
			hp.min = Math.Min(hp.min, hpO.min);
			filter.AllowedHitPointsPercents = hp;
		}
	}
}
