using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColossoTest.Colosso
{
    public class Defend : Move
    {
        public Defend() : base("Defend", "🛡", Target.self, 1)
        {
        }

        public override List<string> Use(ColossoFighter User)
        {
            User.buffs.Add(new Buff("Def", 4, 1));
            return new List<string>();
        }
    }
}
