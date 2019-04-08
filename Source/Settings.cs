using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace Stockpile_Ranking
{
	class Settings : ModSettings
	{
		public bool returnLower;

		public static Settings Get()
		{
			return LoadedModManager.GetMod<Stockpile_Ranking.Mod>().GetSettings<Settings>();
		}

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);

			bool old = returnLower;
			options.CheckboxLabeled("TD.SettingReturn".Translate(), ref returnLower);
			if (old != returnLower)
				if (RankComp.Get() is RankComp comp)
					comp.dirty = true;
			options.Label("TD.SettingDescCPU".Translate());
			options.Label("TD.SettingDescLowerStorage".Translate());
			options.Label("TD.SettingDescDefault".Translate());
			options.Gap();
			options.Label("TD.SettingDesc".Translate());
			options.Gap();

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref returnLower, "returnLower", true);
		}
	}
}