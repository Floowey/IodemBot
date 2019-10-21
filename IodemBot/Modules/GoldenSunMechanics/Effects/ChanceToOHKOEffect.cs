using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ChancetoOHKOEffect : IEffect
    {
        private readonly int Probability = 0;

        public override string Type { get; } = "OHKO";

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            var log = new List<string>();
            if (Target.IsImmuneToOHKO)
            {
                return log;
            }

            if (Target.GetTeam().Count > 1 && Global.Random.Next(1, 100) <= Probability)
            {
                Target.Kill();
                log.Add($":x: {Target.Name}'s life was taken.");
            }
            return log;
        }

        public override string ToString()
        {
            return $"{(Probability != 100 ? $"{Probability}% chance to eliminate" : "Eliminate")} target";
        }
    }
}