using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class DjinnPocket
    {
        public static readonly int MaxDjinn = 2;
        public static readonly int BasePocketSize = 6;

        public int DjinnEssences { get; set; } = 0;
        [JsonIgnore] public List<Djinn> Djinn { get; set; } = new();
        [JsonIgnore] public List<Summon> Summons { get; set; } = new();

        public List<DjinnHolder> DjinnStorage
        {
            get
            {
                return Djinn.Count == 0
                    ? null
                    : Djinn?.Select(
                        d => new DjinnHolder { Djinn = d.Djinnname, Nickname = d.Nickname, Shiny = d.IsShiny }).ToList();
            }

            set
            {
                Djinn = value?.Select(s => DjinnAndSummonsDatabase.TryGetDjinn(s, out var dj) ? dj : null)
                    .Where(c => c != null).ToList() ?? new List<Djinn>();
            }
        }

        public List<SummonHolder> SummonStorage
        {
            get
            {
                return Summons.Count == 0 ? null : Summons?.Select(d => new SummonHolder { Summon = d.Name }).ToList();
            }

            set
            {
                Summons = value?.Select(s => DjinnAndSummonsDatabase.TryGetSummon(s.Summon, out var sum) ? sum : null)
                    .Where(c => c != null).ToList() ?? new List<Summon>();
            }
        }

        public List<Element> DjinnSetup { get; set; } = new();
        public int PocketUpgrades { get; set; }

        [JsonIgnore]
        public int PocketSize => Math.Min(70, BasePocketSize + PocketUpgrades * 2) + Djinn.Count(d => d.IsEvent);

        public List<Djinn> GetDjinns(List<Djinn> blackList = null)
        {
            blackList ??= new List<Djinn>();
            var added = new List<Djinn>();
            var djinns = new List<Djinn>();

            foreach (var el in new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury })
                if (djinns.Count < MaxDjinn)
                {
                    var selected = Djinn.OfElement(el)
                        .Where(d => !blackList.Any(k => k.Djinnname.Equals(d.Djinnname)) &&
                                    !added.Any(k => k.Djinnname.Equals(d.Djinnname)))
                        .Distinct(new DjinnComp())
                        .Take(DjinnSetup.Count(d => d == el));
                    foreach (var d in selected) d.UpdateMove();
                    djinns.AddRange(selected);
                    added.AddRange(selected);
                }

            return djinns;
        }

        public bool AddDjinn(string djinnName)
        {
            if (!DjinnAndSummonsDatabase.TryGetDjinn(djinnName, out var djinn)) return false;
            return AddDjinn(djinn);
        }

        public bool AddDjinn(Djinn newDjinn)
        {
            if (newDjinn.IsEvent && Djinn.Any(d => d.Djinnname == newDjinn.Djinnname)) return true;
            if (Djinn.Count >= PocketSize) return false;
            Djinn.Add(newDjinn);
            return true;
        }

        public bool ReleaseDjinn(string djinnName)
        {
            if (!DjinnAndSummonsDatabase.TryGetDjinn(djinnName, out var djinn)) return false;
            return ReleaseDjinn(djinn);
        }

        public bool ReleaseDjinn(Djinn djinn)
        {
            DjinnEssences++;
            return Djinn.Remove(djinn);
        }

        public Djinn GetDjinn(string djinnName)
        {
            Djinn d = null;
            var list = Djinn;
            d = list.FirstOrDefault(d =>
                djinnName.Equals(d.Djinnname, StringComparison.CurrentCultureIgnoreCase) && d.Nickname.IsNullOrEmpty());
            d ??= list.FirstOrDefault(d => djinnName.Equals(d.Djinnname, StringComparison.CurrentCultureIgnoreCase));
            d ??= list.FirstOrDefault(d => djinnName.Equals(d.Nickname, StringComparison.CurrentCultureIgnoreCase));
            return d;
        }

        public void AddSummon(Summon newSummon)
        {
            Summons.Add(newSummon);
            Summons = Summons
                .OrderBy(s => s.MercuryNeeded)
                .ThenBy(s => s.JupiterNeeded)
                .ThenBy(s => s.MarsNeeded)
                .ThenBy(s => s.VenusNeeded)
                .ToList();
        }

        public void Clear()
        {
            Djinn.RemoveAll(d => !(d.IsShiny || d.IsEvent));
            Djinn.Clear();
            DjinnSetup.Clear();
            Summons.Clear();
            PocketUpgrades = 0;
        }

        public class DjinnHolder
        {
            public string Djinn { get; set; } = "";
            public string Nickname { get; set; } = "";
            public bool Shiny { get; set; }
        }

        public class SummonHolder
        {
            public string Summon { get; set; } = "";
        }

        private class DjinnComp : EqualityComparer<Djinn>
        {
            public override bool Equals(Djinn x, Djinn y)
            {
                return x.Djinnname.Equals(y.Djinnname, StringComparison.CurrentCultureIgnoreCase);
            }

            public override int GetHashCode(Djinn obj)
            {
                return obj.Djinnname.GetHashCode();
            }
        }
    }
}