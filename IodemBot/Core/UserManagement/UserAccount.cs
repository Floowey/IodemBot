using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;
using System;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Core.UserManagement
{
    public class UserAccount
    {
        public ulong ID { get; set; }
        public string Name { get; set; }
        public string Flag { get; set; }
        //Ranking

        public BattleStats BattleStats { get; set; } = new BattleStats();
        public ServerStats ServerStats { get; set; } = new ServerStats();

        internal void revived()
        {
            BattleStats.revives++;
        }

        public uint XP { get; set; } = 0;
        public DateTime lastXP { get; set; }
        public DateTime lastDayActive { get; set; }

        [JsonIgnore]
        public uint LevelNumber
        {
            get
            {
                int rate = 200;
                int cutoff = 125000;
                if (XP <= cutoff)
                {
                    return (uint)Math.Sqrt(XP / 50);
                }
                else
                {
                    return (uint)(50 - Math.Sqrt(cutoff / rate) + Math.Sqrt(XP / rate));
                }
            }
        }

        //Golden Sun
        public Element element { get; set; } = Element.none;

        public int classToggle { get; set; } = 0;

        internal void dealtDmg(uint damage)
        {
            BattleStats.damageDealt += damage;
        }

        internal void killedByHand()
        {
            BattleStats.killsByHand++;
        }

        public string[] BonusClasses = new string[] { };

        internal void healedHP(long HPtoHeal)
        {
            BattleStats.HPhealed += (uint)HPtoHeal;
        }

        [JsonIgnore]
        public string gsClass
        {
            get
            {
                return AdeptClassSeriesManager.getClass(this).name; //GoldenSun.getClass(element, LevelNumber, (uint) classToggle);
            }
        }

        public int uniqueDaysActive { get; set; } = 0;

        //Friend Codes
        public bool arePublicCodes = false;

        public string N3DSCode { get; set; } = "0000-0000-0000";
        public string SwitchCode { get; set; } = "0000-0000-0000";
        public string PoGoCode { get; set; } = "0000-0000-0000";
    }

    public class BattleStats
    {
        public uint damageDealt { get; set; } = 0;
        public uint killsByHand { get; set; } = 0;
        public uint HPhealed { get; set; } = 0;
        public uint revives { get; set; } = 0;
        public int totalTeamMates { get; set; } = 0;
        public int soloBattles { get; set; } = 0;
        public int supported { get; set; } = 0;
        public int kills { get; set; } = 0;
        public int defends { get; set; } = 0;
        public int attackedWeakness { get; set; } = 0;

        public static BattleStats operator +(BattleStats b1, BattleStats b2)
        {
            return new BattleStats()
            {
                damageDealt = b1.damageDealt + b2.damageDealt,
                killsByHand = b1.killsByHand + b2.killsByHand,
                HPhealed = b1.HPhealed + b2.HPhealed,
                revives = b1.revives + b2.revives,
                totalTeamMates = b1.totalTeamMates + b2.totalTeamMates,
                soloBattles = b1.soloBattles + b2.soloBattles,
                supported = b1.supported + b2.supported,
                kills = b1.kills + b2.kills,
                defends = b1.defends + b2.defends
            };
        }
    }

    public class ServerStats
    {
        public uint ColossoWins { get; set; } = 0;
        public uint ColossoStreak { get; set; } = 0;
        public uint ColossoHighestStreak { get; set; } = 0;
        public uint CommandsUsed { get; set; } = 0;
        public uint rpsWins { get; set; } = 0;
        public uint rpsStreak { get; set; } = 0;
        public ulong mostRecentChannel { get; set; } = 0;
        public ulong channelSwitches { get; set; } = 0;
        public bool hasWrittenCurse { get; set; } = false;
        public bool hasQuotedMatthew { get; set; } = false;
        public int uniqueDaysActive { get; set; } = 0;
        public int lookedUpInformation { get; set; } = 0;
        public DateTime lastDayActive { get; set; }
        public int lookedUpClass { get; set; }
    }
}