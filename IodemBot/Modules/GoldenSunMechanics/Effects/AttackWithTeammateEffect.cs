using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class AttackWithTeammateEffect : IEffect
    {
        public AttackWithTeammateEffect()
        {
            timeToActivate = TimeToActivate.beforeDamge;
        }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            var log = new List<string>();
            switch (User.GetTeam().Where(s => s.IsAlive() && !s.Equals(User)).Count())
            {
                case 0:
                    User.offensiveMult *= 1.25;
                    log.Add($"{User.name} charges alone.");
                    break;

                default:
                    var teamMate = User.GetTeam().Where(s => s.IsAlive()).OrderByDescending(p => p.stats.Atk).FirstOrDefault();
                    User.addDamage += (uint)(teamMate.stats.Atk * teamMate.MultiplyBuffs("Attack") / 2);
                    log.Add($"{teamMate.name} assists the attack.");
                    break;
            }
            return log;
        }

        public override string ToString()
        {
            return "Attack alongside your friend";
        }
    }
}