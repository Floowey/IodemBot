using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class LibraEffect : Effect
    {
        public override string Type => "Libra";
        private int notBalanced = 0;
        public LibraEffect()
        {
            ActivationTime = TimeToActivate.BeforeDamage;
        }
        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            List<string> log = new();
            if (user.Party.Count(a => !a.IsAlive) % 2 == 1)
            {
                notBalanced++;
                log.Add("The scale is inclined.");
            }
            else
            {
                log.Add("The scale is balanced.");
            }
            user.AddDamage += (uint)(25 * notBalanced);
            return log;
        }
    }
}