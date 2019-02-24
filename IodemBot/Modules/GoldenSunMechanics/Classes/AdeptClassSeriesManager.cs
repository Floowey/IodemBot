using IodemBot.Core.UserManagement;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class AdeptClassSeriesManager
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
            return moves.ToArray();
        }

        internal static ElementalStats getElStats(UserAccount User)
        {
            return getClassSeries(User).elstats;
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
    }
}
