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
            switch (User.GetTeam().Where(s => s.IsAlive && !s.Equals(User)).Count())
            {
                case 0:
                    User.offensiveMult *= 1.25;
                    log.Add($"{User.Name} charges alone.");
                    break;

                default:
                    var teamMate = User.GetTeam().Where(s => s.IsAlive && !s.Equals(User)).OrderByDescending(p => p.Stats.Atk).FirstOrDefault();
                    User.addDamage += (uint)(teamMate.Stats.Atk * teamMate.MultiplyBuffs("Attack") * 0.75);
                    log.Add($"{teamMate.Name} assists the attack.");
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