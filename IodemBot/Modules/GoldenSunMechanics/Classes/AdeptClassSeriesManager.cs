﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class AdeptClassSeriesManager
    {
        public static List<AdeptClassSeries> allClasses;
        private static readonly string filePath = "Resources/GoldenSun/AdeptClassSeries.json";

        static AdeptClassSeriesManager()
        {
            //saveSeries();
            string json = File.ReadAllText(filePath);
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            allClasses = data.ToObject<List<AdeptClassSeries>>();
        }

        internal static List<Move> GetMoveset(UserAccount avatar)
        {
            List<Move> moves = new List<Move> { new Attack(), new Defend() };

            string[] moveNames = GetClass(avatar).Movepool;

            foreach (string s in moveNames)
            {
                Move m = PsynergyDatabase.GetMove(s);
                moves.Add(m);
            }

            var classSeries = GetClassSeries(avatar);
            var gear = avatar.Inv.GetGear(classSeries.Archtype);
            if (gear.HasItem(ItemCategory.Weapon))
            {
                moves.Where(m => m is Attack).First().Emote = gear.GetItem(ItemCategory.Weapon).Icon;
            }
            if (gear.HasItem(ItemCategory.ArmWear))
            {
                moves.Where(m => m is Defend).First().Emote = gear.GetItem(ItemCategory.ArmWear).Icon;
            }
            return moves;
        }

        internal static Move[] GetMoveset(AdeptClass adeptClass)
        {
            List<Move> moves = new List<Move>();
            string[] moveNames = adeptClass.Movepool;

            foreach (string s in moveNames)
            {
                Move m = PsynergyDatabase.GetMove(s);
                moves.Add(m);
            }
            return moves.ToArray();
        }

        internal static ElementalStats GetElStats(UserAccount User)
        {
            var els = GetClassSeries(User).Elstats;
            switch (User.Element)
            {
                case Element.Venus:
                    els += new ElementalStats() { VenusAtk = 10, VenusRes = 15, MarsAtk = 5, MarsRes = 5, JupiterAtk = -10, JupiterRes = -15 }; break;
                case Element.Mars:
                    els += new ElementalStats() { VenusAtk = 5, VenusRes = 5, MarsAtk = 10, MarsRes = 15, MercuryAtk = -10, MercuryRes = -15 }; break;
                case Element.Jupiter:
                    els += new ElementalStats() { VenusAtk = -10, VenusRes = -15, JupiterAtk = 10, JupiterRes = 15, MercuryAtk = 5, MercuryRes = 5 }; break;
                case Element.Mercury:
                    els += new ElementalStats() { MarsAtk = -10, MarsRes = -15, JupiterAtk = 5, JupiterRes = 5, MercuryAtk = 10, MercuryRes = 15 }; break;
            }
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
            return availableClasses.ElementAt(position).Clone();
        }

        public static bool TryGetClassSeries(string series, out AdeptClassSeries outSeries)
        {
            if (series == "")
            {
                outSeries = null;
                return false;
            }
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