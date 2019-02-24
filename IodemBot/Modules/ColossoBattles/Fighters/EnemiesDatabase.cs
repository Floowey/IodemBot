using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IodemBot.Modules.ColossoBattles.ColossoPvE;

namespace IodemBot.Modules.ColossoBattles
{
    public class EnemiesDatabase
    {
        private static List<List<ColossoFighter>> bronzeFighters;
        private static List<List<ColossoFighter>> silverFighters;
        private static List<List<ColossoFighter>> goldFighters;

        static EnemiesDatabase()
        {
            try
            {
            bronzeFighters = loadEnemiesFromFile("Resources/bronzeFighters.json");
            silverFighters = loadEnemiesFromFile("Resources/silverFighters.json");
            goldFighters = loadEnemiesFromFile("Resources/goldFighters.json");

            } catch (Exception e)
            {
                Console.Write("A");
            }
        }

        public static List<List<ColossoFighter>> loadEnemiesFromFile(string filePath)
        {
            List<List<ColossoFighter>> fighters = new List<List<ColossoFighter>>();
            string json = File.ReadAllText(filePath);
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            List<List<FighterImage>> bronzeImages = data.ToObject<List<List<FighterImage>>>();
            bronzeImages.ForEach(enemyTeam => {
                List<ColossoFighter> curTeam = new List<ColossoFighter>();
                enemyTeam.ForEach(fighter =>
                {
                    curTeam.Add(new NPCEnemy(fighter.name, fighter.imgurl, fighter.stats, fighter.elstats, Moveset.getMoveset(fighter.movepool)));
                });
                fighters.Add(curTeam);
            });
            return fighters;
        }

        internal static List<ColossoFighter> getRandomEnemies(BattleDifficulty diff)
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
                    break;
            }
            return selectedDifficulty[(new Random()).Next(0, selectedDifficulty.Count)];
        }
    }


    internal struct FighterImage
    {
        public string name { get; set; }
        public string imgurl { get; set; }
        public string[] movepool { get; set; }
        public Stats stats { get; set; }
        public ElementalStats elstats { get; set; }
    }
}
