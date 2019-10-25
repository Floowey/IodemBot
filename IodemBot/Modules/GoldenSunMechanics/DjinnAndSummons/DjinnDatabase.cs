using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace IodemBot.Modules.GoldenSunMechanics.DjinnAndSummons
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
    }
}