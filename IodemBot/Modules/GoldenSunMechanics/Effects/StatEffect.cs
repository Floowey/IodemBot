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
        double Multiplier;
        int probability = 100;
        bool OnTarget;
        uint Turns;

        public StatEffect(string StatToBoost, double Value,int probability = 100, bool OnTarget = true, uint Duration = 5)
        {
            Init(StatToBoost, Value, probability, OnTarget, Duration);
        }

        public StatEffect(params object[] args)
        {
            if (args.Length == 2 && args[0] is string && args[1] is double)
            {
                Init((string)args[0], (double)args[1]);
            }
            else if (args.Length == 3 && args[0] is string && args[1] is double
              && args[2] is int)
            {
                Init((string)args[0], (double)args[1], (int)args[2]);
            }
            else if (args.Length == 4 && args[0] is string && args[1] is double
               && args[2] is int && args[3] is bool)
            {
                Init((string)args[0], (double)args[1], (int)args[2], (bool)args[3]);
            }
            else if (args.Length == 5 && args[0] is string && args[1] is double
               && args[2] is int && args[3] is bool && args[4] is uint)
            {
                Init((string)args[0], (double)args[1], (int)args[2], (bool)args[3], (uint)args[4]);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private void Init(string StatToBoost, double Value, int probability = 100, bool OnTarget = true, uint Duration = 5)
        {
            this.StatToBoost = StatToBoost;
            this.Multiplier = Value;
            this.OnTarget = OnTarget;
            this.Turns = Duration;
        }


        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            if (!Target.IsAlive()) return log;
            if (OnTarget)
            {
                Target.applyBuff(new Buff(StatToBoost, Multiplier, Turns));
                log.Add($"{Target.name}'s {StatToBoost} {(Multiplier > 1 ? "rises" : "lowers")}.");
            } else
            {
                User.applyBuff(new Buff(StatToBoost, Multiplier, Turns));
                log.Add($"{User.name}'s {StatToBoost} {(Multiplier > 1 ? "rises" : "lowers")}.");
            }

            return new List<string>();//Add actual text from StatusPsnyergy
        }
    }
}
