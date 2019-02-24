using IodemBot.Core.UserManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules.GoldenSunMechanics
{
    //public enum Requirement { Level, Colosso, Command};
    class AdeptClassSeries
    {
        public static Dictionary<string, IRequirement> DictReq;
        public string name { get; set; }
        public AdeptClass[] classes { get; set; }
        public Element[] elements { get; set; }
        public ElementalStats elstats { get; set; }
        public string requirement { get; set; }
        public bool isDefault { get; set; }

        public AdeptClass getClass(UserAccount User)
        {
            int classNr;
            try
            {
                classNr = Math.Min(classes.Length-1, DictReq[requirement].apply(User));
            } catch
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
        }

        //Apprentice:       
        //Brute:            15 Colosso Wins
        //Crusader:         
        //Curse Mage:       i!quote Matthew + #^@%!
        //Hermit:           Time on Server (increases with active days)
        //Page:             
        //Pilgrim (Jup):    10 "Switchpoints) between Channels (+2 for switch, -1 for staying)
        //Pilgrim (Mer):    
        //Scrapper:         Bot Commands
        //Seer (Jup):       Winning rps
        //Seer (Mer):       rps streak
        //Swordsman:

        //Dragoon: People in Battle (100, 250, 400) (+0 for Solo, +3 for full party)
        //Samurai: Kills By Hand (161, 666)
        //Medium: Revives (50, 120, 200)
        //Ranger: Solo Battles (50, 200, 450
        //Ninja: Total Damage (666 666, 999 999, 3 333 333)
        //White Mage: Points Healed (333 333, 999 999)
    }
}
