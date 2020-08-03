using System.Collections.Generic;
using System.Linq;
using IodemBot.Modules.ColossoBattles;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Extensions
{
    public static class EffectIEnumerableExtension
    {
        public static IEnumerable<string> ApplyAll(this IEnumerable<Effect> effects, ColossoFighter User, ColossoFighter Target)
        {
            return effects.SelectMany(e => e.Apply(User, Target));
        }
    }
}