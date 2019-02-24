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

        public Tuple<bool, List<string>> PPCheck(ColossoFighter User)
        {
            List<string> log = new List<string>();
            if (User.stats.PP < PPCost)
            {
                log.Add($"{User.name} has not enough PP to cast {this.name}.");
                return new Tuple<bool, List<string>>(false, log);
            }
            User.stats.PP -= PPCost;
            return new Tuple<bool, List<string>>(true, log);
        }
    }
}   
