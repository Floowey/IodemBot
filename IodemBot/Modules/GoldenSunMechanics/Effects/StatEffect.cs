using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class StatEffect : IEffect
    {
        string StatToBoost;
        double value;
        bool onTarget;
        int duration;

        public StatEffect(string statToBoost, double value)
        {
            StatToBoost = statToBoost;
            this.value = value;
        }

        public StatEffect(string statToBoost, double value, bool onTarget) : this(statToBoost, value)
        {
            this.onTarget = onTarget;
        }

        public StatEffect(string statToBoost, double value, bool onTarget, int duration) : this(statToBoost, value, onTarget)
        {
            this.duration = duration;
        }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            throw new NotImplementedException();
        }
    }
}
