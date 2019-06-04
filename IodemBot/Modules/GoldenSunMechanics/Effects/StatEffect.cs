using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class StatEffect : IEffect
    {
        private string StatToBoost;
        private double Multiplier;
        private readonly int probability = 100;
        private bool OnTarget = true;
        private int Turns = 7;

        public StatEffect(string StatToBoost, double Value, long probability = 100, bool OnTarget = true, int Duration = 5)
        {
            Init(StatToBoost, Value, probability, OnTarget, Duration);
        }

        public StatEffect(params string[] args)
        {
            CultureInfo[] cultures = { new CultureInfo("en-US"),
                                 new CultureInfo("fr-FR"),
                                 new CultureInfo("it-IT"),
                                 new CultureInfo("de-DE") };
            switch (args.Length)
            {
                case 5: int.TryParse(args[4], out Turns); goto case 4;
                case 4: bool.TryParse(args[3], out OnTarget); goto case 3;
                case 3: int.TryParse(args[2], out probability); goto case 2;
                case 2:
                    double.TryParse(args[1], NumberStyles.Number, new CultureInfo("en-GB"), out Multiplier);

                    StatToBoost = args[0];
                    break;

                default: throw new ArgumentException("Stat Effects take 2-5 string arguments");
            }
        }

        private void Init(string StatToBoost, double Value, long probability = 100, bool OnTarget = true, int Duration = 5)
        {
            this.StatToBoost = StatToBoost;
            this.Multiplier = Value;
            this.OnTarget = OnTarget;
            this.Turns = Duration;
        }

        public override string ToString()
        {
            return $"{(probability == 100 ? $"{(Multiplier > 1 ? "Raise" : "Lower")}" : $"{probability}% chance to {(Multiplier > 1 ? "raise" : "lower")}")} {StatToBoost} of {(OnTarget ? "target" : "user")} by {Multiplier}";
        }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            if (!Target.IsAlive())
            {
                return log;
            }

            if (Global.Random.Next(1, 100) <= probability)
            {
                if (OnTarget)
                {
                    Target.ApplyBuff(new Buff(StatToBoost, Multiplier, (uint)Turns));
                    log.Add($"{Target.name}'s {StatToBoost} {(Multiplier > 1 ? "rises" : "lowers")}.");
                }
                else
                {
                    User.ApplyBuff(new Buff(StatToBoost, Multiplier, (uint)Turns));
                    log.Add($"{User.name}'s {StatToBoost} {(Multiplier > 1 ? "rises" : "lowers")}.");
                }
            }

            return log;
        }
    }
}