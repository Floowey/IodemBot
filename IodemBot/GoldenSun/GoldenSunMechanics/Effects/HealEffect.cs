using System;
using System.Collections.Generic;
using System.Text;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class HealEffect : Effect
    {
        public override string Type => "Heal";
        public int Percentage { get; set; }
        public int HealPower { get; set; }
        public int PPHeal { get; set; }
        public int PPPercent { get; set; }
        public Element Element { get; set; }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            var log = new List<string>(); 
            int Power = (int)(User.ElStats.GetPower(Element) * User.MultiplyBuffs("Power"));
            var HPtoHeal = (uint)(HealPower * Power / 100 + Target.Stats.MaxHP * Percentage / 100);
            if (HPtoHeal > 0)
            {
                log.AddRange(Target.Heal(HPtoHeal));
            }

            var PPToHeal = (uint)(PPHeal * Power / 100 + Target.Stats.MaxPP * PPPercent / 100);
            if (PPToHeal > 0)
            {
                log.AddRange(Target.RestorePP(PPToHeal));
            }

            if (User is PlayerFighter p)
            {
                p.battleStats.HPhealed += HPtoHeal;
            }
            return log;
        }
    }
}
