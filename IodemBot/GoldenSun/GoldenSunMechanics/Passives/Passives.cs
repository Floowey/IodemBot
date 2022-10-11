using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Passives
    {
        public static readonly List<Passive> AllPassives = new() {
            new(){Name="Stone Skin", elements = new[]{Element.Venus}, Description="A thick, lithic coat covers your skin, reducing damage taken by 10/30/50%.", args=new[]{0.10/0.30/0.50} },
            new(){Name="Instant Ignition", elements = new[]{Element.Mars}, Description="The thought of battle fires you up, increasing damage by 5/15/25%" },
            new(){Name="Tail Wind", elements = new[]{Element.Jupiter}, Description="A breeze from behind makes you quicker, allowing you to get your Status Moves/Offensive Psynergy/All Moves in first." },
            new(){Name="Soothing Song", elements = new[]{Element.Mercury}, Description="Collecting your thoughts at the beginning of battle recovers any ailments." },
            new(){Name="Vital Spark", elements = new[]{Element.Venus, Element.Mars}, Description="Your spark of life never fades and kickstarts you back into battle at 5/15/25%" },
            new(){Name="Fiery Reflex", elements = new[]{Element.Mars, Element.Jupiter}, Description="Fast reaction speed allows you to quickly strike back on any incoming attacks." },
            new(){Name="Brisk Flow", elements = new[]{Element.Jupiter, Element.Mercury}, Description="Surrounded by psynergy in fluid state, your PP recovers by 5/15/25%" },
            new(){Name="Petrichor Scent", elements = new[]{Element.Mercury, Element.Venus}, Description="The smell of fresh rain makes you feel so good, it heals you 10/25/33% of your max HP" }
        };

        public static Passive GetPassive(string name) => AllPassives.First(p => p.Name == name);

        public List<Passive> UnlockedPassives = new();
        public string SelectedPassive { get; set; } = "";

        public Passive GetSelectedPassive()
        {
            return GetPassive(SelectedPassive);
        }

        public int GetPassiveLevel(OathList oaths)
        {
            var passive = GetPassive(SelectedPassive);
            var ps = passive.elements.Select(e => (int)oaths.GetOathCompletion(Enum.Parse<Oath>(e.ToString())));
            return Math.Min(2, ps.Aggregate(1, (a, b) => a * b));
            //return ps.Min();
        }
    }

    public struct Passive
    {
        public string Name;
        public Element[] elements;
        public string Description;
        public double[] args;
    }
}