using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;

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

		public static List<ThingFilter> GetRanks(StorageSettings settings, bool create = true)
		{
			var dict = Get();
			if (dict.TryGetValue(settings, out List<ThingFilter> list))
				return list;
			else if (!create)
				return null;
			List<ThingFilter> newList = new List<ThingFilter>();
			dict[settings] = newList;
			return newList;
		}

		public static bool HasRanks(StorageSettings settings)
		{
			var dict = Get();
			return dict.ContainsKey(settings);
		}

		public static void RemoveRanks(StorageSettings settings)
		{
			var dict = Get();
			dict.Remove(settings);
		}

		public static int CountExtraFilters(StorageSettings settings)
		{
			return GetRanks(settings).Count;
		}

		public static void AddFilter(StorageSettings settings, ThingFilter filter)
		{
			GetRanks(settings).Add(filter);
		}

		public static void CopyFrom(StorageSettings settings, StorageSettings other)
		{
			List<ThingFilter> otherRanks = GetRanks(other, false);
			if(otherRanks == null)
			{
				RemoveRanks(settings);
				return;
			}

			List<ThingFilter> ranks = GetRanks(settings);
			ranks.Clear();
			foreach(ThingFilter otherFilter in otherRanks)
			{
				ThingFilter filter = new ThingFilter();
				filter.CopyAllowancesFrom(otherFilter);
				ranks.Add(filter);
			}
		}

		public static ThingFilter GetFilter(StorageSettings settings, int rank)
		{
			return rank == 0 ? settings.filter : GetRanks(settings)[rank - 1];
		}

		public static void RemoveFilter(StorageSettings settings, int rank)
		{
			if (rank == 0) return;//sanity check
			GetRanks(settings).RemoveAt(rank - 1);
		}
	}
}
