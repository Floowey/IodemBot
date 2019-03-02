using IodemBot.Modules.GoldenSunMechanics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.ColossoBattles
{
    public class NPCEnemy : ColossoFighter
    {
        public NPCEnemy(string name, string imgUrl, Stats stats, ElementalStats elstats, Move[] moves) : base(name, imgUrl, stats, elstats, moves)
        {
        }

        public override List<string> EndTurn()
        {
            selectRandom();
            return base.EndTurn();
        }
    }
}
