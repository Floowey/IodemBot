using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;

namespace IodemBot.ColossoBattles
{
    public static class EnemiesDatabase
    {
        private static readonly List<List<ColossoFighter>> TutorialFighters;
        private static readonly List<List<ColossoFighter>> BronzeFighters;
        private static readonly List<List<ColossoFighter>> SilverFighters;
        private static readonly List<List<ColossoFighter>> GoldFighters;
        private static readonly Dictionary<string, NpcEnemy> AllEnemies;
        public static readonly Dictionary<string, Dungeon> Dungeons;

        static EnemiesDatabase()
        {
            try
            {
                TutorialFighters = LoadEnemiesFromFile("Resources/GoldenSun/Battles/tutorialFighters.json");
                BronzeFighters = LoadEnemiesFromFile("Resources/GoldenSun/Battles/bronzeFighters.json");
                SilverFighters = LoadEnemiesFromFile("Resources/GoldenSun/Battles/silverFighters.json");
                GoldFighters = LoadEnemiesFromFile("Resources/GoldenSun/Battles/goldFighters.json");

                var json = File.ReadAllText("Resources/GoldenSun/Battles/enemies.json");
                AllEnemies = new Dictionary<string, NpcEnemy>(
                    JsonConvert.DeserializeObject<Dictionary<string, NpcEnemy>>(json),
                    StringComparer.InvariantCultureIgnoreCase);

                json = File.ReadAllText("Resources/GoldenSun/Battles/dungeons.json");
                Dungeons = new Dictionary<string, Dungeon>(
                    JsonConvert.DeserializeObject<Dictionary<string, Dungeon>>(json),
                    StringComparer.InvariantCultureIgnoreCase);

                Dungeons.Values.ToList().SelectMany(d => d.Matchups.SelectMany(m => m.Enemy)).ToList();
            }
            catch (Exception e) // Just for debugging
            {
                Console.Write("enemies not loaded correctly" + e);
            }
        }

        public static List<Dungeon> DefaultDungeons
        {
            get { return Dungeons.Where(d => d.Value.IsDefault).Select(d => d.Value).ToList(); }
        }

        public static List<List<ColossoFighter>> LoadEnemiesFromFile(string filePath)
        {
            //List<List<ColossoFighter>> fighters = new List<List<ColossoFighter>>();
            var json = File.ReadAllText(filePath);
            var fighters = JsonConvert.DeserializeObject<List<List<NpcEnemy>>>(json);
            return fighters.Select(s1 => s1.Select(s2 => (ColossoFighter)s2).ToList()).ToList();
        }

        internal static List<ColossoFighter> GetRandomEnemies(BattleDifficulty diff, double boost = 1)
        {
            List<List<ColossoFighter>> selectedDifficulty;
            switch (diff)
            {
                case BattleDifficulty.Tutorial:
                    selectedDifficulty = TutorialFighters;
                    break;

                case BattleDifficulty.Easy:
                    selectedDifficulty = BronzeFighters;
                    break;

                case BattleDifficulty.Medium:
                case BattleDifficulty.MediumRare:
                    selectedDifficulty = SilverFighters;
                    break;

                case BattleDifficulty.Hard:
                    selectedDifficulty = GoldFighters;
                    break;

                default:
                    selectedDifficulty = BronzeFighters;
                    Console.WriteLine("enemies from default!!!");
                    break;
            }

            var enemies = selectedDifficulty.Random().Select(f => (ColossoFighter)f.Clone()).ToList();
            if (diff == BattleDifficulty.MediumRare) enemies.ForEach(e => e.Stats *= 1.5);
            enemies.ForEach(e => e.Stats *= boost);
            if (enemies.Count == 0)
            {
                Console.WriteLine($"{diff}: enemies were empty");
                enemies = GetRandomEnemies(diff);
            }

            return enemies;
        }

