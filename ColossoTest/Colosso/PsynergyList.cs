using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ColossoTest.Colosso
{
    public class PsynergyList
    {
        private static Dictionary<string, OffensivePsynergy> offpsy = new Dictionary<string, OffensivePsynergy>();
        private static Dictionary<string, HealPsynergy> healpsy = new Dictionary<string, HealPsynergy>();
        private static Dictionary<string, StatusPsynergy> statpsy = new Dictionary<string, StatusPsynergy>();
        private static Dictionary<string, Psynergy> otherPsynergy = new Dictionary<string, Psynergy>();

        static PsynergyList()
        {
            string json = File.ReadAllText("Resources/offpsy.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            offpsy = data.ToObject<Dictionary<string, OffensivePsynergy>>();

            json = File.ReadAllText("Resources/healpsy.json");
            data = JsonConvert.DeserializeObject<dynamic>(json);
            healpsy = data.ToObject<Dictionary<string, HealPsynergy>>();

            json = File.ReadAllText("Resources/statpsy.json");
            data = JsonConvert.DeserializeObject<dynamic>(json);
            statpsy = data.ToObject<Dictionary<string, StatusPsynergy>>();

            otherPsynergy.Add("Revive", new Revive());
        }

        public static Psynergy GetPsynergy(string psynergy)
        {
            if (offpsy.ContainsKey(psynergy))
            {
                return offpsy[psynergy];
            }
            else if (healpsy.ContainsKey(psynergy))
            {
                return healpsy[psynergy];
            }
            else if (statpsy.ContainsKey(psynergy))
            {
                return statpsy[psynergy];
            }
            else if (otherPsynergy.ContainsKey(psynergy))
            {
                return otherPsynergy[psynergy];
            }

            return new OffensivePsynergy("Not Implemented", "", Target.otherSingle, 1, Psynergy.Element.Venus, 0, 0 ,0);
        }
    }
}
