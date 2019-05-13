using IodemBot.Core.UserManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules.GoldenSunMechanics
{
    //public enum Requirement { Level, Colosso, Command};
    internal class AdeptClassSeries
    {
        public static Dictionary<string, IRequirement> DictReq;
        public string Name { get; set; }
        public AdeptClass[] Classes { get; set; }
        public Element[] Elements { get; set; }
        public ElementalStats Elstats { get; set; }
        public string Requirement { get; set; }
        public bool IsDefault { get; set; }
        public string Description { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ArchType Archtype { get; set; }

        public AdeptClass GetClass(UserAccount User)
        {
            int classNr;
            try
            {
                classNr = Math.Min(Classes.Length - 1, DictReq[Requirement].apply(User));
            }
            catch
            {
                classNr = 0;
            }
            return Classes[classNr];
        }

        static AdeptClassSeries()
        {
            DictReq = new Dictionary<string, IRequirement>
            {
                { "Level", new LevelRequirement() },
                { "ColossoWins", new ColossoWinRequirement() },
                { "CommandUses", new CommandRequirement() },
                { "Damage", new DamageRequirement() },
                { "Heals", new HealRequirement() },
                { "Revives", new ReviveRequirement() },
                { "KillsByHand", new KillsByHandRequirement() },
                { "SoloBattles", new SoloBattleRequirement() },
                { "Teammates", new TeammatesRequirement() },
                { "DaysActive", new DaysActiveRequirement() },
                { "UnlockedClasses", new UnlockedClassesRequirement() }
            };
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