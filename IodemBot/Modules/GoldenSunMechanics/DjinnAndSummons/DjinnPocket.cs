using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class DjinnPocket
    {
        public static readonly int MaxDjinn = 2;
        private List<Djinn> djinn = new List<Djinn>();
        [JsonIgnore] public List<Summon> summons = new List<Summon>();
        [JsonProperty] private List<DjinnHolder> DjinnName = new List<DjinnHolder>();
        [JsonProperty] private List<string> SummonNames = new List<string>();
        public List<Element> DjinnSetup { get; set; } = new List<Element>();

        private class DjinnHolder
        {
            public string Djinn { get; set; } = "";
            public string Nickname { get; set; } = "";
            public bool Shiny { get; set; } = false;
        }

        public List<Djinn> GetDjinns(List<Djinn> BlackList = null)
        {
            BlackList = BlackList ?? new List<Djinn>();
            var djinns = new List<Djinn>();
            foreach (var el in new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury })
            {
                if (djinns.Count < MaxDjinn)
                {
                    djinns.AddRange(djinn.Where(d => d.Element == el).Where(d => !BlackList.Any(k => k.Djinnname.Equals(d.Djinnname))).Take(DjinnSetup.Count(d => d == el)));
                }
            }
            return djinns;
        }

        public void Initialize()
        {
            DjinnName.ForEach(d =>
            {
                var x = DjinnAndSummonsDatabase.GetDjinn(d.Djinn);
                x.IsShiny = d.Shiny;
                x.Nickname = d.Nickname;
                x.UpdateMove();
                djinn.Add(x);
            }
            );
            SummonNames.ForEach(s => summons.Add(DjinnAndSummonsDatabase.GetSummon(s)));
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
            DjinnName = djinn.Select(d => new DjinnHolder() { Djinn = d.Djinnname, Nickname = d.Nickname, Shiny = d.IsShiny }).ToList();
            SummonNames = summons.Select(s => s.Name).ToList();
        }
    }
}