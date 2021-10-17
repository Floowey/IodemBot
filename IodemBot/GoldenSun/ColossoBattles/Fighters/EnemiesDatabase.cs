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
        private static readonly List<List<ColossoFighter>> tutorialFighters;
        private static readonly List<List<ColossoFighter>> bronzeFighters;
        private static readonly List<List<ColossoFighter>> silverFighters;
        private static readonly List<List<ColossoFighter>> goldFighters;
        private static readonly Dictionary<string, NPCEnemy> allEnemies;
        public static readonly Dictionary<string, Dungeon> dungeons;

        public static List<Dungeon> DefaultDungeons { get { return dungeons.Where(d => d.Value.IsDefault).Select(d => d.Value).ToList(); } }

        static EnemiesDatabase()
        {
            try
            {
                tutorialFighters = LoadEnemiesFromFile("Resources/GoldenSun/Battles/tutorialFighters.json");
                bronzeFighters = LoadEnemiesFromFile("Resources/GoldenSun/Battles/bronzeFighters.json");
                silverFighters = LoadEnemiesFromFile("Resources/GoldenSun/Battles/silverFighters.json");
                goldFighters = LoadEnemiesFromFile("Resources/GoldenSun/Battles/goldFighters.json");

                string json = File.ReadAllText("Resources/GoldenSun/Battles/enemies.json");
                allEnemies = new Dictionary<string, NPCEnemy>(
                    JsonConvert.DeserializeObject<Dictionary<string, NPCEnemy>>(json),
                    StringComparer.InvariantCultureIgnoreCase);

                json = File.ReadAllText("Resources/GoldenSun/Battles/dungeons.json");
                dungeons = new Dictionary<string, Dungeon>(
                    JsonConvert.DeserializeObject<Dictionary<string, Dungeon>>(json),
                    StringComparer.InvariantCultureIgnoreCase);
            }
            catch (Exception e) // Just for debugging
            {
                Console.Write("Enemies not loaded correctly" + e);
            }
        }

        public static List<List<ColossoFighter>> LoadEnemiesFromFile(string filePath)
        {
            //List<List<ColossoFighter>> fighters = new List<List<ColossoFighter>>();
            string json = File.ReadAllText(filePath);
            List<List<NPCEnemy>> fighters = JsonConvert.DeserializeObject<List<List<NPCEnemy>>>(json);
            return fighters.Select(s1 => s1.Select(s2 => (ColossoFighter)s2).ToList()).ToList();
        }

        internal static List<ColossoFighter> GetRandomEnemies(BattleDifficulty diff, double boost = 1)
        {
            List<List<ColossoFighter>> selectedDifficulty;
            switch (diff)
            {
                case (BattleDifficulty.Tutorial):
                    selectedDifficulty = tutorialFighters;
                    break;

                case (BattleDifficulty.Easy):
                    selectedDifficulty = bronzeFighters;
                    break;

                case (BattleDifficulty.Medium):
                case (BattleDifficulty.MediumRare):
                    selectedDifficulty = silverFighters;
                    break;

                case (BattleDifficulty.Hard):
                    selectedDifficulty = goldFighters;
                    break;

                default:
                    selectedDifficulty = bronzeFighters;
                    Console.WriteLine("Enemies from default!!!");
                    break;
            }

            var enemies = selectedDifficulty.Random().Select(f => (ColossoFighter)f.Clone()).ToList();
            if (diff == BattleDifficulty.MediumRare)
            {
                enemies.ForEach(e => e.Stats *= 1.5);
            }
            enemies.ForEach(e => e.Stats *= boost);
            if (enemies.Count == 0)
            {
                Console.WriteLine($"{diff}: Enemies were empty");
                enemies = GetRandomEnemies(diff);
            }
            return enemies;
        }

        internal static NPCEnemy GetEnemy(string enemyKey)
        {
            if (allEnemies.TryGetValue(enemyKey, out NPCEnemy enemy))
            {
                return (NPCEnemy)enemy.Clone();
            }
            else if (enemyKey.StartsWith("Trap"))
            {
                allEnemies.TryGetValue("DeathTrap", out var trapEnemy);
                var clone = (NPCEnemy)trapEnemy.Clone();
                clone.Name = enemyKey.Split(':').Last();
                return clone;
            }
            else if (enemyKey.StartsWith("BoobyTrap"))
            {
                allEnemies.TryGetValue("BoobyTrap", out var trapEnemy);
                var clone = (NPCEnemy)trapEnemy.Clone();
                clone.Name = enemyKey.Split(':').Last();
                var args = enemyKey.Split(':').First();
                foreach (var arg in args.Split('-').Skip(1))
                {
                    if (int.TryParse(arg, out int damage))
                    {
                        clone.Stats.Atk = damage;
                    }
                    else if (Enum.TryParse(arg, out Condition c))
                    {
                        clone.EquipmentWithEffect.Add(new Item() { Unleash = new Unleash() { Effects = new List<Effect>() { new ConditionEffect() { Condition = c } } }, ChanceToActivate = 100, ChanceToBreak = 0 });
                    }

                }
                return clone;
            }
            else if (enemyKey.StartsWith("Key"))
            {
                allEnemies.TryGetValue("Key", out var trapEnemy);
                var clone = (NPCEnemy)trapEnemy.Clone();
                clone.Name = enemyKey.Split(':').Last();
                return clone;

            }
            else
            {
                Console.WriteLine($"{enemyKey} not found! Generating Dummy");
                return new NPCEnemy($"{enemyKey} Not Implemented", Sprites.GetRandomSprite(), new Stats(), new ElementalStats(), new string[] { }, true, true);
            }
        }

        internal static Dungeon GetDungeon(string dungeonKey)
        {
            if (dungeons.TryGetValue(dungeonKey, out Dungeon dungeon))
            {
                return dungeon;
            }
            else
            {
                throw new KeyNotFoundException(dungeonKey);
            }
        }

        internal static bool TryGetDungeon(string dungeonKey, out Dungeon dungeon)
        {
            if (dungeons.TryGetValue(dungeonKey, out dungeon))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool HasDungeon(string dungeonKey)
        {
            return dungeons.ContainsKey(dungeonKey);
        }

        internal static List<ColossoFighter> GetEnemies(BattleDifficulty diff, string enemy)
        {
            List<List<ColossoFighter>> selectedDifficulty;
            switch (diff)
            {
                case (BattleDifficulty.Easy):
                    selectedDifficulty = bronzeFighters;
                    break;

                case (BattleDifficulty.Medium):
                    selectedDifficulty = silverFighters;
                    break;

                case (BattleDifficulty.Hard):
                    selectedDifficulty = goldFighters;
                    break;

                default:
                    selectedDifficulty = bronzeFighters;
                    Console.WriteLine("Enemies from default!!!");
                    break;
            }
            var enemies = selectedDifficulty.Where(l => l.Any(e => e.Name.ToUpper().Contains(enemy.ToUpper()))).FirstOrDefault();
            if (enemies == null)
            {
                enemies = GetRandomEnemies(diff);
            }

            return enemies.Select(f => (ColossoFighter)f.Clone()).ToList();
        }

        public class Dungeon
        {
            public List<DungeonMatchup> Matchups { get; set; }
            public Requirement Requirement { get; set; } = new Requirement();
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
            public DungeonMatchup(List<string> EnemyNames)
            {
                this.EnemyNames = EnemyNames;
            }

            [JsonIgnore]
            public List<NPCEnemy> Enemy
            {
                get
                {
                    var enemies = new List<NPCEnemy>();
                    EnemyNames.ForEach(s =>
                    {
                        var enemy = GetEnemy(s);
                        enemies.Add(enemy);
                    });
                    return enemies;
                }
            }

            public List<string> EnemyNames { get; set; }
            public string FlavourText { get; set; }
            public RewardTables RewardTables { get; set; } = new RewardTables();
            public string Image { get; set; }
            public bool Shuffle { get => Keywords.Contains("Shuffle"); }
            public bool HealBefore { get => Keywords.Contains("Heal"); }

            public List<string> Keywords { get; set; } = new List<string>();
        }
    }

    public class Requirement
    {
        public Element[] Elements { get; set; } = new Element[] { };

        public ArchType[] ArchTypes { get; set; } = new ArchType[] { };

        public string[] ClassSeries { get; set; } = new string[] { };
        public string[] Classes { get; set; } = new string[] { };
        public int MinLevel { get; set; } = 0;
        public int MaxLevel { get; set; } = 200;

        public string[] TagsRequired { get; set; } = new string[] { };
        public string[] TagsAny { get; set; } = new string[] { };
        public int TagsHowMany { get; set; } = 0;
        public string[] TagsLock { get; set; } = new string[] { };

        public string GetDescription()
        {
            var s = new List<string>();
            if (ArchTypes.Length > 0)
            {
                s.Add($"For Archtypes: {string.Join(", ", ArchTypes.Select(a => a.ToString()))}");
            }
            if (ClassSeries.Length > 0)
            {
                s.Add($"For Class series: {string.Join(", ", ClassSeries.Select(a => a.ToString()))}");
            }
            if (Classes.Length > 0)
            {
                s.Add($"For Classes: {string.Join(", ", Classes.Select(a => a.ToString()))}");
            }
            if (Elements.Length > 0)
            {
                s.Add($"For Elements: {string.Join(", ", Elements.Select(a => a.ToString()))}");
            }
            if (MinLevel > 0)
            {
                s.Add($"Minimum Level: {MinLevel}");
            }
            if (MaxLevel < 200)
            {
                s.Add($"Maximum Level: {MaxLevel}");
            }
            if (TagsRequired.Count() > 0 || TagsAny.Count() > 0)
            {
                s.Add($"Requires completion of a previous dungeon.");
            }
            if (s.Count == 0)
            {
                s.Add($"No Requirements");
            }
            return string.Join("\n", s);
        }

        public bool IsLocked(UserAccount playerAccount)
        => (TagsLock.Count() > 0 && TagsLock.Any(t => playerAccount.Tags.Contains(t)));

        public bool FulfilledRequirements(UserAccount playerAvatar)
        {
            if (Elements.Count() > 0 && !Elements.Contains(playerAvatar.Element))
            {
                return false;
            }

            if (Classes.Count() > 0 && !Classes.Contains(playerAvatar.GsClass))
            {
                return false;
            }

            if (ClassSeries.Count() > 0 && !ClassSeries.Contains(AdeptClassSeriesManager.GetClassSeries(playerAvatar).Name))
            {
                return false;
            }

            if (ArchTypes.Count() > 0 && !ArchTypes.Contains(AdeptClassSeriesManager.GetClassSeries(playerAvatar).Archtype))
            {
                return false;
            }

            if (TagsRequired.Count() > 0 && !TagsRequired.All(t => playerAvatar.Tags.Contains(t)))
            {
                return false;
            }

            if (TagsAny.Count() > 0 && TagsAny.Count(t => playerAvatar.Tags.Contains(t)) < TagsHowMany)
            {
                return false;
            }

            if (MinLevel > playerAvatar.LevelNumber || MaxLevel < playerAvatar.LevelNumber)
            {
                return false;
            }
            return true;
        }

        public bool Applies(UserAccount playerAvatar)
        {
            return FulfilledRequirements(playerAvatar) && !IsLocked(playerAvatar);
        }
    }
}