using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class OffensivePsynergy : Psynergy
    {
        private static readonly double[] spread = new double[] { 1.0, 0.66, 0.5, 0.33, 0.25, 0.15, 0.1 };

        public uint Power { get; set; } = 0;
        public uint AddDamage { get; set; } = 0;
        public double DmgMult { get; set; } = 1;
        public uint PercentageDamage { get; set; } = 0;
        private bool AttackBased { get { return Power == 0; } }
        public bool IgnoreSpread { get; set; }

        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<OffensivePsynergy>(serialized);
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            var aliveEnemies = User.GetEnemies().Where(f => f.IsAlive).ToList();
            if (aliveEnemies.Count == 0)
            {
                TargetNr = 0;
                return;
            }
            TargetNr = User.GetEnemies().IndexOf(aliveEnemies[Global.Random.Next(0, aliveEnemies.Count)]);
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            //Psynergy Handling
            List<string> log = new List<string>();

            //Get enemies and targeted enemies
            double[] actualSpread = new double[2 * Range - 1];
            List<ColossoFighter> enemyTeam = User.battle.GetTeam(User.enemies);
            List<ColossoFighter> targets = GetTarget(User);

            int ii = 0;
            foreach (var t in targets)
            {
                if (!t.IsAlive)
                {
                    continue;
                }

                if (PPCost > 1 && t.IsImmuneToPsynergy)
                {
                    log.Add($"{t.Name} protects themselves with a magical barrier.");
                    continue;
                }

                //Effects that trigger before damage
                log.AddRange(Effects.Where(e => e.ActivationTime == TimeToActivate.beforeDamge).ApplyAll(User, t));

                if (!t.IsAlive)
                {
                    continue;
                }

                var baseDmg = Global.Random.Next(0, 4);
                var dmg = AttackBased ?
                    Math.Max(0,
                    ((int)User.Stats.Atk * User.MultiplyBuffs("Attack") - (int)t.Stats.Def * t.ignoreDefense * t.MultiplyBuffs("Defense")) / 2)
                    : (int)Power;

                //                var elMult = 1 + Math.Max(0.0, (int)User.elstats.GetPower(element) * User.MultiplyBuffs("Power") - (int)t.elstats.GetRes(element) * t.MultiplyBuffs("Resistance")) / (attackBased ? 400 : 200);
                var elMult = 1 + (User.ElStats.GetPower(Element) * User.MultiplyBuffs("Power") - t.ElStats.GetRes(Element) * t.MultiplyBuffs("Resistance")) / (AttackBased ? 400 : 200);
                elMult = Math.Max(0.5, elMult);
                elMult = Math.Max(0, elMult);
                var distFromCenter = Math.Abs(enemyTeam.IndexOf(t) - TargetNr);
                var spreadMult = IgnoreSpread ? 1 : spread[distFromCenter];
                var prctdmg = (uint)(t.Stats.MaxHP * PercentageDamage / 100);
                var realDmg = (uint)((baseDmg + dmg + AddDamage) * DmgMult * elMult * spreadMult * t.defensiveMult * User.offensiveMult + prctdmg);
                var punctuation = "!";

                if (User.offensiveMult > 1)
                {
                    Console.WriteLine($"{User.Name} uses {Name} with multiplier {User.offensiveMult}");
                }

                if (t.ElStats.GetRes(Element) == t.ElStats.HighestRes())
                {
                    punctuation = ".";
                }

                if (t.ElStats.GetRes(Element) == t.ElStats.LeastRes())
                {
                    punctuation = "!!!";
                    if (User is PlayerFighter k)
                    {
                        k.battleStats.AttackedWeakness++;
                    }
                }

                realDmg = Math.Max(1, realDmg);

                log.AddRange(t.DealDamage(realDmg, punctuation));
                User.damageDoneThisTurn += realDmg;

                log.AddRange(Effects.Where(e => e.ActivationTime == TimeToActivate.afterDamage).ApplyAll(User, t));

                if (User is PlayerFighter p)
                {
                    p.avatar.DealtDmg(realDmg);
                    if (!t.IsAlive)
                    {
                        if (AttackBased && Range == 1)
                        {
                            p.battleStats.KillsByHand++;
                        }
                        p.battleStats.Kills++;
                    }
                }

                if (t.IsAlive && t.HasCondition(Condition.Counter))
                {
                    var counterAtk = t.Stats.Atk * t.MultiplyBuffs("Attack");
                    var counterDef = User.Stats.Def * User.MultiplyBuffs("Defense") * User.ignoreDefense;
                    uint CounterDamage = (uint)Global.Random.Next(0, 4);
                    if (counterDef < counterAtk)
                    {
                        CounterDamage += (uint)((counterAtk - counterDef) * User.defensiveMult / 2);
                    }
                    log.Add($"{t.Name} strikes back!");
                    log.AddRange(User.DealDamage(CounterDamage));
                }
                ii++;
            }

            return log;
        }

        public override string ToString()
        {
            return $"Attack {(TargetType == Target.otherSingle ? "an enemy" : (TargetType == Target.otherAll ? $"all Enemies" : $"up to {Range * 2 - 1} Targets"))} with a base damage of {GoldenSun.ElementIcons[Element]} {(AttackBased ? "a normal physical Attack" : $"{Power}")}{(AddDamage > 0 ? $" plus an additional {AddDamage} Points" : "")}{(DmgMult != 1 ? $" multiplied by {DmgMult}" : "")}{(PercentageDamage > 0 ? $" and damages {PercentageDamage}% of the targets health" : "")}.{(TargetType == Target.self || TargetType == Target.ownSingle || TargetType == Target.ownAll ? "Target type set to hit your teammates! Probably an error..." : "")}";
        }
    }
}