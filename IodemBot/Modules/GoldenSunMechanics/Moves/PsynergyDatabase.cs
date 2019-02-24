using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class PsynergyDatabase
    {
        private static Dictionary<string, OffensivePsynergy> offpsy = new Dictionary<string, OffensivePsynergy>();
        private static Dictionary<string, HealPsynergy> healpsy = new Dictionary<string, HealPsynergy>();
        private static Dictionary<string, StatusPsynergy> statpsy = new Dictionary<string, StatusPsynergy>();
        private static Dictionary<string, Psynergy> otherPsynergy = new Dictionary<string, Psynergy>();

        static PsynergyDatabase()
        {
            string json = File.ReadAllText("Resources/offpsy.json");
            offpsy = JsonConvert.DeserializeObject<Dictionary<string, OffensivePsynergy>>(json);

            json = File.ReadAllText("Resources/healpsy.json");
            healpsy = JsonConvert.DeserializeObject<Dictionary<string, HealPsynergy>>(json);

            json = File.ReadAllText("Resources/statpsy.json");
            statpsy = JsonConvert.DeserializeObject<Dictionary<string, StatusPsynergy>>(json);

            otherPsynergy.Add("Revive", new Revive());
            otherPsynergy.Add("Phoenix", new Phoenix());
            otherPsynergy.Add("Break", new Break());
        }

        public static Psynergy GetPsynergy(string psynergy)
        {
            if (offpsy.ContainsKey(psynergy))
            {
                return (OffensivePsynergy)Clone(offpsy[psynergy]).Clone();
            }
            else if (healpsy.ContainsKey(psynergy))
            {
                return (HealPsynergy) healpsy[psynergy].Clone();
            }
            else if (statpsy.ContainsKey(psynergy))
            {
                return (StatusPsynergy) statpsy[psynergy].Clone();
            }
            else if (otherPsynergy.ContainsKey(psynergy))
            {
                return (Psynergy) otherPsynergy[psynergy].Clone();
            }

            return new OffensivePsynergy($"{psynergy} (Not Implemented!)", "⛔", Target.otherSingle, 1, Psynergy.Element.none, 0, 1 ,0, 1);
        }

        public static T Clone<T>(T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }
    }
}
