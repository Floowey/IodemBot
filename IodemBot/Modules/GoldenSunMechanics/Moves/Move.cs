using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.Modules.ColossoBattles;
using JsonSubTypes;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    [JsonConverter(typeof(JsonSubtypes))]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(OffensivePsynergy), "Power")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(OffensivePsynergy), "AddDamage")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(OffensivePsynergy), "DmgMult")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(OffensivePsynergy), "PercentageDamage")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(HealPsynergy), "HealPower")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(HealPsynergy), "Percentage")]
    [JsonSubtypes.FallBackSubType(typeof(StatusPsynergy))]
    public abstract class Move : ICloneable
    {
        public virtual string Name { get; set; } = "No Name";
        public virtual string Emote { get; set; } = "😶";
        public virtual Target TargetType { get; set; } = Target.self;
        public virtual List<Effect> Effects { get; set; } = new List<Effect>();
        public virtual int TargetNr { get; set; } = 0;
        public virtual uint Range { get; set; } = 1;
        public virtual bool HasPriority { get; set; } = false;

        [JsonIgnore]
        public bool OnEnemy
        {
            get
            {
                return new Target[] { Target.otherSingle, Target.otherAll, Target.otherRange }.Contains(TargetType);
            }
        }

        public List<string> Use(ColossoFighter User)
        {
            List<string> log = new List<string>();
            var t = Validate(User);
            log.AddRange(t.log);
            if (!t.isValid)
            {
                return log;
            }
            try
            {
                log.AddRange(InternalUse(User));
            }
            catch (Exception e)
            {
                Console.WriteLine($"{Name} from {User.Name} has raised an error:\n" + e.ToString());
            }
            return log;
        }

        public List<ColossoFighter> GetTarget(ColossoFighter user)
        {
            List<ColossoFighter> targets = new List<ColossoFighter>();
            var playerCount = user.battle.GetTeam(user.party).Count - 1;
            var enemyCount = user.battle.GetTeam(user.enemies).Count - 1;

            switch (TargetType)
            {
                case Target.self:
                    targets.Add(user);
                    break;

                case Target.ownAll:
                    TargetNr = Math.Min(TargetNr, playerCount);
                    targets.AddRange(user.battle.GetTeam(user.party));
                    break;

                case Target.ownSingle:
                    TargetNr = Math.Min(TargetNr, playerCount);
                    targets.Add(user.battle.GetTeam(user.party)[TargetNr]);
                    break;

                case Target.otherAll:
                    targets.AddRange(user.GetEnemies());
                    break;

                case Target.otherSingle:
                    TargetNr = Math.Min(TargetNr, enemyCount);
                    targets.Add(user.battle.GetTeam(user.enemies)[TargetNr]);
                    break;

                case Target.otherRange:
                    TargetNr = Math.Min(TargetNr, enemyCount);
                    var targetTeam = user.battle.GetTeam(user.enemies);
                    for (int i = -(int)Range + 1; i <= Range - 1; i++)
                    {
                        if (TargetNr + i >= 0 && TargetNr + i < targetTeam.Count())
                        {
                            targets.Add(targetTeam[TargetNr + i]);
                        }
                    }
                    break;
            }
            return targets;
        }

        protected abstract List<string> InternalUse(ColossoFighter User);

        protected virtual Validation Validate(ColossoFighter User)
        {
            List<string> log = new List<string>();
            if (!User.IsAlive)
            {
                return new Validation(false, log);
            }

            if (User.HasCondition(Condition.Stun))
            {
                log.Add($"{User.Name} can't move.");
                return new Validation(false, log);
            }

            if (User.HasCondition(Condition.Sleep))
            {
                log.Add($"{User.Name} is asleep!");
                return new Validation(false, log);
            }

            if (User.HasCondition(Condition.Flinch))
            {
                log.Add($"{User.Name} can't move.");
                User.RemoveCondition(Condition.Flinch);
                return new Validation(false, log);
            }

            if (User.HasCondition(Condition.ItemCurse) && !User.IsImmuneToItemCurse && Global.Random.Next(0, 3) == 0)
            {
                log.Add($"{User.Name} can't move.");
                return new Validation(false, log);
            }

            return new Validation(true, log);
        }

        public class Validation
        {
            public bool isValid;
            public List<string> log;

            public Validation(bool isValid, List<string> log)
            {
                this.isValid = isValid;
                this.log = log;
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public abstract object Clone();

        public abstract bool InternalValidSelection(ColossoFighter User);

        public abstract void InternalChooseBestTarget(ColossoFighter User);

        internal bool ValidSelection(ColossoFighter User)
        {
            return InternalValidSelection(User);
        }

        internal void ChooseBestTarget(ColossoFighter User)
        {
            InternalChooseBestTarget(User);
        }
    }
}