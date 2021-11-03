using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using IodemBot.ColossoBattles;
using JsonSubTypes;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    [JsonConverter(typeof(JsonSubtypes)),
     JsonSubtypes.KnownSubTypeWithProperty(typeof(OffensivePsynergy), "Power"),
     JsonSubtypes.KnownSubTypeWithProperty(typeof(OffensivePsynergy), "AddDamage"),
     JsonSubtypes.KnownSubTypeWithProperty(typeof(OffensivePsynergy), "DmgMult"),
     JsonSubtypes.KnownSubTypeWithProperty(typeof(OffensivePsynergy), "PercentageDamage"),
     JsonSubtypes.KnownSubTypeWithProperty(typeof(HealPsynergy), "HealPower"),
     JsonSubtypes.KnownSubTypeWithProperty(typeof(HealPsynergy), "Percentage"),
     JsonSubtypes.FallBackSubType(typeof(StatusPsynergy))]
    public abstract class Move : ICloneable, IEquatable<Move>
    {
        public virtual string Name { get; set; } = "No Name";
        public virtual string Emote { get; set; } = "😶";
        public virtual TargetType TargetType { get; set; } = TargetType.PartySelf;
        public virtual List<Effect> Effects { get; set; } = new();
        public virtual int TargetNr { get; set; }
        public virtual uint Range { get; set; } = 1;
        public virtual bool HasPriority { get; set; } = false;

        [JsonIgnore]
        public bool OnEnemy
        {
            get { return new[] { TargetType.EnemyAll, TargetType.EnemyRange }.Contains(TargetType); }
        }

        public abstract object Clone();

        public bool Equals([AllowNull] Move other)
        {
            if (other == null)
            {
                return false;
            }

            return Name == other.Name && Emote == other.Emote;
        }

        public List<string> Use(ColossoFighter user)
        {
            var log = new List<string>();
            var t = Validate(user);
            log.AddRange(t.Log);
            if (!t.IsValid)
            {
                return log;
            }

            try
            {
                log.AddRange(InternalUse(user));
            }
            catch (Exception e)
            {
                Console.WriteLine($"{Name} from {user.Name} has raised an error:\n" + e);
            }

            return log;
        }

        public List<ColossoFighter> GetTarget(ColossoFighter user)
        {
            var targets = new List<ColossoFighter>();
            var playerCount = user.Battle.GetTeam(user.party).Count - 1;
            var enemyCount = user.Battle.GetTeam(user.enemies).Count - 1;

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
                    targets.AddRange(user.Battle.GetTeam(user.party));
                    break;

                case TargetType.EnemyRange:
                    TargetNr = Math.Min(TargetNr, enemyCount);
                    var targetTeam = user.Battle.GetTeam(user.enemies);
                    for (var i = -(int)Range + 1; i <= Range - 1; i++)
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

        protected abstract List<string> InternalUse(ColossoFighter user);

        protected virtual Validation Validate(ColossoFighter user)
        {
            var log = new List<string>();
            if (!user.IsAlive)
            {
                return new Validation(false, log);
            }

            if (user.HasCondition(Condition.Stun))
            {
                log.Add($"{user.Name} can't move.");
                return new Validation(false, log);
            }

            if (user.HasCondition(Condition.Sleep))
            {
                log.Add($"{user.Name} is asleep!");
                return new Validation(false, log);
            }

            if (user.HasCondition(Condition.Flinch))
            {
                log.Add($"{user.Name} can't move.");
                user.RemoveCondition(Condition.Flinch);
                return new Validation(false, log);
            }

            if (user.HasCondition(Condition.ItemCurse) && !user.IsImmuneToItemCurse && Global.RandomNumber(0, 3) == 0)
            {
                log.Add($"{user.Name} can't move.");
                return new Validation(false, log);
            }

            return new Validation(true, log);
        }

        public override string ToString()
        {
            return Name;
        }

        public abstract bool InternalValidSelection(ColossoFighter user);

        public abstract void InternalChooseBestTarget(ColossoFighter user);

        internal bool ValidSelection(ColossoFighter user)
        {
            return InternalValidSelection(user);
        }

        internal void ChooseBestTarget(ColossoFighter user)
        {
            InternalChooseBestTarget(user);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Move);
        }

        public class Validation
        {
            public Validation(bool isValid, List<string> log)
            {
                IsValid = isValid;
                Log = log;
            }

            public bool IsValid { get; set; }
            public List<string> Log { get; set; }
        }
    }
}