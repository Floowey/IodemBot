using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class AttackWithTeammateEffect : Effect
    {
        public AttackWithTeammateEffect()
        {
            ActivationTime = TimeToActivate.BeforeDamage;
        }

        public override string Type => "AttackWithTeammate";
        public int TeamMates { get; set; } = 1;

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            var log = new List<string>();
            switch (user.Party.Count(s => s.IsAlive && !s.Equals(user)))
            {
                case 0:
                    user.OffensiveMult *= 1.25;
                    log.Add($"{user.Name} charges alone.");
                    break;

                default:
                    var teamMate = user.Party.Where(s => s.IsAlive && !s.Equals(user))
                        .OrderByDescending(p => p.Stats.Atk).Take(TeamMates).ToList();
                    teamMate.ForEach(m =>
                    {
                        user.AddDamage += (uint)(m.Stats.Atk * m.MultiplyBuffs("Attack") / (TeamMates + 1));
                        log.Add($"{m.Name} assists the attack.");
                    });
                    break;
            }

            return log;
        }

        public override string ToString()
        {
            return $"Attack alongside {TeamMates} friend{(TeamMates > 1 ? "s" : "")}";
        }
    }
}