using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using IodemBot.ColossoBattles;
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
    public abstract class Move : ICloneable, IEquatable<Move>
    {
        public virtual string Name { get; set; } = "No Name";
        public virtual string Emote { get; set; } = "😶";
        public virtual TargetType TargetType { get; set; } = TargetType.PartySelf;
        public virtual List<Effect> Effects { get; set; } = new List<Effect>();
        public virtual int TargetNr { get; set; } = 0;
        public virtual uint Range { get; set; } = 1;
        public virtual bool HasPriority { get; set; } = false;

        [JsonIgnore]
        public bool OnEnemy
        {
            get
            {
                return new TargetType[] { TargetType.EnemyAll, TargetType.EnemyRange }.Contains(TargetType);
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
                case TargetType.PartySelf:
                    targets.Add(user);
                    break;

                case TargetType.PartySingle:
                    TargetNr = Math.Min(TargetNr, playerCount);
                    targets.Add(user.Party[TargetNr]);
                    break;

                case TargetType.PartyAll:
                    TargetNr = Math.Min(TargetNr, playerCount);
                    targets.AddRange(user.battle.GetTeam(user.party));
                    break;

                case TargetType.EnemyRange:
                    TargetNr = Math.Min(TargetNr, enemyCount);
                    var targetTeam = user.battle.GetTeam(user.enemies);
                    for (int i = -(int)Range + 1; i <= Range - 1; i++)
                    {
                        if (TargetNr + i >= 0 && TargetNr + i < targetTeam.Count)
                        {
                            targets.Add(targetTeam[TargetNr + i]);
                        }
                    }
                    break;

                case TargetType.EnemyAll:
                    targets.AddRange(user.Enemies);
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

            if (User.HasCondition(Condition.ItemCurse) && !User.IsImmuneToItemCurse && Global.RandomNumber(0, 3) == 0)
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

        public bool Equals([AllowNull] Move other)
        {
            if (other == null)
            {
                return false;
            }

            return Name == other.Name && Emote == other.Emote;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Move);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Emote);
        }
    }
}