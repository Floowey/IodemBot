using IodemBot.Core.UserManagement;
using System;
using System.Collections.Generic;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules.GoldenSunMechanics
{
    //public enum Requirement { Level, Colosso, Command};
    internal class AdeptClassSeries
    {
        public static Dictionary<string, IRequirement> DictReq;
        public string name { get; set; }
        public AdeptClass[] classes { get; set; }
        public Element[] elements { get; set; }
        public ElementalStats elstats { get; set; }
        public string requirement { get; set; }
        public bool isDefault { get; set; }
        public string description { get; set; }

        public AdeptClass getClass(UserAccount User)
        {
            int classNr;
            try
            {
                classNr = Math.Min(classes.Length - 1, DictReq[requirement].apply(User));
            }
            catch
            {
                classNr = 0;
            }
            return classes[classNr];
        }

        static AdeptClassSeries()
        {
            DictReq = new Dictionary<string, IRequirement>();
            DictReq.Add("Level", new LevelRequirement());
            DictReq.Add("ColossoWins", new ColossoWinRequirement());
            DictReq.Add("CommandUses", new CommandRequirement());
            DictReq.Add("Damage", new DamageRequirement());
            DictReq.Add("Heals", new HealRequirement());
            DictReq.Add("Revives", new ReviveRequirement());
            DictReq.Add("KillsByHand", new KillsByHandRequirement());
            DictReq.Add("SoloBattles", new SoloBattleRequirement());
            DictReq.Add("Teammates", new TeammatesRequirement());
            DictReq.Add("DaysActive", new DaysActiveRequirement());
            DictReq.Add("UnlockedClasses", new UnlockedClassesRequirement());
        }

        // Apprentice:       21 i!psy lookups | Level
        // Brute:            15 Colosso Wins | (0, 50, 150, 300, 500+10, 800+25 Wins + Streak)
        // Crusader:         - | same as Brute
        // Curse Mage:       i!quote Matthew + #^@%! | same as Brute
        // Hermit:           30 Days since joining the Server | (0, 7, 14, 30, 45, 70 *active* Days)
        // Page:             21 i!classinfo lookups | (0, 3, 6, 9, 14) Unlocked Classes
        // Pilgrim (Jup):    14 "Switchpoints" between Channels (+2 for switch, -1 for staying) | Level
        // Pilgrim (Mer):    - | Level
        // Scrapper:         Bot Commands | same as Brute
        // Seer (Jup):       15 RPS Wins | Level
        // Seer (Mer):       4 RPS Wins in a row | Level
        // Swordsman:        50 Messages in ColossoTalks | same as Brute

        // Dragoon: People in Battle (100, 250, 450) (+0 for Solo, +3 for full party)
        // Samurai: Kills By Hand (161, 666)
        // Medium: Revives (50, 120, 200)
        // Ranger: Solo Battles (50, 200, 400)
        // Ninja: Total Damage (666 666, 999 999, 3 333 333)
        // White Mage: Points Healed (333 333, 999 999)
    }
}