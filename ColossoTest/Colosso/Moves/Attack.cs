using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColossoTest.Colosso
{
    public class Attack : Move
    {
        public Attack() : base("Attack", "\u2694", Target.otherSingle, 1)
        {
        }

        public override List<string> Use(ColossoFighter User)
        {
            var enemy = User.battle.getTeam(User.enemies)[targetNr];

            var log = new List<string>();
            log.Add($"{User.name} attacks!");
            var atk = User.stats.Atk * User.MultiplyBuffs("Atk");
            var def = enemy.stats.Def * enemy.MultiplyBuffs("Def");
            uint damage = 1;
            if (def < atk)
            {
                damage = (uint) (atk - def)/2 + (uint)(new Random()).Next(1, 4);
            }
            if ((new Random()).Next(0, 8) == 0)
            {
                log.Add("Critical!!");
                damage = (uint)(damage*1.25 + (new Random()).Next(5,15));    
            }
            log.AddRange(enemy.dealDamage(damage));
            //log.Add($"{enemy.name} takes {damage} damage.");
            return log;
        }
    }
}
