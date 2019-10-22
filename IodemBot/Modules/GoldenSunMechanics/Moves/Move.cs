using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public enum Target { self, ownSingle, ownAll, otherSingle, otherRange, otherAll }

    public abstract class Move : ICloneable
    {
        public string Name { get; set; } = "No Name";
        public string Emote { get; set; } = "😶";
        public Target TargetType { get; set; } = Target.self;
        public List<Effect> Effects { get; set; } = new List<Effect>();
        public int TargetNr { get; set; } = 0;
        public uint Range { get; set; } = 0;
        public bool HasPriority { get; set; } = false;

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
                Console.WriteLine("What!?" + e.Message);
            }
            return log;
        }

        [JsonIgnore]
        public bool OnEnemy
        {
            get
            {
                return new Target[] { Target.otherSingle, Target.otherAll, Target.otherRange }.Contains(TargetType);
            }
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
                log.Add($"{User.Name} can't move");
                return new Validation(false, log);
            }

            if (User.HasCondition(Condition.Sleep))
            {
                log.Add($"{User.Name} is asleep!");
                return new Validation(false, log);
            }

            if (User.HasCondition(Condition.Flinch))
            {
                log.Add($"{User.Name} can't move");
                User.RemoveCondition(Condition.Flinch);
                return new Validation(false, log);
            }

            if (User.HasCondition(Condition.ItemCurse) && !User.IsImmuneToItemCurse && Global.Random.Next(0, 3) == 0)
            {
                log.Add($"{User.Name} can't move");
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