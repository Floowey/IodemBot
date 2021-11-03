using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class PsynergyDatabase
    {
        private static readonly Dictionary<string, OffensivePsynergy> Offpsy = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, HealPsynergy> Healpsy = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, StatusPsynergy> Statpsy = new(StringComparer.OrdinalIgnoreCase);

        static PsynergyDatabase()
        {
            try
            {
                var json = File.ReadAllText("Resources/GoldenSun/Moves/offpsy.json");
                Offpsy = new Dictionary<string, OffensivePsynergy>(
                    JsonConvert.DeserializeObject<Dictionary<string, OffensivePsynergy>>(json),
                    StringComparer.OrdinalIgnoreCase);

                json = File.ReadAllText("Resources/GoldenSun/Moves/healpsy.json");
                Healpsy = new Dictionary<string, HealPsynergy>(
                    JsonConvert.DeserializeObject<Dictionary<string, HealPsynergy>>(json),
                    StringComparer.OrdinalIgnoreCase);

                json = File.ReadAllText("Resources/GoldenSun/Moves/statpsy.json");
                Statpsy = new Dictionary<string, StatusPsynergy>(
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
            if (Offpsy.TryGetValue(move, out var op)) return (OffensivePsynergy)op.Clone();

            if (Healpsy.TryGetValue(move, out var hp)) return (HealPsynergy)hp.Clone();

            if (Statpsy.TryGetValue(move, out var sp)) return (StatusPsynergy)sp.Clone();

            if (DjinnAndSummonsDatabase.TryGetDjinn(move, out var d)) return d;

            if (DjinnAndSummonsDatabase.TryGetSummon(move, out var s)) return s;

            //Console.WriteLine($"{psynergy} is not implemented.");
            return new StatusPsynergy
            {
                Name = $"{move} (NOT IMPLEMENTED)",
                Effects = new List<Effect> { new NoEffect() }
            };
        }

        public static bool TryGetMove(string move, out Move psy)
        {
            psy = GetMove(move);
            if (psy.Name.ToLower().Contains("not implemented"))
                return false;
            return true;
        }

        public static IEnumerable<Move> GetMove(string[] movesString)
        {
            var moves = new List<Move>();
            if (movesString == null) return moves;

            if (movesString.Length > 0)
                foreach (var s in movesString)
                {
                    moves.Add(GetMove(s));
                }

            return moves.ToArray();
        }

        public static List<Move> GetMovepool(string[] psynergiesString, bool hasAttack, bool hasDefend)
        {
            var moves = new List<Move>();
            if (hasAttack)
            {
                moves.Add(new Attack());
                moves.Add(new Attack());
            }

            if (hasDefend) moves.Add(new Defend());

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