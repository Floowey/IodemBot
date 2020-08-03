using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class DjinnPocket
    {
        public static readonly int MaxDjinn = 2;
        public static readonly int BasePocketSize = 6;
        public static readonly int AllowedDjinnGap = 3;
        [JsonIgnore] public List<Djinn> djinn = new List<Djinn>();
        [JsonIgnore] public List<Summon> summons = new List<Summon>();
        [JsonProperty] private List<DjinnHolder> DjinnStorage = new List<DjinnHolder>();
        [JsonProperty] private List<SummonHolder> SummonStorage = new List<SummonHolder>();
        public List<Element> DjinnSetup { get; set; } = new List<Element>();
        public int PocketUpgrades = 0;
        public int PocketSize { get => Math.Min(60, BasePocketSize + PocketUpgrades * 2); }

        private class DjinnHolder
        {
            public string Djinn { get; set; } = "";
            public string Nickname { get; set; } = "";
            public bool Shiny { get; set; } = false;
        }

        private class SummonHolder
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
                    var selected = djinn.OfElement(el)
                        .Where(d => !BlackList.Any(k => k.Djinnname.Equals(d.Djinnname)) && !Added.Any(k => k.Djinnname.Equals(d.Djinnname)))
                        .Distinct(new DjinnComp())
                        .Take(DjinnSetup.Count(d => d == el));

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
            var djinnOfElement = djinn.GroupBy(d => d.Element).Select(s => s.Count()).ToArray();
            //var minDjinn = 0;
            //minDjinn = djinnOfElement.Count() > 0 ? djinnOfElement.Min() : 0;
            //if (djinn.OfElement(newDjinn.Element).Count() - minDjinn < AllowedDjinnGap && djinn.Count < PocketSize)
            if (djinn.Count < PocketSize)
            {
                djinn.Add(newDjinn);
                return true;
            }
            return false;
        }
        public Djinn GetDjinn(string DjinnName)
        {
            return djinn
                .Where(d => DjinnName.Equals(d.Djinnname, StringComparison.CurrentCultureIgnoreCase) || DjinnName.Equals(d.Nickname, StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();
        }
        public void AddSummon(Summon newSummon)
        {
            summons.Add(newSummon);
            summons = summons
                .OrderBy(s => s.MercuryNeeded)
                .ThenBy(s => s.JupiterNeeded)
                .ThenBy(s => s.MarsNeeded)
                .ThenBy(s => s.VenusNeeded)
                .ToList();
        }
        public void Initialize()
        {
            DjinnStorage.ForEach(d =>
            {
                var x = DjinnAndSummonsDatabase.GetDjinn(d.Djinn);
                x.IsShiny = d.Shiny;
                x.Nickname = d.Nickname;
                x.UpdateMove();
                djinn.Add(x);
            }
            );
            SummonStorage.ForEach(s => summons.Add(DjinnAndSummonsDatabase.GetSummon(s.Summon)));
        }
        public void Clear()
        {
            djinn.RemoveAll(d => !d.IsShiny);
            DjinnSetup.Clear();
            summons.Clear();
            PocketUpgrades = 0;
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            Initialize();
        }

        [OnSerializing]
        public void OnSerializing(StreamingContext context)
        {
            UpdateStrings();
        }

        private void UpdateStrings()
        {
            DjinnStorage = djinn.Select(d => new DjinnHolder() { Djinn = d.Djinnname, Nickname = d.Nickname, Shiny = d.IsShiny }).ToList();
            SummonStorage = summons.Select(s => new SummonHolder() { Summon = s.Name }).ToList();
        }
    }
}