        internal static NpcEnemy GetEnemy(string enemyKey)
        {
            var tags = enemyKey.Contains('|') ? enemyKey.Split('|').Skip(1).ToList() : new();
            enemyKey = enemyKey.Split('|').First();
            var nickname = enemyKey.Contains(':') ? enemyKey.Split(':').Last() : null;
            enemyKey = enemyKey.Split(':').First();

            NpcEnemy outEnemy = null;

            if (AllEnemies.TryGetValue(enemyKey, out var enemy))
                outEnemy = enemy.Clone() as NpcEnemy;

            if (enemyKey.StartsWith("Trap"))
            {
                AllEnemies.TryGetValue("DeathTrap", out var trapEnemy);
                outEnemy = trapEnemy.Clone() as NpcEnemy;
            }

            if (enemyKey.StartsWith("BoobyTrap"))
            {
                AllEnemies.TryGetValue("BoobyTrap", out var trapEnemy);
                outEnemy = trapEnemy.Clone() as NpcEnemy;
                var args = enemyKey.Split(':').First();
                foreach (var arg in args.Split('-').Skip(1))
                    if (int.TryParse(arg, out var damage))
                        outEnemy.Stats.Atk = damage;
                    else if (Enum.TryParse(arg, out Condition c))
                        outEnemy.EquipmentWithEffect.Add(new Item
                        {
                            Unleash = new Unleash { Effects = new List<Effect> { new ConditionEffect { Condition = c } } },
                            ChanceToActivate = 100,
                            ChanceToBreak = 0
                        });
            }

            if (enemyKey.StartsWith("Choice"))
            {
                AllEnemies.TryGetValue("BoobyTrap", out var trapEnemy);
                outEnemy = (NpcEnemy)trapEnemy.Clone();
                outEnemy.Stats.Atk = 0;
            }

            if (enemyKey.StartsWith("Key"))
            {
                AllEnemies.TryGetValue("Key", out var trapEnemy);
                outEnemy = (NpcEnemy)trapEnemy.Clone();
            }

            if (outEnemy == null)
            {
                throw new ArgumentException("Enemy not found");
                //Console.WriteLine($"{enemyKey} was not found.");
                //outEnemy = new($"{enemyKey} Not Implemented", Sprites.GetRandomSprite(), new Stats(),
                //    new ElementalStats(), Array.Empty<string>(), false, false);
            }
            else
            {
                outEnemy.Name = nickname ?? outEnemy.Name;
            }

            outEnemy.Tags.AddRange(tags);
            return outEnemy;
        }

        internal static Dungeon GetDungeon(string dungeonKey)
        {
            if (Dungeons.TryGetValue(dungeonKey, out var dungeon))
                return dungeon;
            throw new KeyNotFoundException(dungeonKey);
        }

        internal static bool TryGetDungeon(string dungeonKey, out Dungeon dungeon)
        {
            if (Dungeons.TryGetValue(dungeonKey, out dungeon))
                return true;
            return false;
        }

        internal static bool HasDungeon(string dungeonKey)
        {
            return Dungeons.ContainsKey(dungeonKey);
        }

        internal static List<ColossoFighter> GetEnemies(BattleDifficulty diff, string enemy)
        {
            List<List<ColossoFighter>> selectedDifficulty;
            switch (diff)
            {
                case BattleDifficulty.Easy:
                    selectedDifficulty = BronzeFighters;
                    break;

                case BattleDifficulty.Medium:
                    selectedDifficulty = SilverFighters;
                    break;

                case BattleDifficulty.Hard:
                    selectedDifficulty = GoldFighters;
                    break;

                default:
                    selectedDifficulty = BronzeFighters;
                    Console.WriteLine("enemies from default!!!");
                    break;
            }

            var enemies = selectedDifficulty
                              .FirstOrDefault(enemyLists => enemyLists.Any(e =>
                                  e.Name.Equals(enemy, StringComparison.CurrentCultureIgnoreCase)))
                          ?? GetRandomEnemies(diff);

            return enemies.Select(f => (ColossoFighter)f.Clone()).ToList();
        }

        public class Dungeon
        {
            public List<DungeonMatchup> Matchups { get; set; }
            public Requirement Requirement { get; set; } = new();
            public string Name { get; set; }
            public string FlavourText { get; set; }
            public string Image { get; set; }
            public bool IsOneTimeOnly { get; set; }
            public bool IsDefault { get; set; }
            public uint MaxPlayer { get; set; } = 4;
        }

