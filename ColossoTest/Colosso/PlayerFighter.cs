using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColossoTest.Colosso
{
    public class PlayerFighter : ColossoFighter
    {
        public PlayerFighter(SocketGuildUser user) : base(name, stats, elstats, moves)
        {
            
        }

        public override List<string> dealDamage(uint damage)
        {
            var log = new List<string>();
            log.Add($"{name} takes {damage} damage.");
            if (damage < stats.HP)
            {
                stats.HP -= damage;
            } else
            {
                stats.HP = 0;
                log.Add($"{name} goes down.");
            }
            return log;
        }

        public override void EndTurn()
        {
            var newBuffs = new List<Buff>();
            buffs.ForEach(s => {
                s.turns -= 1;
                if (s.turns >= 1)
                {
                    newBuffs.Add(s);
                }
            });
            buffs = newBuffs;
            selected = null;
            hasSelected = false;
        }

        public override void StartTurn()
        {
            if (selected.name == "Defend")
            {
                var a = selected.Use(this);
            }
        }
    }
}
