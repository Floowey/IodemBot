using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules.ColossoBattles
{
    public static class EnemiesDatabase
    {
        private static readonly List<List<ColossoFighter>> tutorialFighters;
        private static readonly List<List<ColossoFighter>> bronzeFighters;
        private static readonly List<List<ColossoFighter>> silverFighters;
        private static readonly List<List<ColossoFighter>> goldFighters;
        private static readonly Dictionary<string, NPCEnemy> allEnemies;
        private static readonly Dictionary<string, Dungeon> dungeons;

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
                Console.Write("Enemies not loaded correctly" + e.Message);
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
            else
            {
                throw new KeyNotFoundException(enemyKey);
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
        }

        public class DungeonMatchup
        {
            [JsonConstructor]
            public DungeonMatchup(List<string> EnemyNames)
            {
                EnemyNames.ForEach(s => Enemy.Add(GetEnemy(s)));
                this.EnemyNames = EnemyNames;
            }

            [JsonIgnore] public List<NPCEnemy> Enemy { get; } = new List<NPCEnemy>();
            public List<string> EnemyNames { get; set; }
            public string FlavourText { get; set; }
            public RewardTables RewardTables { get; set; } = new RewardTables();
            public Reward Reward { get; set; } = new Reward();
            public string Image { get; set; }
        }
    }

    public class Reward
    {
        public uint XP { get; set; }
        public uint Coins { get; set; }

        public int ChestProbability { get; set; } = 0;
        public ChestQuality Chest { get; set; }

        public int ItemProbability { get; set; }
        public string Item { get; set; }

        public int DungeonProbability { get; set; }
        public string DungeonUnlock { get; set; }

        public int SecretDungeonProbability { get; set; }
        public string SecretDungeon { get; set; }
    }

    public class Requirement
    {
        public Element[] Elements { get; set; } = new Element[] { };
        public ArchType[] ArchTypes { get; set; } = new ArchType[] { };
        public string[] ClassSeries { get; set; } = new string[] { };
        public string[] Classes { get; set; } = new string[] { };
        public int MinLevel { get; set; } = 0;
        public int MaxLevel { get; set; } = 100;

        public bool Applies(UserAccount playerAvatar)
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

            if (MinLevel > playerAvatar.LevelNumber || MaxLevel < playerAvatar.LevelNumber)
            {
                return false;
            }
            return true;
        }
    }
}