using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Passives
    {
        public static readonly List<Passive> AllPassives = new() {
            new(){Name="Stone Skin", elements = new[]{Element.Venus},
                Description="A thick, lithic coat covers your skin, reducing damage taken by 10/30/50%.",
                ShortDescription="Reduce damage by 10/30/50%",
                args=new[]{0.10,0.30,0.50} },
            new(){Name="Instant Ignition", elements = new[]{Element.Mars},
                Description="The thought of battle fires you up, increasing damage by 5/15/25%",
                ShortDescription="Increase damage by 5/15/25%",
                args=new[]{1.05,1.15,1.25} },
            new(){Name="Tail Wind", elements = new[]{Element.Jupiter},
                Description="A breeze from behind makes you quicker, allowing you to get your Status Moves/Offensive Psynergy/All Moves in first.",
                ShortDescription="Act faster in battle.",},
            new(){Name="Soothing Song", elements = new[]{Element.Mercury},
                Description="Collecting your thoughts at the beginning of battle recovers any ailments.",
                ShortDescription="Recover from ailments",},
            new(){Name="Vital Spark", elements = new[]{Element.Venus, Element.Mars},
                Description="Your spark of life never fades and kickstarts you back into battle at 5/15/25%",
                ShortDescription="Revive to 5/15/25%",args=new[]{5.0,15.0,25.0} },
            new(){Name="Fiery Reflex", elements = new[]{Element.Mars, Element.Jupiter},
                Description="Fast reaction speed allows you to quickly strike back on any incoming attacks.",
                ShortDescription="Strike back when attacked.",},
            new(){Name="Brisk Flow", elements = new[]{Element.Jupiter, Element.Mercury},
                Description="Surrounded by psynergy in fluid state, your PP recovers by 5/15/25%",
                ShortDescription="Recover 5/15/25% PP", args=new[]{0.5,0.15,0.25} },
            new(){Name="Petrichor Scent", elements = new[]{Element.Mercury, Element.Venus},
                Description="The smell of fresh rain makes you feel so good, it heals you 10/25/33% of your max HP",
                ShortDescription="Recover 10/25/33% HP", args=new[]{0.1,0.25,0.33} }
        };

        public static Passive GetPassive(string name) => AllPassives.FirstOrDefault(p => p.Name == name);

        [JsonIgnore]
        public List<Passive> UnlockedPassives { get; set; } = new();

        public List<string> UnlockedPassivesStorage
        {
            get
            {
                return UnlockedPassives.Count == 0 ? null : UnlockedPassives?.Select(p => p.Name).ToList();
            }
            set
            {
                UnlockedPassives = value?.Select(s => GetPassive(s)).ToList() ?? new List<Passive>();
            }
        }

        public string SelectedPassive { get; set; } = "";

        public void AddPassive(params Passive[] ps)
        {
            foreach (var p in ps)
            {
                UnlockedPassives.Add(p);
            }
        }

        public Passive GetSelectedPassive()
        {
            return GetPassive(SelectedPassive);
        }

        public int GetPassiveLevel(OathList oaths)
        {
            var passive = GetPassive(SelectedPassive);
            if (!passive.Name.IsNullOrEmpty())
            {
                return GetPassiveLevel(passive, oaths);
            }
            else return 0;
            //return ps.Min();
        }

        public static int GetPassiveLevel(Passive passive, OathList oaths)
        {
            var ps = passive.elements.Select(e => (int)oaths.GetOathCompletion(Enum.Parse<Oath>(e.ToString()))).ToList();
            return Math.Min(2, ps.Aggregate(1, (a, b) => a * b));
        }
    }

    public struct Passive
    {
        public string Name = "";
        public Element[] elements = Array.Empty<Element>();
        public string Description = "";
        public string ShortDescription = "";
        public double[] args = Array.Empty<double>();

        public Passive()
        {
        }
    }
}