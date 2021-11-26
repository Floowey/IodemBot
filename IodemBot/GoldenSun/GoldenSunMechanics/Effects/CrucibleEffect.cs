using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class CrucibleEffect : Effect
    {
        public override string Type => "Crucible";

        public CrucibleEffect()
        {
            ActivationTime = TimeToActivate.BeforeDamage;
        }

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            List<string> log = new();
            if (user.SelectedMove.TargetType != TargetType.PartySelf)
            {
                log.Add("Crucible must be on a move with TargetType PartySelf");
                Console.WriteLine("Crucible must be on a move with TargetType PartySelf");
                return log;
            }

            var enemyDjinn = user.Enemies.SelectMany(m => m.Moves).OfType<Djinn>().Where(d => d.State == DjinnState.Standby);
            var summonList = user.Enemies.SelectMany(m => m.Moves).OfType<Summon>()
                .Where(s => s.CanSummon(enemyDjinn))
                .ToList();
            summonList.Shuffle();
            var summon = summonList.OrderByDescending(m => m.VenusNeeded + m.MarsNeeded + m.JupiterNeeded + m.MercuryNeeded).FirstOrDefault();

            if (summon is null)
            {
                log.Add($"{user.SelectedMove.Name} fails to summon anything.");
                return log;
            }

            if (summon.Move.ValidSelection(user))
            {
                enemyDjinn.OfElement(Element.Venus).Take(summon.VenusNeeded).ToList().ForEach(d => d.Summon(user));
                enemyDjinn.OfElement(Element.Mars).Take(summon.MarsNeeded).ToList().ForEach(d => d.Summon(user));
                enemyDjinn.OfElement(Element.Jupiter).Take(summon.JupiterNeeded).ToList().ForEach(d => d.Summon(user));
                enemyDjinn.OfElement(Element.Mercury).Take(summon.MercuryNeeded).ToList().ForEach(d => d.Summon(user));
                log.AddRange(summon.Move.Use(user));

                if (summon.EffectsOnUser != null) log.AddRange(summon.EffectsOnUser.ApplyAll(user, user));
                if (summon.EffectsOnParty != null)
                    user.Battle.GetTeam(user.party).ForEach(p => log.AddRange(summon.EffectsOnParty.ApplyAll(user, p)));
            }
            return log;
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            var enemyDjinn = user.Enemies.SelectMany(m => m.Moves).OfType<Djinn>().Where(d => d.State == DjinnState.Standby);
            var summonList = user.Enemies.SelectMany(m => m.Moves).OfType<Summon>()
                .Where(s => s.CanSummon(enemyDjinn))
                .ToList();
            return summonList.Any();
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            return Global.RandomNumber(0, targets.First().Enemies.Count);
        }
    }
}