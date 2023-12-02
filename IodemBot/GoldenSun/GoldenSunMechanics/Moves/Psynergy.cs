using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public abstract class Psynergy : Move
    {
        public uint PpCost { get; set; }
        public Element Element { get; set; }

        public override bool InternalValidSelection(ColossoFighter user)
        {
            return user.Stats.PP >= PpCost && !(PpCost > 1 && user.HasCondition(Condition.Seal));
        }

        protected override Validation Validate(ColossoFighter user)
        {
            var log = new List<string>();
            var t = base.Validate(user);
            if (!t.IsValid) return t;

            log.AddRange(t.Log);

            //Psy Seal:
            //PPCost > 1 is, because Items are right now implemented as Psynergy with PPCost 1
            if (PpCost > 1 && user.HasCondition(Condition.Seal))
            {
                log.Add($"{user.Name}'s Psynergy is sealed!");
                return new Validation(false, log);
            }

            if (user.Stats.PP < PpCost)
            {
                log.Add($"{user.Name} has not enough PP to cast {Name}.");
                return new Validation(false, log);
            }

            var targets = GetTarget(user);
            if (!Effects.Any(i => i is ReviveEffect || i is MysticCallEffect) && targets.TrueForAll(i => !i.IsAlive))
            {
                log.Add(
                    $"{user.Name} wants to {(PpCost == 1 ? "use" : "cast")} {Name}, but {(targets.Count == 1 ? "the target is" : "all the targets are")} down.");
                if (user.Moves.FirstOrDefault(m => m is Defend) != null)
                    log.AddRange(user.Moves.FirstOrDefault(m => m is Defend).Use(user));
                return new Validation(false, log);
            }

            user.Stats.PP -= (int)PpCost;
            
            if(user is PlayerFighter p){
                p.BattleStats.PPUsed += PpCost;
            }

            log.Add($"{Emote} {user.Name} {(PpCost == 1 ? "uses" : "casts")} {Name}!");
            return new Validation(true, log);
        }
    }
}