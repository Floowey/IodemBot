using System.Collections.Generic;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ChancetoOhkoEffect : Effect
    {
        public ChancetoOhkoEffect()
        {
            ActivationTime = TimeToActivate.BeforeDamage;
        }

        public int Probability { get; set; } = 0;

        public override string Type => "OHKO";

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            var log = new List<string>();
            if (target.IsImmuneToOhko)
            {
                return log;
            }

            if (target.Party.Count > 1 && Global.RandomNumber(0, 100) <= Probability)
            {
                target.Kill();
                log.Add($":x: {target.Name}'s life was taken.");
            }
            return log;
        }

        public override string ToString()
        {
            return $"{(Probability != 100 ? $"{Probability}% chance to eliminate" : "Eliminate")} target";
        }
    }
}