        public class DungeonMatchup
        {
            [JsonConstructor]
            public DungeonMatchup(List<string> enemyNames)
            {
                EnemyNames = enemyNames;
            }

            [JsonIgnore]
            public List<NpcEnemy> Enemy
            {
                get
                {
                    var enemies = new List<NpcEnemy>();
                    EnemyNames.ForEach(s =>
                    {
                        var enemy = GetEnemy(s);
                        enemies.Add(enemy);
                        if (enemy.Name.Contains("Not Implemented"))
                            Console.WriteLine($"Enemy {s} not found. {FlavourText}");
                    });
                    return enemies;
                }
            }

            public List<string> EnemyNames { get; set; }
            public string FlavourText { get; set; }
            public RewardTables RewardTables { get; set; } = new();
            public string Image { get; set; }
            public bool Shuffle => Keywords.Contains("Shuffle");
            public bool HealBefore => Keywords.Contains("Heal");

            public List<string> Keywords { get; set; } = new();
        }
    }

    public class Requirement
    {
        public Element[] Elements { get; set; } = Array.Empty<Element>();

        public ArchType[] ArchTypes { get; set; } = Array.Empty<ArchType>();

        public string[] ClassSeries { get; set; } = Array.Empty<string>();
        public string[] Classes { get; set; } = Array.Empty<string>();
        public int MinLevel { get; set; } = 0;
        public int MaxLevel { get; set; } = 200;

        public string[] TagsRequired { get; set; } = Array.Empty<string>();
        public string[] TagsAny { get; set; } = Array.Empty<string>();
        public int TagsHowMany { get; set; } = 0;
        public string[] TagsLock { get; set; } = Array.Empty<string>();

        public string GetDescription()
        {
            var s = new List<string>();
            if (ArchTypes.Length > 0) s.Add($"For Archtypes: {string.Join(", ", ArchTypes.Select(a => a.ToString()))}");
            if (ClassSeries.Length > 0)
                s.Add($"For Class series: {string.Join(", ", ClassSeries.Select(a => a.ToString()))}");
            if (Classes.Length > 0) s.Add($"For Classes: {string.Join(", ", Classes.Select(a => a.ToString()))}");
            if (Elements.Length > 0) s.Add($"For Elements: {string.Join(", ", Elements.Select(a => a.ToString()))}");
            if (MinLevel > 0) s.Add($"Minimum Level: {MinLevel}");
            if (MaxLevel < 200) s.Add($"Maximum Level: {MaxLevel}");
            if (TagsRequired.Length > 0 || TagsAny.Length > 0) s.Add("Requires completion of a previous dungeon.");
            if (s.Count == 0) s.Add("No Requirements");
            return string.Join("\n", s);
        }

        public bool IsLocked(UserAccount playerAccount)
        {
            return TagsLock.Length > 0 && TagsLock.Any(t => playerAccount.Tags.Contains(t));
        }

        public bool FulfilledRequirements(UserAccount playerAvatar)
        {
            if (Elements.Length > 0 && !Elements.Contains(playerAvatar.Element)) return false;

            if (Classes.Length > 0 && !Classes.Contains(playerAvatar.GsClass)) return false;

            if (ClassSeries.Length > 0 &&
                !ClassSeries.Contains(playerAvatar.ClassSeries.Name)) return false;

            if (ArchTypes.Length > 0 &&
                !ArchTypes.Contains(playerAvatar.ClassSeries.Archtype)) return false;

            if (TagsRequired.Length > 0 && !TagsRequired.All(t => playerAvatar.Tags.Contains(t))) return false;

            if (TagsAny.Length > 0 && TagsAny.Count(t => playerAvatar.Tags.Contains(t)) < TagsHowMany) return false;

            if (playerAvatar.Oaths.IsOathActive(Oath.Oaf))
                return true;

            if (MinLevel > playerAvatar.LevelNumber || MaxLevel < playerAvatar.LevelNumber) return false;
            return true;
        }

        public bool Applies(UserAccount playerAvatar)
        {
            return FulfilledRequirements(playerAvatar) && !IsLocked(playerAvatar);
        }
    }
}