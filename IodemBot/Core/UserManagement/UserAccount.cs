using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Core.UserManagement
{
    public class UserAccount
    {
        public ulong ID { get; set; }
        public string Name { get; set; }
        //Ranking

        public BattleStats BattleStats { get; set; }
            = new BattleStats();

        public ServerStats ServerStats { get; set; } = new ServerStats();

        private Inventory hiddenInv;

        public Inventory Inv
        {
            get
            {
                if (hiddenInv == null)
                { hiddenInv = new Inventory(); }
                else if (!hiddenInv.IsInitialized)
                { hiddenInv.Initialize(); }
                return hiddenInv;
            }
            set { hiddenInv = value; }
        }

        internal void Revived()
        {
            BattleStats.Revives++;
        }

        public ulong XP { get; set; } = 0;
        public DateTime LastXP { get; set; }

        [JsonIgnore]
        public uint LevelNumber
        {
            get
            {
                ulong rate0 = 50;

                ulong cutoff50 = 125000;
                ulong rate50 = 200;

                ulong cutoff80 = 605000;
                ulong rate80 = 1000;

                ulong cutoff90 = 1196934;
                ulong rate90 = 2500;

                ulong cutoff100 = 2540978;
                ulong rate100 = 10000;

                uint level = 1;

                if (XP <= cutoff50)
                {
                    level = (uint)Math.Sqrt(XP / rate0);
                }
                else if (XP <= cutoff80)
                {
                    level = (uint)(50 - Math.Sqrt(cutoff50 / rate50) + Math.Sqrt(XP / rate50));
                }
                else if (XP <= cutoff90)
                {
                    level = (uint)(80 - Math.Sqrt(cutoff80 / rate80) + Math.Sqrt(XP / rate80));
                }
                else if (XP <= cutoff100)
                {
                    level = (uint)(90 - Math.Sqrt(cutoff90 / rate90) + Math.Sqrt(XP / rate90));
                }
                else
                {
                    level = (uint)(100 - Math.Sqrt(cutoff100 / rate100) + Math.Sqrt(XP / rate100));
                }

                return level;
            }
        }

        //Golden Sun
        public Element Element { get; set; } = Element.none;

        public int ClassToggle { get; set; } = 0;

        internal void DealtDmg(uint damage)
        {
            BattleStats.DamageDealt += damage;
        }

        internal void KilledByHand()
        {
            BattleStats.KillsByHand++;
        }

        public string[] BonusClasses = new string[] { };
        public List<string> Dungeons = new List<string>() { };

        internal void HealedHP(long HPtoHeal)
        {
            BattleStats.HPhealed += (uint)HPtoHeal;
        }

        [JsonIgnore]
        public string GsClass
        {
            get
            {
                return AdeptClassSeriesManager.GetClass(this).Name; //GoldenSun.getClass(element, LevelNumber, (uint) classToggle);
            }
        }

        //Friend Codes
        public bool arePublicCodes = false;

        public DateTime LastClaimed { get; set; }

        public string N3DSCode { get; set; } = "0000-0000-0000";
        public string SwitchCode { get; set; } = "0000-0000-0000";
        public string PoGoCode { get; set; } = "0000-0000-0000";
    }

    public class BattleStats
    {
        public uint DamageDealt { get; set; } = 0;
        public uint KillsByHand { get; set; } = 0;
        public uint HPhealed { get; set; } = 0;
        public uint Revives { get; set; } = 0;
        public int TotalTeamMates { get; set; } = 0;
        public int SoloBattles { get; set; } = 0;
        public int Supported { get; set; } = 0;
        public int Kills { get; set; } = 0;
        public int Defends { get; set; } = 0;
        public int AttackedWeakness { get; set; } = 0;
        public uint DamageTanked { get; set; } = 0;
        public uint HighestDamage { get; set; } = 0;

        public static BattleStats operator +(BattleStats b1, BattleStats b2)
        {
            return new BattleStats()
            {
                DamageDealt = b1.DamageDealt + b2.DamageDealt,
                KillsByHand = b1.KillsByHand + b2.KillsByHand,
                HPhealed = b1.HPhealed + b2.HPhealed,
                Revives = b1.Revives + b2.Revives,
                TotalTeamMates = b1.TotalTeamMates + b2.TotalTeamMates,
                SoloBattles = b1.SoloBattles + b2.SoloBattles,
                Supported = b1.Supported + b2.Supported,
                Kills = b1.Kills + b2.Kills,
                Defends = b1.Defends + b2.Defends,
                AttackedWeakness = b1.AttackedWeakness + b2.AttackedWeakness,
                DamageTanked = b1.DamageTanked + b2.DamageTanked,
                HighestDamage = Math.Max(b1.HighestDamage, b2.HighestDamage)
            };
        }
    }

    public class ServerStats
    {
        public uint ColossoWins { get; set; } = 0;
        public uint ColossoStreak { get; set; } = 0;
        public uint ColossoHighestStreak { get; set; } = 0;
        public uint CommandsUsed { get; set; } = 0;
        public uint RpsWins { get; set; } = 0;
        public uint RpsStreak { get; set; } = 0;
        public ulong MostRecentChannel { get; set; } = 0;
        public ulong ChannelSwitches { get; set; } = 0;
        public bool HasWrittenCurse { get; set; } = false;
        public bool HasQuotedMatthew { get; set; } = false;
        public int UniqueDaysActive { get; set; } = 0;
        public int LookedUpInformation { get; set; } = 0;
        public DateTime LastDayActive { get; set; }
        public int LookedUpClass { get; set; }
        public int MessagesInColossoTalks { get; set; }
        public int ReactionsAdded { get; set; }
        public int ColossoHighestRoundEndless { get; set; } = 0;
        public int ColossoHighestRoundEndlessSolo { get; set; }
        public int ColossoHighestRoundEndlessDuo { get; set; } = 0;
        public int ColossoHighestRoundEndlessTrio { get; set; } = 0;
        public int ColossoHighestRoundEndlessQuad { get; set; } = 0;
        public string ColossoHighestRoundEndlessDuoNames { get; set; } = "";
        public string ColossoHighestRoundEndlessTrioNames { get; set; } = "";
        public string ColossoHighestRoundEndlessQuadNames { get; set; } = "";
        public uint SpentMoneyOnArtifacts { get; set; }
    }
}