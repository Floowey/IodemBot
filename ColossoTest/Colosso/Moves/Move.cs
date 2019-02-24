using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColossoTest.Colosso
{
    public enum Target { self, ownSingle, ownAll, otherSingle, otherRange, otherAll}
    public abstract class Move
    {
        public string name;
        public string emote;
        public Target targetType;
        public int targetNr;
        public uint range;
        

        public abstract List<string> Use(ColossoFighter User);

        public Move(string name, string emote, Target targetType, uint range)
        {
            this.name = name;
            this.emote = emote;
            this.targetType = targetType;
            this.range = range;
        }
    }
}
