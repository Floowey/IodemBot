using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Core.UserManagement
{
    public class UserAccount
    {
        public ulong ID { get; set; }
        public string Name { get; set; }
        //Ranking
        public uint ColossoWins { get; set; } = 0;
        public uint ColossoStreak { get; set; } = 0;
        public uint ColossoHighestStreak { get; set; } = 0;

        public uint emotesAdded { get; set; } = 0;
        public uint commandsUsed { get; set; } = 0;

        public uint rpsWins { get; set; } = 0;
        public uint rpsStreak { get; set; } = 0;
        public ulong mostRecentChannel { get; set; } = 0;
        public ulong channelSwitches { get; set; } = 0;
        public bool hasWrittenCurse { get; set; } = false;
        public bool hasQuotedMatthew { get; set; } = false;

        public uint damageDealt { get; set; } = 0;
        public uint killsByHand { get; set; } = 0;
        public uint HPhealed { get; set; } = 0;
        public uint revives { get; set; } = 0;
        public uint curStreak { get; set; } = 0;
        public uint highestStreak { get; set; } = 0;
        public int totalTeamMates { get; set; } = 0;
        public int soloBattles { get; set; } = 0;

        internal void revived()
        {
            revives++;
        }

        public uint XP { get; set; } = 0;
        public DateTime lastXP { get; set; }
        public DateTime lastDayActive { get; set; }
        [JsonIgnore] public uint LevelNumber
        {
            get
            {
                int rate = 200;
                int cutoff = 125000;
                if (XP <= cutoff)
                {
                    return (uint)Math.Sqrt(XP / 50);
                } else
                {
                    return (uint) (50 - Math.Sqrt(cutoff / rate) + Math.Sqrt(XP / rate));
                }
            }
        }

        //Golden Sun
        public Element element = Element.none;
        public int classToggle;

        internal void dealtDmg(uint damage)
        {
            damageDealt += damage;
        }

        internal void killedByHand()
        {
            killsByHand++;
        }

        public string[] BonusClasses = new string[] { };

        internal void healedHP(long HPtoHeal)
        {
            HPhealed += (uint) HPtoHeal;
        }

        [JsonIgnore] public string gsClass
        {
            get
            {
                return AdeptClassSeriesManager.getClass(this).name; //GoldenSun.getClass(element, LevelNumber, (uint) classToggle);
            }
        }

        public int uniqueDaysActive { get; set; }

        //Friend Codes
        public bool arePublicCodes = false;
        public string N3DSCode { get; set; } = "0000-0000-0000";
        public string SwitchCode { get; set; } = "0000-0000-0000";
        public string PoGoCode { get; set; } = "0000-0000-0000";
    }
        //etc 
}
