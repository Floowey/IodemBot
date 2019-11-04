using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class DjinnAndSummonsDatabase
    {
        private static Dictionary<string, Djinn> DjinnDatabase { get; set; } = new Dictionary<string, Djinn>();
        private static Dictionary<string, Summon> SummonsDatabase { get; set; } = new Dictionary<string, Summon>();

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

        public static bool TryGetDjinn(string DjinnName, out Djinn djinn)
        {
            if (DjinnDatabase.TryGetValue(DjinnName, out djinn))
            {
                return true;
            }

            Console.WriteLine($"Djinn {DjinnName} is not implemented.");
            return false;
        }

        public static bool TryGetSummon(string SummonName, out Summon summon)
        {
            if (SummonsDatabase.TryGetValue(SummonName, out summon))
            {
                return true;
            }

            Console.WriteLine($"Summon {SummonName} is not implemented.");
            return false;
        }
    }
}