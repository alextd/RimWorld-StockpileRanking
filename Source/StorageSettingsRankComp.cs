using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace Stockpile_Ranking
{
	public class RankComp : GameComponent
	{
		public Dictionary<StorageSettings, List<ThingFilter>> rankedSettings = new Dictionary<StorageSettings, List<ThingFilter>>();
		public RankComp(Game game) : base() { }

		public static Dictionary<StorageSettings, List<ThingFilter>> Get()
		{
			return Current.Game.GetComponent<RankComp>().rankedSettings;
		}

		public static List<ThingFilter> GetRanks(StorageSettings settings)
		{
			var dict = Get();
			if (dict.TryGetValue(settings, out List<ThingFilter> list))
				return list;
			List<ThingFilter> newList = new List<ThingFilter>();
			dict[settings] = newList;
			return newList;
		}

		public static int CountExtraFilters(StorageSettings settings)
		{
			return GetRanks(settings).Count;
		}

		public static void AddFilter(StorageSettings settings, ThingFilter filter)
		{
			GetRanks(settings).Add(filter);
		}

		public static ThingFilter GetFilter(StorageSettings settings, int rank)
		{
			return rank == 0 ? settings.filter : GetRanks(settings)[rank - 1];
		}
	}
}
