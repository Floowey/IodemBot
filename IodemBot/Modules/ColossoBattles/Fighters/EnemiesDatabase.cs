using IodemBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static IodemBot.Modules.ColossoBattles.ColossoPvE;

namespace IodemBot.Modules.ColossoBattles
{
    public static class EnemiesDatabase
    {
        private static readonly List<List<ColossoFighter>> tutorialFighters;
        private static readonly List<List<ColossoFighter>> bronzeFighters;
        private static readonly List<List<ColossoFighter>> silverFighters;
        private static readonly List<List<ColossoFighter>> goldFighters;

        static EnemiesDatabase()
        {
            try
            {
                tutorialFighters = LoadEnemiesFromFile("Resources/tutorialFighters.json");
                bronzeFighters = LoadEnemiesFromFile("Resources/bronzeFighters.json");
                silverFighters = LoadEnemiesFromFile("Resources/silverFighters.json");
                goldFighters = LoadEnemiesFromFile("Resources/goldFighters.json");
            }
            catch (Exception e) // Just for debugging
            {
                Console.Write("A" + e.Message);
            }
        }

        public static List<List<ColossoFighter>> LoadEnemiesFromFile(string filePath)
        {
            //List<List<ColossoFighter>> fighters = new List<List<ColossoFighter>>();
            string json = File.ReadAllText(filePath);
            List<List<NPCEnemy>> fighters = JsonConvert.DeserializeObject<List<List<NPCEnemy>>>(json);
            return fighters.Select(s1 => s1.Select(s2 => (ColossoFighter)s2).ToList()).ToList();
        }

        internal static List<ColossoFighter> GetRandomEnemies(BattleDifficulty diff)
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

            try
            {
                var enemies = selectedDifficulty.Random().Select(f => (ColossoFighter)f.Clone()).ToList();
                if (diff == BattleDifficulty.MediumRare)
                {
                    enemies.ForEach(e => e.stats *= 1.5);
                }
                return enemies;
            }
            catch (Exception e)
            {
                throw e;
            }
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
            var enemies = selectedDifficulty.Where(l => l.Any(e => e.name.ToUpper().Contains(enemy.ToUpper()))).FirstOrDefault();
            if (enemies == null)
            {
                enemies = GetRandomEnemies(diff);
            }

            return enemies.Select(f => (ColossoFighter)f.Clone()).ToList();
        }
    }
}