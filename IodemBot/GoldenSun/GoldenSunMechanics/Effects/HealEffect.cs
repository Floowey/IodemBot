using System.Collections.Generic;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class HealEffect : Effect
    {
        public override string Type => "Heal";
        public int Percentage { get; set; }
        public int HealPower { get; set; }
        public int PpHeal { get; set; }
        public int PpPercent { get; set; }
        public Element Element { get; set; }

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            var log = new List<string>();
            int power = (int)(user.ElStats.GetPower(Element) * user.MultiplyBuffs("Power"));
            var hPtoHeal = (uint)(HealPower * power / 100 + target.Stats.MaxHP * Percentage / 100);
            if (hPtoHeal > 0)
            {
                log.AddRange(target.Heal(hPtoHeal));
            }

            var ppToHeal = (uint)(PpHeal * power / 100 + target.Stats.MaxPP * PpPercent / 100);
            if (ppToHeal > 0)
            {
                log.AddRange(target.RestorePp(ppToHeal));
            }

            if (user is PlayerFighter p)
            {
                p.BattleStats.HPhealed += hPtoHeal;
            }
            return log;
        }
    }
}