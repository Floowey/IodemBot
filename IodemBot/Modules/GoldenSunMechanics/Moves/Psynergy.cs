using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public abstract class Psynergy : Move
    {
        public uint PPCost;
        public Element element;

        public enum Element { Venus, Mars, Jupiter, Mercury, none };


        protected Psynergy(string name, string emote, Target targetType, uint range, Element element, uint PPCost) : base(name, emote, targetType, range)
        {
            this.element = element;
            this.PPCost = PPCost;
        }

        protected override Validation Validate(ColossoFighter User)
        {
            List<string> log = new List<string>();
            var t = base.Validate(User);
            if (!t.isValid) return t;

            log.AddRange(t.log);

            //Psy Seal:
            if (User.HasCondition(Condition.Seal))
            {
                log.Add($"{User.name}'s Psynergy is sealed!");
                return new Validation(false, log);
            }

            if (User.stats.PP < PPCost)
            {
                log.Add($"{User.name} has not enough PP to cast {this.name}.");
                return new Validation(false, log);
            }
            User.stats.PP -= PPCost;
            log.Add($"{emote} {User.name} casts {this.name}!");
            return new Validation(true, log);
        }
    }
}   
