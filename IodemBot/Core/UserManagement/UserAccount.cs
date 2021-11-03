using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;

namespace IodemBot.Core.UserManagement
{

    public class UserAccount : IEquatable<UserAccount>
    {
        public static readonly ulong[][] rates = new ulong[][] {
                    new ulong[] { 2538160, 25000, 100 },
                    new ulong[] { 1196934, 2500, 90 },
                    new ulong[] { 605000, 1000, 80 },
                    new ulong[]{ 125000, 200, 50 },
                    new ulong[]{ 0, 50, 0 }
        };
        public ulong ID { get; set; }
        public string Name { get => HiddenName; set => HiddenName = value.RemoveBadChars(); }
        [JsonProperty] private string HiddenName { get; set; }

        public Inventory Inv { get; set; } = new Inventory();
        public List<string> Dungeons { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public DjinnPocket DjinnPocket { get; set; } = new DjinnPocket();
        public int ClassToggle { get; set; } = 0;
        public List<string> BonusClasses { get; set; } = new List<string>();
        public Element Element { get; set; } = Element.none;
        public Loadouts Loadouts { get; set; } = new Loadouts();
        public TrophyCase TrophyCase { get; set; } = new TrophyCase();
        public Preferences Preferences { get; set; } = new();

        [JsonIgnore]
        public string GsClass
        {
            get
            {
                return AdeptClassSeriesManager.GetClass(this).Name; //GoldenSun.getClass(element, LevelNumber, (uint) classToggle);
            }
        }
        public DateTime LastXP { get; set; }

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
        public bool arePublicCodes { get; set; } = false;
        public string N3DSCode { get; set; } = "0000-0000-0000";
        public int NewGames { get; set; } = 0;

        public DateTime LastReset { get; set; } = DateTime.MinValue;
        public string PoGoCode { get; set; } = "0000-0000-0000";
        public string SwitchCode { get; set; } = "0000-0000-0000";
        [JsonIgnore] public ulong TotalXP { get { return XPLastGame + XP; } }
        public ulong XP { get; set; } = 0;
        public double XpBoost { get; set; } = 1;
        public ulong XPLastGame { get; set; } = 0;
        public int DjinnBadLuck { get; set; } = 0;
        public ServerStats ServerStats { get; set; } = new ServerStats();
        public ServerStats ServerStatsTotal { get; set; } = new ServerStats();
        public BattleStats BattleStats { get; set; } = new BattleStats();
        public BattleStats BattleStatsTotal { get; set; } = new BattleStats();
        public string ImgUrl { get; set; }
        public void AddXp(ulong xp)
        {
            XP += (ulong)(xp * XpBoost);
        }
        public void NewGame()
        {
            XpBoost *= 1 + 0.1 * (1 - Math.Exp(-(double)XP / 120000));
            XpBoost = Math.Min(2, XpBoost);
            XPLastGame = TotalXP;

            if (LevelNumber >= 99)
            {
                TrophyCase.Trophies.Add(new Trophy()
                {
                    Icon = "<:99Trophy:739170181745475601>",
                    Text = $"Awarded for resetting their character at level {LevelNumber}",
                    ObtainedOn = DateTime.Now
                }
                );
            }
            else if (LevelNumber >= 90)
            {
                TrophyCase.Trophies.Add(new Trophy()
                {
                    Icon = "<:90Trophy:739170181359599687>",
                    Text = $"Awarded for resetting their character at level {LevelNumber}",
                    ObtainedOn = DateTime.Now
                }
                );
            }
            else if (LevelNumber >= 75)
            {
                TrophyCase.Trophies.Add(new Trophy()
                {
                    Icon = "<:75Trophy:739170181334695978>",
                    Text = $"Awarded for resetting their character at level {LevelNumber}",
                    ObtainedOn = DateTime.Now
                }
                );
            }
            else if (LevelNumber >= 50)
            {
                TrophyCase.Trophies.Add(new Trophy()
                {
                    Icon = "<:50Trophy:739170181602869289>",
                    Text = $"Awarded for resetting their character at level {LevelNumber}",
                    ObtainedOn = DateTime.Now
                }
                );
            }

            XP = 0;
            Inv.Clear();
            DjinnPocket.Clear();
            BonusClasses.Clear();
            Dungeons.Clear();
            Loadouts.loadouts.Clear();
            Preferences.AutoSell.Clear();

            BattleStatsTotal += BattleStats;
            ServerStatsTotal += ServerStats;
            BattleStats = new BattleStats();
            ServerStats = new ServerStats();
            LastReset = DateTime.Now;
            Tags.RemoveAll(t => !t.Contains("Halloween20"));
            Tags.Add($"{Element}Adept");
            NewGames++;
        }
        public bool Equals([AllowNull] UserAccount other)
        {
            if (other == null)
            {
                return false;
            }

            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as UserAccount);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ID);
        }
    }

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

    public class EndlessStreak
    {
        public int Solo { get; set; }
        public int Duo { get; set; } = 0;
        public string DuoNames { get; set; } = "";
        public int Trio { get; set; } = 0;
        public string TrioNames { get; set; } = "";
        public int Quad { get; set; } = 0;
        public string QuadNames { get; set; } = "";

        public Tuple<int, string> GetEntry(RankEnum rank)
        {
            return rank switch
            {
                RankEnum.Solo => new Tuple<int, string>(Solo, ""),
                RankEnum.Duo => new Tuple<int, string>(Duo, DuoNames),
                RankEnum.Trio => new Tuple<int, string>(Trio, TrioNames),
                RankEnum.Quad => new Tuple<int, string>(Quad, QuadNames),
                _ => null,
            };
        }
        public void AddStreak(int streak, int players = 1, string Names = "")
        {
            switch (players)
            {
                case 1:
                    Solo = Math.Max(Solo, streak);
                    break;

                case 2:
                    if (streak > Duo)
                    {
                        Duo = streak;
                        DuoNames = Names;
                    }
                    break;

                case 3:
                    if (streak > Trio)
                    {
                        Trio = streak;
                        TrioNames = Names;
                    }
                    break;

                case 4:
                    if (streak > Quad)
                    {
                        Quad = streak;
                        QuadNames = Names;
                    }
                    break;
                default: break;
            }
        }


        public static EndlessStreak operator +(EndlessStreak s1, EndlessStreak s2)
        {
            return new EndlessStreak()
            {
                Solo = Math.Max(s1.Solo, s2.Solo),
                Duo = Math.Max(s1.Duo, s2.Duo),
                Trio = Math.Max(s1.Trio, s2.Trio),
                Quad = Math.Max(s1.Quad, s2.Quad),
                DuoNames = s1.Duo > s2.Duo ? s1.DuoNames : s2.DuoNames,
                TrioNames = s1.Trio > s2.Trio ? s1.TrioNames : s2.TrioNames,
                QuadNames = s1.Quad > s2.Quad ? s1.QuadNames : s2.QuadNames,
            };
        }
    }

    public class ServerStats
    {
        public ulong ChannelSwitches { get; set; } = 0;
        public EndlessStreak EndlessStreak { get; set; } = new EndlessStreak();
        public EndlessStreak LegacyStreak { get; set; } = new EndlessStreak();
        public uint ColossoHighestStreak { get; set; } = 0;
        public uint ColossoStreak { get; set; } = 0;
        public uint ColossoWins { get; set; } = 0;
        public uint CommandsUsed { get; set; } = 0;
        public DateTime LastDayActive { get; set; } = DateTime.Now;
        public int LookedUpClass { get; set; } = 0;
        public int LookedUpInformation { get; set; } = 0;
        public int MessagesInColossoTalks { get; set; } = 0;
        public ulong MostRecentChannel { get; set; } = 0;
        public int ReactionsAdded { get; set; } = 0;
        public uint RpsStreak { get; set; } = 0;
        public uint RpsWins { get; set; } = 0;
        public uint SpentMoneyOnArtifacts { get; set; }
        public int UniqueDaysActive { get; set; } = 0;
        public int DungeonsCompleted { get; set; } = 0;
        public string LastDungeon { get; set; } = "";
        public int SameDungeonInARow { get; set; } = 0;

        public EndlessStreak GetStreak(EndlessMode mode) => mode == EndlessMode.Default ? EndlessStreak : LegacyStreak;

        public static ServerStats operator +(ServerStats s1, ServerStats s2)
        {
            return new ServerStats()
            {
                ColossoHighestStreak = Math.Max(s1.ColossoHighestStreak, s2.ColossoHighestStreak),
                EndlessStreak = s1.EndlessStreak + s2.EndlessStreak,
                LegacyStreak = s1.LegacyStreak + s2.LegacyStreak,
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

}
