using System.Reflection;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace Stockpile_Ranking
{
	public class Mod : Verse.Mod
	{
		public Mod(ModContentPack content) : base(content)
		{
			// initialize settings
			GetSettings<Settings>();
#if DEBUG
			Harmony.DEBUG = true;
#endif

			Harmony harmony = new Harmony("Uuugggg.rimworld.Stockpile_Ranking.main");

			harmony.PatchAll();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);
			GetSettings<Settings>().DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "TD.StockpileRanking".Translate();
		}
	}
}