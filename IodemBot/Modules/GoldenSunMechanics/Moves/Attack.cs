using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Attack : Move
    {
        public Attack() : base("Attack", "<:Attack:536919809393295381>", Target.otherSingle, 1, new List<EffectImage>())
        {
        }

        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<Attack>(serialized);
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            var enemy = User.battle.getTeam(User.enemies)[targetNr];

            var log = new List<string>();
            log.Add($"{emote} {User.name} attacks!");
            if (!enemy.IsAlive())
            {
                log.Add($"{enemy.name} is down already!");
                return log;
            }

            int chanceToMiss = 8;
            if (User.HasCondition(Condition.Delusion)) chanceToMiss = 3;
           
            if(Global.random.Next(0, chanceToMiss) == 0)
            {
                log.Add($"{enemy.name} dodges the blow!");
                return log;
            }
            
            var atk = User.stats.Atk * User.MultiplyBuffs("Attack");
            var def = enemy.stats.Def * enemy.MultiplyBuffs("Defense");
            uint damage = 1;
            if (def < atk)
            {
                damage = (uint) ((atk - def)*enemy.defensiveMult/2 + (uint)Global.random.Next(1, 4));
            }
            if (Global.random.Next(0, 8) == 0)
            {
                log.Add("Critical!!");
                damage = (uint)(damage*1.25 + Global.random.Next(5,15));    
            }
            if (damage == 0) damage = 1;
            log.AddRange(enemy.DealDamage(damage));
            if (User is PlayerFighter)
            {
                ((PlayerFighter)User).avatar.dealtDmg(damage);
                if (!enemy.IsAlive()) ((PlayerFighter)User).avatar.killedByHand();
            }
            return log;
        }
    }
}
