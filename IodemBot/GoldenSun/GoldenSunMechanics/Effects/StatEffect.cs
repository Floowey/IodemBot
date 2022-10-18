using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class StatEffect : Effect
    {
        public override string Type => "Stat";
        [JsonProperty] private string Stat { get; set; } = "OH NO";
        [JsonProperty] private double Multiplier { get; set; } = 1;
        [JsonProperty] private int Probability { get; set; } = 100;
        [JsonProperty] private int Turns { get; set; } = 7;

        public override string ToString()
        {
            return $"{(Probability == 100 ? $"{(Multiplier > 1 ? "Raise" : "Lower")}" : $"{Probability}% chance to {(Multiplier > 1 ? "raise" : "lower")}")} {Stat} of {(OnTarget ? "target" : "user")} to {(Multiplier * 100):0.##}%";
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            Dictionary<string, string> redirect = new()
            {
                { "Attack", "Atk" },
                { "Defense", "Def" },
                { "Speed", "Spd" },
                { "Power", "MaxPP" },
                { "Resistance", "MaxHP" }
            };
            if (!redirect.ContainsKey(Stat))
                return base.InternalChooseBestTarget(targets);

            if (targets.Any(t => t.Tags.Contains("VIP")))
                return ChooseAliveVIPTarget(targets);

            var aliveTargets = targets.Where(t => t.IsAlive);
            return targets.IndexOf(aliveTargets.OrderByDescending(t => ((double)(int)t.Stats.GetType().GetProperty(redirect[Stat]).GetValue(t.Stats)) / t.MultiplyBuffs(Stat)).FirstOrDefault() ?? targets.First());
        }

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            List<string> log = new();
            ColossoFighter targetted = OnTarget ? target : user;

            if (!targetted.IsAlive)
            {
                return log;
            }

            if (Stat == "OH NO")
            {
                Console.WriteLine(string.Join(", ", user.Moves.Select(s => s.Name)));
            }

            if (Global.RandomNumber(0, 100) <= Probability)
            {
                targetted.ApplyBuff(new Buff(Stat, Multiplier, (uint)Turns));
                log.Add($"{targetted.Name}'s {Stat} {(Multiplier > 1 ? "rises" : "lowers")}.");
            }

            return log;
        }
    }
}