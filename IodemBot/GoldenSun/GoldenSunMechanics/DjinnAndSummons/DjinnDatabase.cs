using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IodemBot.Extensions;
using Newtonsoft.Json;
using static IodemBot.Modules.GoldenSunMechanics.DjinnPocket;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class DjinnAndSummonsDatabase
    {
        public static readonly string[] Blacklist = { };

        static DjinnAndSummonsDatabase()
        {
            try
            {
                var json = File.ReadAllText("Resources/GoldenSun/DjinnAndSummons/Djinn.json");
                DjinnDatabase = new Dictionary<string, Djinn>(
                    JsonConvert.DeserializeObject<Dictionary<string, Djinn>>(json),
                    StringComparer.OrdinalIgnoreCase);

                json = File.ReadAllText("Resources/GoldenSun/DjinnAndSummons/Summons.json");
                SummonsDatabase = new Dictionary<string, Summon>(
                    JsonConvert.DeserializeObject<Dictionary<string, Summon>>(json),
                    StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception e)
            {
                //Just for debugging.
                Console.WriteLine(e.ToString());
            }
        }

        public static Dictionary<string, Djinn> DjinnDatabase { get; } = new();
        public static Dictionary<string, Summon> SummonsDatabase { get; } = new();

        public static Djinn GetDjinn(string djinnName)
        {
            if (!TryGetDjinn(djinnName, out var djinn))
                djinn = new Djinn { Element = Element.Venus, Name = $"{djinnName} NOT IMPLEMENTED" };
            return (Djinn)djinn.Clone();
        }

        public static Djinn GetDjinn(DjinnHolder djinn)
        {
            var d = GetDjinn(djinn.Djinn);
            d.IsShiny = djinn.Shiny;
            d.Nickname = djinn.Nickname;
            d.UpdateMove();
            return d;
        }

        public static Summon GetSummon(string summonName)
        {
            if (!TryGetSummon(summonName, out var summon)) summon = new Summon { Name = $"{summonName} NOT IMPLEMENTED" };
            return summon;
        }

        public static Djinn GetRandomDjinn(params Element[] elements)
        {
            return (Djinn)DjinnDatabase.Values
                .Where(d => (elements.Length == 0 || elements.Contains(d.Element)) && !d.IsEvent &&
                            !Blacklist.Contains(d.Name))
                .Random()
                .Clone();
        }

        public static bool TryGetDjinn(string djinnName, out Djinn djinn)
        {
            djinn = null;
            if (djinnName.IsNullOrEmpty()) return false;
            if (!DjinnDatabase.TryGetValue(djinnName, out var d)) return false;
            djinn = (Djinn)d.Clone();
            return true;

            //    Console.WriteLine($"Djinn {DjinnName} is not implemented.");
        }

        public static bool TryGetDjinn(DjinnHolder djinnHolder, out Djinn djinn)
        {
            djinn = null;
            if (djinnHolder.Djinn.IsNullOrEmpty()) return false;
            if (DjinnDatabase.TryGetValue(djinnHolder.Djinn, out var d))
            {
                djinn = (Djinn)d.Clone();
                djinn.Nickname = djinnHolder.Nickname;
                djinn.IsShiny = djinnHolder.Shiny;
                return true;
            }

            //    Console.WriteLine($"Djinn {DjinnName} is not implemented.");
            return false;
        }

        public static bool TryGetSummon(string summonName, out Summon summon)
        {
            if (summonName.IsNullOrEmpty())
            {
                summon = null;
                return false;
            }

            if (SummonsDatabase.TryGetValue(summonName, out summon)) return true;

            //Console.WriteLine($"Summon {SummonName} is not implemented.");
            return false;
        }
    }
}