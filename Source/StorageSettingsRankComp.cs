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
		public struct Key
		{
			StorageSettings st; int rank;
			public Key(StorageSettings s, int r) { st = s; rank = r; }
		}
		public Dictionary<Key, ThingFilter> rankedSettings = new Dictionary<Key, ThingFilter>();

		public RankComp(Game game) : base() { }

		public static Dictionary<Key, ThingFilter> Get()
		{
			return Current.Game.GetComponent<RankComp>().rankedSettings;
		}

		public static int CountFilters(StorageSettings settings)
		{
			var dict = Get();
			for (int k = 1; k < 100;k++)//100 is beyond reasonable usage
			{
				Key key = new Key(settings, k);
				if (!dict.ContainsKey(key))
					return k;
			}
			return 100;
		}

		public static void SetFilter(StorageSettings settings, int rank, ThingFilter filter)
		{
			var dict = Get();
			dict[new Key(settings, rank)] = filter;
		}

		public static ThingFilter GetFilter(StorageSettings settings, int rank)
		{
			var dict = Get();
			if (dict.TryGetValue(new Key(settings, rank), out ThingFilter filter))
				return filter;
			return settings.filter;
		}

		public static void Remove(StorageSettings settings, int rank)
		{
			var dict = Get();
			dict.Remove(new Key(settings, rank));
		}
	}
}
