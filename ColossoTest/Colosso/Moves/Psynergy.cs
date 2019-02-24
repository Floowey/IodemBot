using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColossoTest.Colosso
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
    }
}
