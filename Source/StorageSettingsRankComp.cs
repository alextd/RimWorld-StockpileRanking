using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;

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
			return Current.Game?.GetComponent<RankComp>();
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

		public void DetermineUsedFilter(StorageSettings settings, List<ThingFilter> ranks)
		{
			usedFilter.Remove(settings);
			if (ranks == null) return;

			//Find map
			Map map = (settings.owner as IHaulDestination)?.Map;

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
			if (Mod.settings.returnLower)
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
		//This one is called from ilcode where it'd be tricky to get RankComp in front of arg list
		public static ThingFilter UsedFilter(StorageSettings settings)
		{
			var comp = Get();
			if (comp != null && comp.usedFilter.TryGetValue(settings, out ThingFilter used))
			{
				return used;
			}
			return settings.filter;
		}
		
		public List<ThingFilter> GetRanks(StorageSettings settings, bool create = true)
		{
			var dict = rankedSettings;
			if (dict.TryGetValue(settings, out List<ThingFilter> list))
				return list;
			else if (!create)
				return null;
			List<ThingFilter> newList = new List<ThingFilter>();
			dict[settings] = newList;
			return newList;
		}

		public bool HasRanks(StorageSettings settings)
		{
			return rankedSettings.ContainsKey(settings);
		}

		public int CountExtraFilters(StorageSettings settings)
		{
			return GetRanks(settings, false)?.Count ?? 0;
		}

		public ThingFilter GetLowestFilter(StorageSettings settings)
		{
			return GetRanks(settings, false)?.Last() ?? settings.filter;
		}

		public void CascadeDown(StorageSettings settings)
		{
			Log.Message($"Cascade down {settings.owner}");
			List<ThingFilter> ranks = GetRanks(settings, false);
			if (ranks == null) return;

			ThingFilter higher = settings.filter;
			for (int i = 0; i < ranks.Count; i++)
			{
				ThingFilter lower = ranks[i];
				lower.Add(higher);
				higher = lower;
			}
		}

		public static FieldInfo settingsChangedCallbackInfo = AccessTools.Field(typeof(ThingFilter), "settingsChangedCallback");
		public static Action SettingsChangedAction(StorageSettings settings) => settingsChangedCallbackInfo.GetValue(settings.filter) as Action;
		public void AddFilter(StorageSettings settings, ThingFilter filter = null)
		{
			if (filter == null)
				filter = GetLowestFilter(settings);
			ThingFilter newFilter = new ThingFilter(SettingsChangedAction(settings));
			newFilter.CopyAllowancesFrom(filter);
			GetRanks(settings).Add(newFilter);
		}

		public void SetRanks(StorageSettings settings, List<ThingFilter> newRanks)
		{
			if (newRanks == null)
			{
				rankedSettings.Remove(settings);
			}
			else
			{
				rankedSettings[settings] = newRanks;
				foreach (ThingFilter filter in newRanks)
					settingsChangedCallbackInfo.SetValue(filter, SettingsChangedAction(settings));
			}
		}

		public static void CopyFrom(StorageSettings settings, StorageSettings other)
		{
			var comp = Get();
			if (comp == null)	//fixed storage settings will copy do this copy on game load
				return;

			List<ThingFilter> otherRanks = comp.GetRanks(other, false);
			if (otherRanks == null)
			{
				comp.rankedSettings.Remove(settings);
			}
			else
			{
				comp.GetRanks(settings).Clear();
				foreach (ThingFilter otherFilter in otherRanks)
					comp.AddFilter(settings, otherFilter);
			}

			comp.DetermineUsedFilter(settings, comp.GetRanks(settings, false));
		}

		//This one is called from ilcode where it'd be tricky to get RankComp in front of arg list
		public static ThingFilter GetFilter(StorageSettings settings, int rank)
		{
			var comp = Get();
			return comp == null || rank == 0 ? settings.filter : comp.GetRanks(settings)[rank - 1];
		}

		public static MethodInfo TryNotifyChangedInfo = AccessTools.Method(typeof(StorageSettings), "TryNotifyChanged");
		public void RemoveFilter(StorageSettings settings, int rank)
		{
			if (rank == 0) return;//sanity check
			List<ThingFilter> ranks = GetRanks(settings);
			if (ranks.Count == 1)
			{
				rankedSettings.Remove(settings);
			}
			else
			{
				ranks.RemoveAt(rank - 1);
			}

			TryNotifyChangedInfo.Invoke(settings, null);
			DetermineUsedFilter(settings, GetRanks(settings, false));
		}
	}
}
