using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class Break : Psynergy
    {
        public Break() : base("Break", "<:Break:536969993490006036>", Target.otherAll, 4, Element.Mercury, 5)
        {
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override List<string> Use(ColossoFighter User)
        {
            List<string> Log = new List<string>();
            var res = PPCheck(User);
            Log.AddRange(res.Item2);
            if (!res.Item1) return Log;

            List<ColossoFighter> targets = getTarget(User);

            Log.Add($"{emote} {User.name} casts {this.name}.");
            foreach (var t in targets)
            {
                var newBuffs = new List<Buff>();
                t.Buffs.ForEach(b => {
                    b.turns -= 1;
                    if (b.multiplier >= 1)
                    {
                        newBuffs.Add(b);
                    }
                    else
                    {
                        Log.Add($"{name}'s {b.stat} normalizes.");
                    }
                });
                t.Buffs = newBuffs;
            }

            return Log;
        }
    }
}
