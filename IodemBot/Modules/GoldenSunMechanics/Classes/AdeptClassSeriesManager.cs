using IodemBot.Core.UserManagement;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class AdeptClassSeriesManager
    {
        private static List<AdeptClassSeries> allClasses;
        private static readonly string filePath = "Resources/AdeptClassSeries.json";

        static AdeptClassSeriesManager()
        {
            //saveSeries();
            string json = File.ReadAllText(filePath);
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            allClasses = data.ToObject<List<AdeptClassSeries>>();
        }

        internal static Move[] GetMoveset(UserAccount avatar)
        {
            List<Move> moves = new List<Move> { new Attack(), new Defend() };

            string[] moveNames = GetClass(avatar).Movepool;

            foreach (string s in moveNames)
            {
                Move m = PsynergyDatabase.GetPsynergy(s);
                moves.Add(m);
            }

            var classSeries = AdeptClassSeriesManager.GetClassSeries(avatar);
            var gear = avatar.Inv.GetGear(classSeries.Archtype);
            gear.ForEach(g =>
            {
                if (g.IsWeapon)
                {
                    moves.Where(m => m is Attack).First().emote = g.IconDisplay;
                }

                if (g.IsArmWear)
                {
                    moves.Where(m => m is Defend).First().emote = g.IconDisplay;
                }
            });
            return moves.ToArray();
        }

        internal static Move[] GetMoveset(AdeptClass adeptClass)
        {
            List<Move> moves = new List<Move>();
            string[] moveNames = adeptClass.Movepool;

            foreach (string s in moveNames)
            {
                Move m = PsynergyDatabase.GetPsynergy(s);
                moves.Add(m);
            }
            return moves.ToArray();
        }

        internal static ElementalStats GetElStats(UserAccount User)
        {
            var classSeries = AdeptClassSeriesManager.GetClassSeries(User);
            var els = GetClassSeries(User).Elstats;

            var gear = User.Inv.GetGear(classSeries.Archtype);
            gear.ForEach(g =>
            {
                els += g.AddElStatsOnEquip;
            });
            return els;
        }

        public static AdeptClass GetClass(UserAccount User)
        {
            return GetClassSeries(User).GetClass(User);
        }

        public static AdeptClassSeries GetClassSeries(UserAccount User)
        {
            List<AdeptClassSeries> availableClasses = allClasses.Where(c => c.IsDefault && c.Elements.Contains(User.Element)).ToList();
            availableClasses.AddRange(allClasses.Where(c => User.BonusClasses.Contains(c.Name) && c.Elements.Contains(User.Element)).ToList());
            var position = User.ClassToggle % availableClasses.Count;
            return availableClasses.ElementAt(position);
        }

        public static bool TryGetClassSeries(string series, out AdeptClassSeries outSeries)
        {
            var trySeries = allClasses.Where(s => s.Name.ToUpper().Contains(series.ToUpper()) || s.Classes.Any(c => c.Name.ToUpper().Contains(series.ToUpper())));
            if (trySeries.FirstOrDefault() == null)
            {
                outSeries = null;
                return false;
            }
            else
            {
                outSeries = trySeries.Where(s => s.Classes.Any(c => c.Name.ToUpper() == series.ToUpper())).FirstOrDefault() ?? trySeries.FirstOrDefault();
            }

            return true;
        }
    }
}