using System;
using System.Collections.Generic;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class DealDamageEffect : Effect
    {
        public override string Type => "DealDamage";
        public uint Power { get; set; } = 0;
        public uint AddDamage { get; set; } = 0;
        public double DmgMult { get; set; } = 1;
        public uint PercentageDamage { get; set; } = 0;
        public Element Element { get; set; }
        private bool AttackBased => Power == 0;

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            var log = new List<string>();
            if (!target.IsAlive)
            {
                return log;
            }

            if (target.HasCondition(Condition.Key) || target.HasCondition(Condition.Trap) || target.HasCondition(Condition.Decoy))
            {
                log.Add($"A magical barrier is protecting {target.Name}.");
                return log;
            }

            if (!target.IsAlive)
            {
                return log;
            }

            var baseDmg = Global.RandomNumber(0, 5);
            var dmg = AttackBased ?
                Math.Max(0,
                (user.Stats.Atk * user.MultiplyBuffs("Attack") - target.Stats.Def * target.IgnoreDefense * target.MultiplyBuffs("Defense")) / 2)
                : (int)Power;

            var elMult = 1 + (user.ElStats.GetPower(Element) * user.MultiplyBuffs("Power") - target.ElStats.GetRes(Element) * target.MultiplyBuffs("Resistance")) / (AttackBased ? 400 : 200);
            elMult = Math.Max(0.5, elMult);
            elMult = Math.Max(0, elMult);
            var prctdmg = (uint)(target.Stats.MaxHP * PercentageDamage / 100);
            var realDmg = (uint)((baseDmg + dmg + AddDamage) * DmgMult * elMult * target.DefensiveMult * user.OffensiveMult + prctdmg);
            var punctuation = "!";

            if (target.ElStats.GetRes(Element) == target.ElStats.HighestRes())
            {
                punctuation = ".";
            }

            if (target.ElStats.GetRes(Element) == target.ElStats.LowestRes())
            {
                punctuation = "!!!";
                if (user is PlayerFighter k)
                {
                    k.BattleStats.AttackedWeakness++;
                }
            }

            realDmg = Math.Max(1, realDmg);

            log.AddRange(target.DealDamage(realDmg, punctuation));
            user.DamageDoneThisTurn += realDmg;

            if (user is PlayerFighter p)
            {
                p.BattleStats.DamageDealt += realDmg;
                if (!target.IsAlive)
                {
                    p.BattleStats.Kills++;
                }
            }

            if (target.IsAlive && target.HasCondition(Condition.Counter))
            {
                var counterAtk = target.Stats.Atk * target.MultiplyBuffs("Attack");
                var counterDef = user.Stats.Def * user.MultiplyBuffs("Defense") * user.IgnoreDefense;
                uint counterDamage = (uint)Global.RandomNumber(0, 5);
                if (counterDef < counterAtk)
                {
                    counterDamage += (uint)((counterAtk - counterDef) * user.DefensiveMult / 2);
                }
                log.Add($"{target.Name} strikes back!");
                log.AddRange(user.DealDamage(counterDamage));
            }

            return log;
        }
    }
}