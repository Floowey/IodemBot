using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class PsynergyDatabase
    {
        private static readonly Dictionary<string, OffensivePsynergy> offpsy = new Dictionary<string, OffensivePsynergy>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, HealPsynergy> healpsy = new Dictionary<string, HealPsynergy>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, StatusPsynergy> statpsy = new Dictionary<string, StatusPsynergy>(StringComparer.OrdinalIgnoreCase);

        static PsynergyDatabase()
        {
            try
            {
                string json = File.ReadAllText("Resources/GoldenSun/Moves/offpsy.json");
                offpsy = new Dictionary<string, OffensivePsynergy>(
                    JsonConvert.DeserializeObject<Dictionary<string, OffensivePsynergy>>(json),
                    StringComparer.OrdinalIgnoreCase);

                json = File.ReadAllText("Resources/GoldenSun/Moves/healpsy.json");
                healpsy = new Dictionary<string, HealPsynergy>(
                    JsonConvert.DeserializeObject<Dictionary<string, HealPsynergy>>(json),
                    StringComparer.OrdinalIgnoreCase);

                json = File.ReadAllText("Resources/GoldenSun/Moves/statpsy.json");
                statpsy = new Dictionary<string, StatusPsynergy>(
                    JsonConvert.DeserializeObject<Dictionary<string, StatusPsynergy>>(json),
                    StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception e)
            {
                //Just for debugging.
                Console.WriteLine(e.ToString());
            }
        }

        public static Move GetMove(string move)
        {
            if (offpsy.TryGetValue(move, out OffensivePsynergy op))
            {
                return (OffensivePsynergy)op.Clone();
            }

            if (healpsy.TryGetValue(move, out HealPsynergy hp))
            {
                return (HealPsynergy)hp.Clone();
            }

            if (statpsy.TryGetValue(move, out StatusPsynergy sp))
            {
                return (StatusPsynergy)sp.Clone();
            }

            if (DjinnAndSummonsDatabase.TryGetDjinn(move, out Djinn d))
            {
                return d;
            }

            if (DjinnAndSummonsDatabase.TryGetSummon(move, out Summon s))
            {
                return s;
            }

            //Console.WriteLine($"{psynergy} is not implemented.");
            return new StatusPsynergy()
            {
                Name = $"{move} (NOT IMPLEMENTED)",
                Effects = new List<Effect>() { new NoEffect() }
            };
        }

        public static bool TryGetMove(string move, out Move psy)
        {
            psy = GetMove(move);
            if (psy.Name.ToLower().Contains("not implemented"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static IEnumerable<Move> GetMove(string[] movesString)
        {
            List<Move> moves = new List<Move>();
            if (movesString == null)
            {
                return moves;
            }

            if (movesString.Length > 0)
            {
                foreach (var s in movesString)
                {
                    moves.Add(GetMove(s));
                }
            }
            return moves.ToArray();
        }

        public static List<Move> GetMovepool(string[] psynergiesString, bool hasAttack, bool hasDefend)
        {
            List<Move> moves = new List<Move>();
            if (hasAttack)
            {
                moves.Add(new Attack());
                moves.Add(new Attack());
            }

            if (hasDefend)
            {
                moves.Add(new Defend());
            }

            moves.AddRange(GetMove(psynergiesString));

            return moves;
        }

        public static T Clone<T>(T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }
    }
}