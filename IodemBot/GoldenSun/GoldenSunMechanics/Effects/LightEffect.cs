using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class LightEffect : Effect
    {
        public override string Type => "Light";

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            List<string> log = new();

            var newElement = new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury }.OrderByDescending(e => user.ElStats.GetPower(e)).First();

            if (user.SelectedMove.Effects.Any(e => e.Type == Type))
            {
                if (user.SelectedMove is Psynergy psy)
                {
                    psy.Element = newElement;
                }
            }
            else if (user.SelectedMove is Attack
                && (user?.Weapon?.Unleash?.AllEffects?.Any(e => e.Type == Type) ?? false))
            {
                user.Weapon.Unleash.UnleashAlignment = newElement;
            }
            return log;
        }

        public override string ToString()
        {
            return "Draws out the users strongest element.";
        }
    }
}