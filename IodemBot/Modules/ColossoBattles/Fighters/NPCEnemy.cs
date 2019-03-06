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
        int extraTurns { get; set; } = 0;
        public NPCEnemy(string name, string imgUrl, Stats stats, ElementalStats elstats, Move[] moves, int extraTurns) : base(name, imgUrl, stats, elstats, moves)
        {
            this.extraTurns = extraTurns;
        }

        public override List<string> EndTurn()
        {
            List<string> log = new List<string>();
            for(int i = 0; i < extraTurns; i++)
            {
                selectRandom();
                log.AddRange(MainTurn());
            }
            
            selectRandom();
            log.AddRange(base.EndTurn());
            return log;
        }
    }
}
