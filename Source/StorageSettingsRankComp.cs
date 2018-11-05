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
		public Dictionary<StorageSettings, ThingFilter> usedFilter = new Dictionary<StorageSettings, ThingFilter>();
		public bool dirty = false;

		public RankComp(Game game) : base() { }

		public static RankComp Get()
		{
			return Current.Game.GetComponent<RankComp>();
		}

		public override void GameComponentTick()
		{
			base.GameComponentTick();

			if (dirty)
			{
				DetermineUsedFilters();
				dirty = false;
			}
		}

		public override void LoadedGame()
		{
			base.LoadedGame();

			//pawns tick before game component so this needs to be set up first... probably.
			DetermineUsedFilters();
		}

		public void DetermineUsedFilters()
		{
			usedFilter.Clear();

			foreach (var kvp in rankedSettings)
				DetermineUsedFilter(kvp.Key, kvp.Value);
		}

		public static void DetermineUsedFilter(StorageSettings settings)
		{
			var comp = Get();
			comp.usedFilter.Remove(settings);
			var ranks = GetRanks(settings, false);
			if (ranks == null) return;

			comp.DetermineUsedFilter(settings, ranks);
		}

		public void DetermineUsedFilter(StorageSettings settings, List<ThingFilter> ranks)
		{
			//Find map
			Map map = null;
			if (settings.owner is IHaulDestination haulDestination)
				map = haulDestination.Map;
			else if (settings.owner is CompChangeableProjectile thingComp)
				map = thingComp.parent.Map;
			else if (settings.owner is ISlotGroupParent slotGroupParent)//probably redundant
				map = slotGroupParent.Map;

			if (map == null)
				return;

			Log.Message($"DetermineUsedFilter for {settings.owner}");

			//First filter is just the one in the settings
			ThingFilter bestFilter = settings.filter;

			//Find haulables that are in lower priority storage
			//Don't check if they are valid for that storage, since that would call filter.Allows() but wouldn't check lower-ranked filters
			//listerHaulables isn't perfect since things in good storage aren't listed
			//listerHaulables doesn't know if a higher-rank filter would apply, because we use that list to determine if the higher-rank applies to begin with...
			//It's a circular dependency
			//listerHaulables will get refilled when an item is missing from the ranked storage, 
			//so then things are all haulable to higher priority and put in listerHaulables 
			//and then DetermineUsedFilter finds which is best and sets the filter 
			//so then listerHaulables removes things that fit the higher-rank filter

			List<Thing> haulables;
			if (Settings.Get().returnLower)
				haulables = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways).
					FindAll(t => !t.IsForbidden(Faction.OfPlayer) &&
					(StoreUtility.CurrentHaulDestinationOf(t)?.GetStoreSettings().Priority ?? StoragePriority.Unstored) < settings.Priority &&
					ranks.Last().Allows(t));
			else
				haulables = map.listerHaulables.ThingsPotentiallyNeedingHauling().
					FindAll(t => 
					(StoreUtility.CurrentHaulDestinationOf(t)?.GetStoreSettings().Priority ?? StoragePriority.Unstored) < settings.Priority &&
					ranks.Last().Allows(t));

			Log.Message($"haulables are {haulables.ToStringSafeEnumerable()}");
			//Loop but don't include last filter
			for (int i = 0; i < ranks.Count; i++)
			{
				Log.Message($"does rank {i+1} work?");
				//something matches this filter? Then that's the one to use
				if (haulables.Any(t => bestFilter.Allows(t)))
				{
					Log.Message($"(yes)");
					usedFilter[settings] = bestFilter;
					return;
				}
				Log.Message($"trying rank {i+2}");
				bestFilter = ranks[i];
			}

			//If all other filters had nothing available, the last filter is used:
			usedFilter[settings] = bestFilter;
		}

		public static ThingFilter UsedFilter(StorageSettings settings)
		{
			var dict = Get().usedFilter;
			if (dict.TryGetValue(settings, out ThingFilter used))
			{
				return used;
			}
			return settings.filter;
		}

		public static List<ThingFilter> GetRanks(StorageSettings settings, bool create = true)
		{
			var dict = Get().rankedSettings;
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
			var dict = Get().rankedSettings;
			return dict.ContainsKey(settings);
		}

		public static void RemoveRanks(StorageSettings settings)
		{
			var dict = Get().rankedSettings;
			dict.Remove(settings);
		}

		public static int CountExtraFilters(StorageSettings settings)
		{
			return GetRanks(settings, false)?.Count ?? 0;
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
