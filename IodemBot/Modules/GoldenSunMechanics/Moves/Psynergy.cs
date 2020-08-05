using System.Collections.Generic;
using System.Linq;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public abstract class Psynergy : Move
    {
        public uint PPCost { get; set; }
        public Element Element { get; set; }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            return User.Stats.PP >= PPCost && !(PPCost > 1 && User.HasCondition(Condition.Seal));
        }

        protected override Validation Validate(ColossoFighter User)
        {
            List<string> log = new List<string>();
            var t = base.Validate(User);
            if (!t.isValid)
            {
                return t;
            }

            log.AddRange(t.log);

            //Psy Seal:
            //PPCost > 1 is, because Items are right now implemented as Psynergy with PPCost 1
            if (PPCost > 1 && User.HasCondition(Condition.Seal))
            {
                log.Add($"{User.Name}'s Psynergy is sealed!");
                return new Validation(false, log);
            }

            if (User.Stats.PP < PPCost)
            {
                log.Add($"{User.Name} has not enough PP to cast {this.Name}.");
                return new Validation(false, log);
            }
            List<ColossoFighter> targets = GetTarget(User);
            if (!Effects.Any(i => i is ReviveEffect || i is MysticCallEffect) && targets.TrueForAll(i => !i.IsAlive))
            {
                log.Add($"{User.Name} wants to {(PPCost == 1 ? "use" : "cast")} {Name}, but {(targets.Count == 1 ? "the target is" : "all the targets are")} down.");
                if (User.Moves.FirstOrDefault(m => m is Defend) != null)
                {
                    log.AddRange(User.Moves.FirstOrDefault(m => m is Defend).Use(User));
                }
                return new Validation(false, log);
            }

            User.Stats.PP -= (int)PPCost;
            log.Add($"{Emote} {User.Name} {(PPCost == 1 ? "uses" : "casts")} {this.Name}!");
            return new Validation(true, log);
        }
    }
}