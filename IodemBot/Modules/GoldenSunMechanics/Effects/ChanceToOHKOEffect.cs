using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class ChancetoOHKOEffect : IEffect
    {
        int Probability = 0;
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            var log = new List<string>();
            if (Target.isImmuneToEffects) return log;
            if (Global.random.Next(1, 100) <= Probability)
            {
                Target.Kill();
                log.Add($"{Target.name}'s life was taken.");
            }
            return log;
        }
        public ChancetoOHKOEffect(string[] args)
        {
            timeToActivate = TimeToActivate.beforeDamge;
           
            if (args.Length == 1)
                int.TryParse(args[0], out Probability);
        }

        public override string ToString()
        {
            return $"{(Probability != 100 ? $"{Probability}% chance to eliminate" : "Eliminate")} target.";
        }
    }
}
