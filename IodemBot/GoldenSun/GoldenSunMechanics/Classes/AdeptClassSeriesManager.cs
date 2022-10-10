using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class AdeptClassSeriesManager
    {
        public static List<AdeptClassSeries> AllClasses;
        private static readonly string FilePath = "Resources/GoldenSun/AdeptClassSeries.json";

        static AdeptClassSeriesManager()
        {
            //saveSeries();
            var json = File.ReadAllText(FilePath);
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            AllClasses = data.ToObject<List<AdeptClassSeries>>();
        }

        internal static List<Move> GetMoveset(UserAccount avatar)
        {
            var moves = new List<Move> { new Attack(), new Defend() };

            var moveNames = GetClass(avatar).Movepool;

            foreach (var s in moveNames)
            {
                var m = PsynergyDatabase.GetMove(s);
                moves.Add(m);
            }

            var classSeries = GetClassSeries(avatar);
            var gear = avatar.Inv.GetGear(classSeries.Archtype);
            if (gear.HasItem(ItemCategory.Weapon))
                moves.First(m => m is Attack).Emote = gear.GetItem(ItemCategory.Weapon).Icon;
            if (gear.HasItem(ItemCategory.ArmWear))
                moves.First(m => m is Defend).Emote = gear.GetItem(ItemCategory.ArmWear).Icon;
            return moves;
        }

        internal static Move[] GetMoveset(AdeptClass adeptClass)
        {
            var moves = new List<Move>();
            var moveNames = adeptClass.Movepool;

            foreach (var s in moveNames)
            {
                var m = PsynergyDatabase.GetMove(s);
                moves.Add(m);
            }

            return moves.ToArray();
        }

        internal static ElementalStats GetElStats(UserAccount user)
        {
            var els = GetClassSeries(user).Elstats;
            switch (user.Element)
            {
                case Element.Venus:
                    els += new ElementalStats
                    { VenusAtk = 10, VenusRes = 15, MarsAtk = 5, MarsRes = 5, JupiterAtk = -10, JupiterRes = -15 };
                    break;

                case Element.Mars:
                    els += new ElementalStats
                    { VenusAtk = 5, VenusRes = 5, MarsAtk = 10, MarsRes = 15, MercuryAtk = -10, MercuryRes = -15 };
                    break;

                case Element.Jupiter:
                    els += new ElementalStats
                    {
                        VenusAtk = -10,
                        VenusRes = -15,
                        JupiterAtk = 10,
                        JupiterRes = 15,
                        MercuryAtk = 5,
                        MercuryRes = 5
                    };
                    break;

                case Element.Mercury:
                    els += new ElementalStats
                    {
                        MarsAtk = -10,
                        MarsRes = -15,
                        JupiterAtk = 5,
                        JupiterRes = 5,
                        MercuryAtk = 10,
                        MercuryRes = 15
                    };
                    break;
            }

            return els;
        }

        public static AdeptClass GetClass(UserAccount user)
        {
            return GetClassSeries(user).GetClass(user);
        }

        public static AdeptClassSeries GetClassSeries(UserAccount user)
        {
            var availableClasses = GetAvailableClasses(user);
            var position = user.ClassToggle % availableClasses.Count;
            return availableClasses.ElementAt(position).Clone();
        }

        public static List<AdeptClassSeries> GetAvailableClasses(UserAccount user)
        {
            var availableClasses = AllClasses.Where(c => c.IsDefault && c.Elements.Contains(user.Element)).ToList();

            availableClasses.AddRange(AllClasses
                .Where(c => user.BonusClasses.Contains(c.Name) && c.Elements.Contains(user.Element)).ToList());

            if (user.Oaths.IsOathActive(Oath.Warrior))
                availableClasses = availableClasses.Where(c => c.Archtype == ArchType.Warrior).ToList();
            if (user.Oaths.IsOathActive(Oath.Mage))
                availableClasses = availableClasses.Where(c => c.Archtype == ArchType.Mage).ToList();
            return availableClasses;
        }

        public static bool SetClass(UserAccount account, string targetClass = "")
        {
            var curClass = GetClassSeries(account).Name;
            account.ClassToggle++;
            if (!targetClass.IsNullOrEmpty())
            {
                account.ClassToggle++;
                while (GetClassSeries(account).Name != curClass)
                {
                    if (GetClassSeries(account).Name
                        .Contains(targetClass.ToLower(), StringComparison.InvariantCultureIgnoreCase)) break;

                    account.ClassToggle++;
                }
            }

            return !curClass.Equals(GetClassSeries(account).Name);
        }

        public static bool TryGetClassSeries(string series, out AdeptClassSeries outSeries)
        {
            if (series == "")
            {
                outSeries = null;
                return false;
            }

            var trySeries = AllClasses.Where(s =>
                s.Name.ToUpper().Contains(series.ToUpper()) ||
                s.Classes.Any(c => c.Name.ToUpper().Contains(series.ToUpper())));
            if (trySeries.FirstOrDefault() == null)
            {
                outSeries = null;
                return false;
            }

            outSeries =
                trySeries.FirstOrDefault(s => s.Classes.Any(c => c.Name.Equals(series, StringComparison.CurrentCultureIgnoreCase))) ??
                trySeries.FirstOrDefault();

            return true;
        }
    }
}