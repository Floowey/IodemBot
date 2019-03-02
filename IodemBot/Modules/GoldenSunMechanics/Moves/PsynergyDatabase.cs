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
        private static Dictionary<string, OffensivePsynergy> offpsy = new Dictionary<string, OffensivePsynergy>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, HealPsynergy> healpsy = new Dictionary<string, HealPsynergy>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, StatusPsynergy> statpsy = new Dictionary<string, StatusPsynergy>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, Psynergy> otherPsynergy = new Dictionary<string, Psynergy>(StringComparer.InvariantCultureIgnoreCase);

        static PsynergyDatabase()
        {
            try
            {
                string json = File.ReadAllText("Resources/offpsy.json");
                offpsy = new Dictionary<string, OffensivePsynergy>(
                    JsonConvert.DeserializeObject<Dictionary<string, OffensivePsynergy>>(json),
                    StringComparer.OrdinalIgnoreCase);

                json = File.ReadAllText("Resources/healpsy.json");
                healpsy = new Dictionary<string, HealPsynergy>(
                    JsonConvert.DeserializeObject<Dictionary<string, HealPsynergy>>(json),
                    StringComparer.OrdinalIgnoreCase);

                json = File.ReadAllText("Resources/statpsy.json");
                statpsy = new Dictionary<string, StatusPsynergy>(
                    JsonConvert.DeserializeObject<Dictionary<string, StatusPsynergy>>(json),
                    StringComparer.OrdinalIgnoreCase);

            } catch (Exception e)
            {
                //Just for debugging.
                Console.WriteLine(e.ToString());
            }

        }

        public static Psynergy GetPsynergy(string psynergy)
        {
            if (offpsy.TryGetValue(psynergy, out OffensivePsynergy op)) return (OffensivePsynergy) op.Clone();

            if (healpsy.TryGetValue(psynergy, out HealPsynergy hp)) return (HealPsynergy) hp.Clone();

            if (statpsy.TryGetValue(psynergy, out StatusPsynergy sp)) return (StatusPsynergy) sp.Clone();
           
            return new OffensivePsynergy($"{psynergy} (Not Implemented!)", "⛔", Target.otherSingle, 1, new List<EffectImage>(), Psynergy.Element.none, 0, 1 ,0, 1);
        }

        public static T Clone<T>(T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }
    }
}
