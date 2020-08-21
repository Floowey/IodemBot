using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using IodemBot.Extensions;
using LiteDB;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class DjinnPocket
    {
        public static readonly int MaxDjinn = 2;
        public static readonly int BasePocketSize = 6;
        public static readonly int AllowedDjinnGap = 3;
        [BsonIgnore] public List<Djinn> Djinn { get; set; }
        [BsonIgnore] public List<Summon> Summons { get; set; }

        public IEnumerable<DjinnHolder> DjinnStorage
        {
            get { return Djinn?.Select(d => new DjinnHolder() { Djinn = d.Djinnname, Nickname = d.Nickname, Shiny = d.IsShiny }); }

            set { Djinn = value.Select(s => DjinnAndSummonsDatabase.GetDjinn(s)).ToList() ; }
        }
        public IEnumerable<SummonHolder> SummonStorage
        {
            get { return Summons?.Select(d => new SummonHolder() { Summon = d.Name }); }

            set { Summons = value.Select(s => DjinnAndSummonsDatabase.GetSummon(s.Summon)).ToList(); }
        }
        public List<Element> DjinnSetup { get; set; } = new List<Element>();
        public int PocketUpgrades { get; set; } = 0;
        [BsonIgnore] public int PocketSize { get => Math.Min(60, BasePocketSize + PocketUpgrades * 2); }

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
            Djinn.RemoveAll(d => !d.IsShiny);
            DjinnSetup.Clear();
            Summons.Clear();
            PocketUpgrades = 0;
        }
    }
}