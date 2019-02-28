using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics 
{
    public enum Target { self, ownSingle, ownAll, otherSingle, otherRange, otherAll}
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
            if (!t.isValid) return log;

            log.AddRange(InternalUse(User));

            //Haunt Damage
            if (User.HasCondition(Condition.Haunt))
            {
                log.AddRange(User.DealDamage((uint)(User.stats.HP * Global.random.Next(20, 40) / 100)));
            }

            return log;
        }

        protected abstract List<string> InternalUse(ColossoFighter User);

        protected virtual Validation Validate(ColossoFighter User)
        {
            List<string> log = new List<string>();
            if (!User.IsAlive()) return new Validation(false, log);
            
            if (User.HasCondition(Condition.Stun)) {
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

            if (User.HasCondition(Condition.ItemCurse) && Global.random.Next(0,3) == 0)
            {
                log.Add($"{User.name} can't move");
                return new Validation(false, log);
            }


            return new Validation(true, log);
        }

        public class Validation{
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
            if(effectImages != null)
                effectImages.ForEach(e => effects.Add(IEffect.EffectFactory(e.id, e.args)));
        }

        public List<ColossoFighter> getTarget(ColossoFighter user)
        {
            List<ColossoFighter> targets = new List<ColossoFighter>();
            switch (targetType)
            {
                case Target.self:
                    targets.Add(user);
                    break;
                case Target.ownAll:
                    targets.AddRange(user.battle.getTeam(user.party));
                    break;
                case Target.ownSingle:
                    targets.Add(user.battle.getTeam(user.party)[targetNr]);
                    break;
                case Target.otherAll:
                    targets.AddRange(user.battle.getTeam(user.enemies));
                    break;
                case Target.otherSingle:
                    targets.Add(user.battle.getTeam(user.enemies)[targetNr]);
                    break;
                case Target.otherRange:
                    var targetTeam = user.battle.getTeam(user.enemies);
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
    }
}
