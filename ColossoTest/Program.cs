using ColossoTest.Colosso;
using System;
using System.Linq;

namespace ColossoTest
{
    class Program
    {
        enum State { A,B};
        static void Main(string[] args)
        {
            Battle();
        }

        private static void Battle()
        {
            var battle = new ColossoBattle();

            var enemy = new NPCEnemy("Enemy",
                StatList.getStats("Squire", 50),
                StatList.getElementalStats(Psynergy.Element.Venus),
                Moveset.getMoveset("Squire"));

            var player = new PlayerFighter("Player 1",
                StatList.getStats("Squire", 50),
                StatList.getElementalStats(Psynergy.Element.Mercury),
                Moveset.getMoveset("Squire"));

            battle.AddPlayer(player, ColossoBattle.Team.A);
            battle.AddPlayer(enemy, ColossoBattle.Team.B);

            while (battle.isActive)
            {
                Console.WriteLine($"{enemy.stats.HP} / {enemy.stats.maxHP}" +
                    $" vs. {player.stats.HP} / {player.stats.maxHP}");
                Console.WriteLine($"{enemy.stats.PP} / {enemy.stats.maxPP}" +
                   $" vs. {player.stats.PP} / {player.stats.maxPP}");

                enemy.selectRandom();

                Console.WriteLine(player.moves.Aggregate("", (s, m) => s += m.name + " "));
                uint.TryParse(Console.ReadLine(), out uint a);
                int.TryParse(Console.ReadLine(), out int b);
                //player.select(a, b);
                battle.Turn();
                Console.Write(battle.log.Aggregate("", (s, l) => s += l + "\n"));
                Console.WriteLine();
            }

            battle.resetGame();
        }
    }
}
