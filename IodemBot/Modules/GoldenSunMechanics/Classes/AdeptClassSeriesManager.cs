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
        private static string filePath = "Resources/AdeptClassSeries.json";

        static AdeptClassSeriesManager()
        {
            //saveSeries();
            string json = File.ReadAllText(filePath);
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            allClasses = data.ToObject<List<AdeptClassSeries>>();
        }

        internal static Move[] getMoveset(UserAccount avatar)
        {
            List<Move> moves = new List<Move> { new Attack(), new Defend() };

            string[] moveNames = getClass(avatar).movepool;

            foreach (string s in moveNames)
            {
                Move m = PsynergyDatabase.GetPsynergy(s);
                moves.Add(m);
            }

            var classSeries = AdeptClassSeriesManager.getClassSeries(avatar);
            var gear = avatar.inv.GetGear(classSeries.archtype);
            gear.ForEach(g =>
            {
                if (g.IsWeapon)
                {
                    moves.Where(m => m is Attack).First().emote = g.Icon;
                }

                if (g.IsArmWear)
                {
                    moves.Where(m => m is Defend).First().emote = g.Icon;
                }
            });
            return moves.ToArray();
        }

        internal static Move[] getMoveset(AdeptClass adeptClass)
        {
            List<Move> moves = new List<Move>();
            string[] moveNames = adeptClass.movepool;

            foreach (string s in moveNames)
            {
                Move m = PsynergyDatabase.GetPsynergy(s);
                moves.Add(m);
            }
            return moves.ToArray();
        }

        internal static ElementalStats getElStats(UserAccount User)
        {
            var classSeries = AdeptClassSeriesManager.getClassSeries(User);
            var els = getClassSeries(User).elstats;

            var gear = User.inv.GetGear(classSeries.archtype);
            gear.ForEach(g =>
            {
                els += g.AddElStatsOnEquip;
            });
            return els;
        }

        public static AdeptClass getClass(UserAccount User)
        {
            return getClassSeries(User).getClass(User);
        }

        public static AdeptClassSeries getClassSeries(UserAccount User)
        {
            List<AdeptClassSeries> availableClasses = allClasses.Where(c => c.isDefault && c.elements.Contains(User.element)).ToList();
            availableClasses.AddRange(allClasses.Where(c => User.BonusClasses.Contains(c.name) && c.elements.Contains(User.element)).ToList());
            var position = User.classToggle % availableClasses.Count;
            return availableClasses.ElementAt(position);
        }

        public static bool TryGetClassSeries(string series, out AdeptClassSeries outSeries)
        {
            var trySeries = allClasses.Where(s => s.name.ToUpper().Contains(series.ToUpper()) || s.classes.Any(c => c.name.ToUpper().Contains(series.ToUpper())));
            if (trySeries.FirstOrDefault() == null)
            {
                outSeries = null;
                return false;
            }
            else
            {
                outSeries = trySeries.Where(s => s.classes.Any(c => c.name.ToUpper() == series.ToUpper())).FirstOrDefault() ?? trySeries.FirstOrDefault();
            }

            return true;
        }
    }
}