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
        public string name;
        public string emote;
        public Target targetType;
        [JsonIgnore] public List<IEffect> effects;
        public List<EffectImage> effectImages;
        public int targetNr;
        public uint range;
        public bool hasPriority = false;

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
                return new Target[] { Target.otherSingle, Target.otherAll, Target.otherRange }.Contains(targetType);
            }
        }

        protected abstract List<string> InternalUse(ColossoFighter User);

        protected virtual Validation Validate(ColossoFighter User)
        {
            List<string> log = new List<string>();
            if (!User.IsAlive())
            {
                return new Validation(false, log);
            }

            if (User.HasCondition(Condition.Stun))
            {
                log.Add($"{User.name} can't move");
                return new Validation(false, log);
            }

            if (User.HasCondition(Condition.Sleep))
            {
                log.Add($"{User.name} is asleep!");
                return new Validation(false, log);
            }

            if (User.HasCondition(Condition.Flinch))
            {
                log.Add($"{User.name} can't move");
                User.RemoveCondition(Condition.Flinch);
                return new Validation(false, log);
            }

            if (User.HasCondition(Condition.ItemCurse) && !User.IsImmuneToItemCurse && Global.Random.Next(0, 3) == 0)
            {
                log.Add($"{User.name} can't move");
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

        public Move(string name, string emote, Target targetType, uint range, List<EffectImage> effectImages)
        {
            this.name = name;
            this.emote = emote;
            this.targetType = targetType;
            this.range = range;
            this.effects = new List<IEffect>();
            this.effectImages = effectImages;
            if (effectImages != null)
            {
                effectImages.ForEach(e => effects.Add(IEffect.EffectFactory(e.Id, e.Args)));
            }
        }

        public List<ColossoFighter> GetTarget(ColossoFighter user)
        {
            List<ColossoFighter> targets = new List<ColossoFighter>();
            var playerCount = user.battle.GetTeam(user.party).Count - 1;
            var enemyCount = user.battle.GetTeam(user.enemies).Count - 1;

            switch (targetType)
            {
                case Target.self:
                    targets.Add(user);
                    break;

                case Target.ownAll:
                    targetNr = Math.Min(targetNr, playerCount);
                    targets.AddRange(user.battle.GetTeam(user.party));
                    break;

                case Target.ownSingle:
                    targetNr = Math.Min(targetNr, playerCount);
                    targets.Add(user.battle.GetTeam(user.party)[targetNr]);
                    break;

                case Target.otherAll:
                    targets.AddRange(user.GetEnemies());
                    break;

                case Target.otherSingle:
                    targetNr = Math.Min(targetNr, enemyCount);
                    targets.Add(user.battle.GetTeam(user.enemies)[targetNr]);
                    break;

                case Target.otherRange:
                    targetNr = Math.Min(targetNr, enemyCount);
                    var targetTeam = user.battle.GetTeam(user.enemies);
                    for (int i = -(int)range + 1; i <= range - 1; i++)
                    {
                        if (targetNr + i >= 0 && targetNr + i < targetTeam.Count())
                        {
                            targets.Add(targetTeam[targetNr + i]);
                        }
                    }
                    break;
            }
            return targets;
        }

        public override string ToString()
        {
            return name;
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