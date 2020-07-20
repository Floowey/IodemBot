using System;
using System.Collections.Generic;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;

namespace IodemBot.Core.UserManagement
{
    public class BattleStats
    {
        public int AttackedWeakness { get; set; } = 0;
        public uint DamageDealt { get; set; } = 0;
        public uint DamageTanked { get; set; } = 0;
        public int Defends { get; set; } = 0;
        public uint HighestDamage { get; set; } = 0;
        public uint HPhealed { get; set; } = 0;
        public int Kills { get; set; } = 0;
        public uint KillsByHand { get; set; } = 0;
        public uint Revives { get; set; } = 0;
        public int SoloBattles { get; set; } = 0;
        public int Supported { get; set; } = 0;
        public int TotalTeamMates { get; set; } = 0;

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
        public ulong ChannelSwitches { get; set; } = 0;
        public int ColossoHighestRoundEndless { get; set; } = 0;
        public int ColossoHighestRoundEndlessDuo { get; set; } = 0;
        public string ColossoHighestRoundEndlessDuoNames { get; set; } = "";
        public int ColossoHighestRoundEndlessQuad { get; set; } = 0;
        public string ColossoHighestRoundEndlessQuadNames { get; set; } = "";
        public int ColossoHighestRoundEndlessSolo { get; set; }
        public int ColossoHighestRoundEndlessTrio { get; set; } = 0;
        public string ColossoHighestRoundEndlessTrioNames { get; set; } = "";
        public uint ColossoHighestStreak { get; set; } = 0;
        public uint ColossoStreak { get; set; } = 0;
        public uint ColossoWins { get; set; } = 0;
        public uint CommandsUsed { get; set; } = 0;
        public bool HasQuotedMatthew { get; set; } = false;
        public bool HasWrittenCurse { get; set; } = false;
        public DateTime LastDayActive { get; set; }
        public int LookedUpClass { get; set; }
        public int LookedUpInformation { get; set; } = 0;
        public int MessagesInColossoTalks { get; set; }
        public ulong MostRecentChannel { get; set; } = 0;
        public int ReactionsAdded { get; set; }
        public uint RpsStreak { get; set; } = 0;
        public uint RpsWins { get; set; } = 0;
        public uint SpentMoneyOnArtifacts { get; set; }
        public int UniqueDaysActive { get; set; } = 0;
        public int DungeonsCompleted { get; internal set; } = 0;
        public string LastDungeon { get; internal set; } = "";
        public int SameDungeonInARow { get; internal set; } = 0;

        public static ServerStats operator +(ServerStats s1, ServerStats s2)
        {
            return new ServerStats()
            {
                ColossoHighestRoundEndless = Math.Max(s1.ColossoHighestRoundEndless, s2.ColossoHighestRoundEndless),
                ColossoHighestRoundEndlessSolo = Math.Max(s1.ColossoHighestRoundEndlessSolo, s2.ColossoHighestRoundEndlessSolo),
                ColossoHighestRoundEndlessDuo = Math.Max(s1.ColossoHighestRoundEndlessDuo, s2.ColossoHighestRoundEndlessDuo),
                ColossoHighestRoundEndlessTrio = Math.Max(s1.ColossoHighestRoundEndlessTrio, s2.ColossoHighestRoundEndlessTrio),
                ColossoHighestRoundEndlessQuad = Math.Max(s1.ColossoHighestRoundEndlessQuad, s2.ColossoHighestRoundEndlessQuad),
                ColossoHighestRoundEndlessDuoNames = s1.ColossoHighestRoundEndlessDuo >= s2.ColossoHighestRoundEndlessDuo ? s1.ColossoHighestRoundEndlessDuoNames : s2.ColossoHighestRoundEndlessDuoNames,
                ColossoHighestRoundEndlessTrioNames = s1.ColossoHighestRoundEndlessTrio >= s2.ColossoHighestRoundEndlessTrio ? s1.ColossoHighestRoundEndlessTrioNames : s2.ColossoHighestRoundEndlessTrioNames,
                ColossoHighestRoundEndlessQuadNames = s1.ColossoHighestRoundEndlessDuo >= s2.ColossoHighestRoundEndlessQuad ? s1.ColossoHighestRoundEndlessQuadNames : s2.ColossoHighestRoundEndlessQuadNames,
                ColossoHighestStreak = Math.Max(s1.ColossoHighestStreak, s2.ColossoHighestStreak),

                ChannelSwitches = s1.ChannelSwitches + s2.ChannelSwitches,
                RpsWins = s1.RpsWins + s2.RpsWins,
                UniqueDaysActive = s1.UniqueDaysActive + s2.UniqueDaysActive,
                LookedUpInformation = s1.LookedUpInformation + s2.LookedUpInformation,
                LookedUpClass = s1.LookedUpClass + s2.LookedUpClass,
                ColossoWins = s1.ColossoWins + s2.ColossoWins,
                CommandsUsed = s1.CommandsUsed + s2.CommandsUsed,
                SpentMoneyOnArtifacts = s1.SpentMoneyOnArtifacts + s2.SpentMoneyOnArtifacts,
                ReactionsAdded = s1.ReactionsAdded + s2.ReactionsAdded,
                MessagesInColossoTalks = s1.MessagesInColossoTalks + s2.MessagesInColossoTalks,
                DungeonsCompleted = s1.DungeonsCompleted + s2.DungeonsCompleted
            };
        }
    }


