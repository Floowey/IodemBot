using System;
using System.Collections.Generic;
using IodemBot.Core.UserManagement;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
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

        public ArchType Archtype { get; set; }

        public AdeptClass GetClass(UserAccount User)
        {
            int classNr;
            try
            {
                classNr = Math.Min(Classes.Length - 1, DictReq[Requirement].Apply(User));
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
                { "UnlockedClasses", new UnlockedClassesRequirement() },
                { "Djinn", new DjinnRequirement() },
                { "DungeonsCompleted", new DungeonsCompletedRequirement()},
                { "VenusDjinn", new VenusDjinnRequirement()},
                { "CursedItems", new CursedItemsRequirement()},
                { "MarsDjinn", new MarsDjinnRequirement()},
                { "JupiterDjinn", new JupiterDjinnRequirement()},
                { "MercuryDjinn", new MercuryDjinnRequirement()}
            };
        }

        internal AdeptClassSeries Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<AdeptClassSeries>(serialized);
        }

        // Mono elementals:  0,2,4,6,8 Djinn of the corresponding element

        // Apprentice:       21 i!psy lookups | Unlocked (0, 4, 8, 12, 16) Class Series
        // Brute:            20 Colosso Wins | (0, 50, 250, 500, 800+20, 1200+27) Wins + Endless Solo Streak
        // Crusader:         Find 6 Dungeons + Keys | Complete (0, 25, 66, 123, 200) Dungeons
        // Curse Mage:       i!quote Matthew OR #^@%! | Wear cursed gear; Number of Unique Cursed items in inv / 2 + cursed gear worn
        // Hermit:           30 Days since joining the Server OR finish same dungeon 5 times in a row | Levels (0, 12, 24, 36, 48, 60)
        // Page:             11 i!classinfo lookups | See Apprentice
        // Pilgrim (Jup):    14 "Switchpoints" between Channels (+2 for switch, -1 for staying) OR Complete 12 dungeons | See Crusader
        // Pilgrim (Mer):    Finish Mercury LH | See Crusader
        // Scrapper:         Use 100 Bot Commands | same as Brute but ends one earlier
        // Seer (Jup):       use i!seer, get the SEER (not the fortune teller) to mention "spirits" | (0,1,3,5,7) as lowest number of djinn of elements
        // Seer (Mer):       3 RPS Wins, overall | Same as Air Seer
        // Swordsman:        50 Messages in ColossoTalks OR Win 1 PvP match | same as Brute but ends one earlier

        // Dragoon: (100, 200, 400) People in Battle  (+0 for Solo, +3 for full party)
        // Samurai: (161, 666) Kills By UNLEASH or Melee Psynergy (i.e. Ragnarok). "Next" and "End" enemies don't count.
        // Medium: (25, 66, 111) Revives
        // Ranger: Solo Battles (100, 300, 500)
        // Ninja: Total Damage (222 222, 888 888, 1 111 111)
        // White Mage: Points Healed (222 222, 555 555)
    }
}