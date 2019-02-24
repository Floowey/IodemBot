using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Moveset
    {
        private static Dictionary<string, string[]> movepools;

        //get Moveset based on Classname
        public static Move[] getMoveset(string className)
        {
            if (!movepools.ContainsKey(className)) return getMoveset(new string[] { });
            return getMoveset(movepools[className]);
            
        }

        public static Move[] getMoveset(string[] moveNames)
        {
            List<Move> moves = new List<Move> { new Attack(), new Defend() };
            foreach (string s in moveNames)
            {
                Move m = PsynergyDatabase.GetPsynergy(s);
                moves.Add(m);
            }
            return moves.ToArray();
        }

        static Moveset() {
            string json = File.ReadAllText("Resources/movepools.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            movepools = data.ToObject<Dictionary<string, string[]>>();
        }

        public static void Save()
        {
            movepools = new Dictionary<string, string[]>();
            movepools.Add("Squire", new string[] { "Ragnarök", "Cure" });
            movepools.Add("Knight", new string[] { "Ragnarök", "Cure" });

            string data = JsonConvert.SerializeObject(movepools, Formatting.Indented);
            File.WriteAllText("Resources/movepools.json", data);
        }

    }
}
