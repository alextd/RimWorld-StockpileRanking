using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Verse;
using Harmony;

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
			//todo HP AND Quality
		}
	}
}
