using System;
using System.Collections.Generic;
using System.Text;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class DealDamageEffect : Effect
    {
        public override string Type => "DealDamage";
        public uint Power { get; set; } = 0;
        public uint AddDamage { get; set; } = 0;
        public double DmgMult { get; set; } = 1;
        public uint PercentageDamage { get; set; } = 0;
        public Element Element { get; set; }
        private bool AttackBased { get { return Power == 0; } }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            var log = new List<string>();
            if (!Target.IsAlive)
            {
                return log;
            }

            if (Target.HasCondition(Condition.Key) || Target.HasCondition(Condition.Trap) || Target.HasCondition(Condition.Decoy))
            {
                log.Add($"A magical barrier is protecting {Target.Name}.");
                return log;
            }

            if (!Target.IsAlive)
            {
                return log;
            }

            var baseDmg = Global.RandomNumber(0, 4);
            var dmg = AttackBased ?
                Math.Max(0,
                (User.Stats.Atk * User.MultiplyBuffs("Attack") - Target.Stats.Def * Target.ignoreDefense * Target.MultiplyBuffs("Defense")) / 2)
                : (int)Power;

            var elMult = 1 + (User.ElStats.GetPower(Element) * User.MultiplyBuffs("Power") - Target.ElStats.GetRes(Element) * Target.MultiplyBuffs("Resistance")) / (AttackBased ? 400 : 200);
            elMult = Math.Max(0.5, elMult);
            elMult = Math.Max(0, elMult);
            var prctdmg = (uint)(Target.Stats.MaxHP * PercentageDamage / 100);
            var realDmg = (uint)((baseDmg + dmg + AddDamage) * DmgMult * elMult * Target.defensiveMult * User.offensiveMult + prctdmg);
            var punctuation = "!";

            if (Target.ElStats.GetRes(Element) == Target.ElStats.HighestRes())
            {
                punctuation = ".";
            }

            if (Target.ElStats.GetRes(Element) == Target.ElStats.LeastRes())
            {
                punctuation = "!!!";
                if (User is PlayerFighter k)
                {
                    k.battleStats.AttackedWeakness++;
                }
            }

            realDmg = Math.Max(1, realDmg);

            log.AddRange(Target.DealDamage(realDmg, punctuation));
            User.damageDoneThisTurn += realDmg;

            if (User is PlayerFighter p)
            {
                p.battleStats.DamageDealt += realDmg;
                if (!Target.IsAlive)
                {
                    
                    p.battleStats.Kills++;
                }
            }

            if (Target.IsAlive && Target.HasCondition(Condition.Counter))
            {
                var counterAtk = Target.Stats.Atk * Target.MultiplyBuffs("Attack");
                var counterDef = User.Stats.Def * User.MultiplyBuffs("Defense") * User.ignoreDefense;
                uint CounterDamage = (uint)Global.RandomNumber(0, 4);
                if (counterDef < counterAtk)
                {
                    CounterDamage += (uint)((counterAtk - counterDef) * User.defensiveMult / 2);
                }
                log.Add($"{Target.Name} strikes back!");
                log.AddRange(User.DealDamage(CounterDamage));
            }

            return log;
        }
    }
}
