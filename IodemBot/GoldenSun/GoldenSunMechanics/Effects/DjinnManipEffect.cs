﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class DjinnManipEffect : Effect
    {
        public override string Type => "DjinnManip";
        [JsonProperty] private int nDjinn { get; set; } = 1;
        [JsonProperty] private DjinnState FromState { get; set; } = DjinnState.Set;
        [JsonProperty] private DjinnState ToState { get; set; } = DjinnState.Standby;

        public DjinnManipEffect()
        {
        }

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            List<string> log = new();
            if (!target.IsAlive)
                return log;
            var validDjinn = target.Moves.OfType<Djinn>().Where(d => d.State == FromState).ToList();
            validDjinn.Shuffle();
            var djinnSelected = validDjinn.Take(nDjinn);

            foreach (var djinn in djinnSelected)
            {
                switch (ToState)
                {
                    case DjinnState.Set:
                        djinn.OnStandby = false;
                        target.Moves.OfType<Djinn>().Where(d => d.CoolDown > djinn.CoolDown).ToList().ForEach(d => d.CoolDown--);
                        djinn.CoolDown = 0;
                        log.Add($"{djinn.Name} is set to {target.Name}");
                        break;

                    case DjinnState.Standby:
                        djinn.OnStandby = true;
                        djinn.CoolDown = Math.Max(2, target.Moves.OfType<Djinn>().Max(d => d.CoolDown) + 1);
                        djinn.Position = target.Party.SelectMany(p => p.Moves.OfType<Djinn>()).Max(d => d.Position) + 1;
                        log.Add($"{target.Name}'s {djinn.Name} was set to standby.");
                        break;

                    case DjinnState.Recovery:
                        djinn.OnStandby = false;
                        djinn.CoolDown = FromState == DjinnState.Set ? Math.Max(2, target.Moves.OfType<Djinn>().Max(d => d.CoolDown) + 1) : djinn.CoolDown;
                        djinn.Position = target.Party.SelectMany(p => p.Moves.OfType<Djinn>()).Max(d => d.Position) + 1;
                        log.Add($"{target.Name}'s {djinn.Name} was drained ");
                        break;
                }
            }

            return log;
        }
    }
}