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
			options.CheckboxLabeled("When higher ranked-items become available, make sure lower-ranked items are removed", ref returnLower);
			if (old != returnLower)
				RankComp.Get().dirty = true;
			options.Label("This also will require more processing time. Disable it if there seems to be a slowdown problem.");
			options.Label("This assumes a stockpile with lower priority with space for lower-ranked items.");
			options.Label("Items would normally be returned when there is a spot in the ranked stockpile open (Not by design but due to internal cache)");
			options.Gap();
			options.Label("(technically speaking, this option is a toggle: Process the ranked filters using either with all things, or only the things that need hauling. Some things don't know they could be hauled since the game's hauling system doesn't know about ranked stockpiles)");
			options.Gap();

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref returnLower, "returnLower", true);
		}
	}
}