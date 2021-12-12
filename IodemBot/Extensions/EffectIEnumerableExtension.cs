using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Extensions
{
    public static class EffectIEnumerableExtension
    {
        public static IEnumerable<string> ApplyAll(this IEnumerable<Effect> effects, ColossoFighter user, ColossoFighter target)
        {
            return effects.SelectMany(e => e.Apply(user, target));
        }
    }
}