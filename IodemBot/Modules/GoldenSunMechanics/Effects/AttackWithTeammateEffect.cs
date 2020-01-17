using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class AttackWithTeammateEffect : Effect
    {
        public AttackWithTeammateEffect()
        {
            ActivationTime = TimeToActivate.beforeDamge;
        }

        public override string Type { get; } = "AttackWithTeammate";
        public int TeamMates { get; set; } = 1;

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
                    var teamMate = User.GetTeam().Where(s => s.IsAlive && !s.Equals(User)).OrderByDescending(p => p.Stats.Atk).Take(TeamMates).ToList();
                    teamMate.ForEach(m =>
                    {
                        User.addDamage += (uint)(m.Stats.Atk * m.MultiplyBuffs("Attack") * 0.75);
                        log.Add($"{m.Name} assists the attack.");
                    });
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