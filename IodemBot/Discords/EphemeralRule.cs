using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Discords
{
   public enum EphemeralRule
    {
        EphemeralOrFail,
        EphemeralOrFallback,
        Permanent
    }
    public static class EphemeralRuleExtensions
    {
        public static bool ToEphemeral(this EphemeralRule action)
        {
            return action != EphemeralRule.Permanent;
        }
    }
}
