using IodemBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class DjinnAndSummonsDatabase
    {
        private static Dictionary<string, Djinn> DjinnDatabase { get; set; } = new Dictionary<string, Djinn>();
        private static Dictionary<string, Summon> SummonsDatabase { get; set; } = new Dictionary<string, Summon>();
        private static readonly string[] blacklist = new[] { "Kite", "Lull", "Aurora", "Eddy" };

        static DjinnAndSummonsDatabase()
        {
            try
            {
                string json = File.ReadAllText("Resources/GoldenSun/DjinnAndSummons/Djinn.json");
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

        public static Djinn GetDjinn(string DjinnName)
        {
            if (!TryGetDjinn(DjinnName, out Djinn djinn))
            {
                djinn = new Djinn() { Element = Element.Venus, Name = $"{DjinnName} NOT IMPLEMENTED" };
            }
            return (Djinn)djinn.Clone();
        }

        public static Summon GetSummon(string SummonName)
        {
            if (!TryGetSummon(SummonName, out Summon summon))
            {
                summon = new Summon() { Name = $"{SummonName} NOT IMPLEMENTED" };
            }
            return summon;
        }

        public static Djinn GetRandomDjinn(params Element[] elements)
        {
            return (Djinn)DjinnDatabase.Values
                .Where(d => (elements.Count() > 0 ? elements.Contains(d.Element) : true) && !blacklist.Contains(d.Name))
                .Random()
                .Clone();
        }

        public static bool TryGetDjinn(string DjinnName, out Djinn djinn)
        {
            djinn = null;
            if (DjinnName.IsNullOrEmpty())
            {
                return false;
            }
            if (DjinnDatabase.TryGetValue(DjinnName, out Djinn d))
            {
                djinn = (Djinn)d.Clone();
                return true;
            }
            Console.WriteLine($"Djinn {DjinnName} is not implemented.");
            return false;
        }

        public static bool TryGetSummon(string SummonName, out Summon summon)
        {
            if (SummonName.IsNullOrEmpty())
            {
                summon = null;
                return false;
            }
            if (SummonsDatabase.TryGetValue(SummonName, out summon))
            {
                return true;
            }

            Console.WriteLine($"Summon {SummonName} is not implemented.");
            return false;
        }
    }
}