    public class UserAccount
    {
        public ulong ID { get; set; }
        public string Name { get; set; }
        public bool arePublicCodes = false;

        public List<string> BonusClasses = new List<string> { };
        public List<string> Dungeons = new List<string>() { };
        public BattleStats BattleStats { get; set; } = new BattleStats();

        public BattleStats BattleStatsTotal { get; set; } = new BattleStats();

        public int ClassToggle { get; set; } = 0;
        public DjinnPocket DjinnPocket { get; set; } = new DjinnPocket();

        public Element Element { get; set; } = Element.none;
        public Loadouts loadouts { get; set; } = new Loadouts();

        [JsonIgnore]
        public string GsClass
        {
            get
            {
                return AdeptClassSeriesManager.GetClass(this).Name; //GoldenSun.getClass(element, LevelNumber, (uint) classToggle);
            }
        }

        public Inventory Inv { get; set; } = new Inventory();
        public DateTime LastClaimed { get; set; }
        public DateTime LastXP { get; set; }

        [JsonIgnore]
        private ulong[][] rates = new ulong[][] {
                    new ulong[] { 2538160, 25000, 100 },
                    new ulong[] { 1196934, 2500, 90 },
                    new ulong[] { 605000, 1000, 80 },
                    new ulong[]{ 125000, 200, 50 },
                    new ulong[]{ 0, 50, 0 }
            };

        [JsonIgnore]
        public uint LevelNumber
        {
            get
            {
                uint curLevel = 1;
                foreach (var r in rates)
                {
                    var cutoff = r[0];
                    var rate = (double)r[1];
                    var level = r[2];
                    if (XP >= cutoff)
                    {
                        curLevel = (uint)(level - Math.Sqrt(cutoff / rate) + Math.Sqrt(XP / rate));
                        break;
                    }
                }

                return Math.Max(1, curLevel);
            }
        }
        [JsonIgnore]
        public ulong XPneeded
        {
            get
            {
                var xpneeded = XP;
                foreach (var r in rates)
                {
                    var cutoff = r[0];
                    var rate = (double)r[1];
                    var level = r[2];
                    if (XP >= cutoff)
                    {
                        var wantedLvl = LevelNumber + 1;
                        var rateFactor = Math.Sqrt(cutoff / rate);
                        xpneeded = (ulong)(Math.Pow(wantedLvl - level + rateFactor, 2) * rate);
                        break;
                    }
                }

                return xpneeded - XP;
            }
        }

        public string N3DSCode { get; set; } = "0000-0000-0000";
        public int NewGames { get; set; } = 0;
        public string PoGoCode { get; set; } = "0000-0000-0000";
        public ServerStats ServerStats { get; set; } = new ServerStats();
        public ServerStats ServerStatsTotal { get; set; } = new ServerStats();
        public string SwitchCode { get; set; } = "0000-0000-0000";

        public List<string> Tags { get; set; } = new List<string>();

        public ulong TotalXP { get { return XPLastGame + XP; } }

        public ulong XP { get; set; } = 0;

        public double XpBoost { get; set; } = 1;

        public ulong XPLastGame { get; set; } = 0;

        public void AddXp(ulong xp)
        {
            XP += (ulong)(xp * XpBoost);
        }

        public void NewGame()
        {
            XpBoost *= 1 + 0.1 * (1 - Math.Exp(-(double)XP / 120000));
            XPLastGame = TotalXP;
            XP = 0;
            Inv.Clear();
            DjinnPocket.Clear();
            BonusClasses.Clear();
            Dungeons.Clear();

            BattleStatsTotal += BattleStats;
            ServerStatsTotal += ServerStats;
            BattleStats = new BattleStats();
            ServerStats = new ServerStats();
            Tags.Clear();
            Tags.Add($"{Element}Adept");
            NewGames++;
        }

        internal void DealtDmg(uint damage)
        {
            BattleStats.DamageDealt += damage;
        }

        internal void HealedHP(long HPtoHeal)
        {
            BattleStats.HPhealed += (uint)HPtoHeal;
        }

        internal void KilledByHand()
        {
            BattleStats.KillsByHand++;
        }

        internal void Revived()
        {
            BattleStats.Revives++;
        }
    }
}