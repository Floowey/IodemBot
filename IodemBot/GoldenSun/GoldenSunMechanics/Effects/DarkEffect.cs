using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class DarkEffect : Effect
    {
        public override string Type => "Dark";

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            List<string> log = new();

            if (!user.SelectedMove.Effects.Any(e => e.Type == Type))
            {
                log.Add("Something went wrong.");
                return log;
            }
            if (user.SelectedMove is Psynergy psy)
            {
                psy.Element = new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury }.OrderBy(e => target.ElStats.GetRes(e)).First();
            }
            return log;
        }
    }
}