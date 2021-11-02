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
        public static readonly int AllowedDjinnGap = 3;
        [JsonIgnore] public List<Djinn> Djinn { get; set; } = new List<Djinn>();
        [JsonIgnore] public List<Summon> Summons { get; set; } = new List<Summon>();

        public List<DjinnHolder> DjinnStorage
        {
            get { return Djinn.Count == 0 ? null : Djinn?.Select(d => new DjinnHolder() { Djinn = d.Djinnname, Nickname = d.Nickname, Shiny = d.IsShiny }).ToList(); }

            set { Djinn = value?.Select(s => DjinnAndSummonsDatabase.TryGetDjinn(s, out Djinn dj) ? dj : null).Where(c => c != null).ToList() ?? new List<Djinn>(); }
        }
        public List<SummonHolder> SummonStorage
        {
            get { return Summons.Count == 0 ? null : Summons?.Select(d => new SummonHolder() { Summon = d.Name }).ToList(); }

            set { Summons = value?.Select(s => DjinnAndSummonsDatabase.TryGetSummon(s.Summon, out Summon sum) ? sum : null).Where(c => c != null).ToList() ?? new List<Summon>(); }
        }
        public List<Element> DjinnSetup { get; set; } = new List<Element>();
        public int PocketUpgrades { get; set; } = 0;
        [JsonIgnore] public int PocketSize { get => Math.Min(70, BasePocketSize + PocketUpgrades * 2) + Djinn.Count(d => d.IsEvent); }

        public class DjinnHolder
        {
            public string Djinn { get; set; } = "";
            public string Nickname { get; set; } = "";
            public bool Shiny { get; set; } = false;
        }

        public class SummonHolder
        {
            public string Summon { get; set; } = "";
        }

        public List<Djinn> GetDjinns(List<Djinn> BlackList = null)
        {
            BlackList ??= new List<Djinn>();
            var Added = new List<Djinn>();
            var djinns = new List<Djinn>();

            foreach (var el in new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury })
            {
                if (djinns.Count < MaxDjinn)
                {
                    var selected = Djinn.OfElement(el)
                        .Where(d => !BlackList.Any(k => k.Djinnname.Equals(d.Djinnname)) && !Added.Any(k => k.Djinnname.Equals(d.Djinnname)))
                        .Distinct(new DjinnComp())
                        .Take(DjinnSetup.Count(d => d == el));
                    foreach (var d in selected)
                    {
                        d.UpdateMove();
                    }
                    djinns.AddRange(selected);
                    Added.AddRange(selected);
                    
                }
            }
            return djinns;
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

        public bool AddDjinn(string DjinnName)
        {
            if (!DjinnAndSummonsDatabase.TryGetDjinn(DjinnName, out Djinn djinn))
            {
                return false;
            }
            return AddDjinn(djinn);
        }

        public bool AddDjinn(Djinn newDjinn)
        {
            var djinnOfElement = Djinn.GroupBy(d => d.Element).Select(s => s.Count()).ToArray();
            if(newDjinn.IsEvent && Djinn.Any(d => d.Djinnname == newDjinn.Djinnname)){
                return true;
            }
            //var minDjinn = 0;
            //minDjinn = djinnOfElement.Count() > 0 ? djinnOfElement.Min() : 0;
            //if (djinn.OfElement(newDjinn.Element).Count() - minDjinn < AllowedDjinnGap && djinn.Count < PocketSize)
            if (Djinn.Count < PocketSize)
            {
                Djinn.Add(newDjinn);
                return true;
            }
            return false;
        }
        public Djinn GetDjinn(string DjinnName)
        {
            Djinn d = null;
            List<Djinn> list = Djinn;
            d ??= list.FirstOrDefault(d => DjinnName.Equals(d.Djinnname, StringComparison.CurrentCultureIgnoreCase) && d.Nickname.IsNullOrEmpty());
            d ??= list.FirstOrDefault(d => DjinnName.Equals(d.Djinnname, StringComparison.CurrentCultureIgnoreCase));
            d ??= list.FirstOrDefault(d => DjinnName.Equals(d.Nickname, StringComparison.CurrentCultureIgnoreCase));
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
            DjinnSetup.Clear();
            Summons.Clear();
            PocketUpgrades = 0;
        }
    }
}