using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace IodemBot.Modules.ColossoBattles
{
    public class NPCEnemy : ColossoFighter
    {
        [JsonProperty] private int extraTurns { get; set; } = 0;
        [JsonProperty] private string[] movepool { get; set; }

        public NPCEnemy(string name, string imgUrl, Stats stats, ElementalStats elstats, Move[] moves, int extraTurns) : base(name, imgUrl, stats, elstats, moves)
        {
            this.extraTurns = extraTurns;
        }

        [JsonConstructor]
        public NPCEnemy(string name, string imgUrl, Stats stats, ElementalStats elstats, string[] movepool) : base(name, imgUrl, stats, elstats, PsynergyDatabase.GetMovepool(movepool))
        {
            this.movepool = movepool;
        }

        public override List<string> ExtraTurn()
        {
            List<string> log = new List<string>();
            for (int i = 0; i < extraTurns; i++)
            {
                selectRandom();
                log.AddRange(MainTurn());
            }
            return log;
        }

        public override List<string> EndTurn()
        {
            List<string> log = new List<string>();
            if (IsAlive())
            {
                selectRandom();
            }
            else
            {
                selected = new Nothing();
                hasSelected = true;
            }
            log.AddRange(base.EndTurn());
            return log;
        }

        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<NPCEnemy>(serialized);
        }
    }
}