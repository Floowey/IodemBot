using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class OffensivePsynergy : Psynergy
    {
        public uint power = 0;
        public uint addDamage = 0;
        public double dmgMult = 1;
        public uint percentageDamage = 0;
        private bool attackBased;
        private double[] spread = new double[] { 1.0, 0.66, 0.5, 0.33, 0.25, 0.15, 0.1 };

        [JsonConstructor]
        public OffensivePsynergy(string name, string emote, Target targetType, uint range, List<EffectImage> effectImages, Element element, uint PPCost, uint power = 0, uint addDamage = 0, double dmgMult = 1) : base(name, emote, targetType, range, effectImages, element, PPCost)
        {
            this.power = power;
            this.addDamage = addDamage;
            this.dmgMult = dmgMult;
            if (this.dmgMult == 0)
            {
                this.dmgMult = 1;
            }

            attackBased = power == 0;
        }

        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<OffensivePsynergy>(serialized);
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            var aliveEnemies = User.getEnemies().Where(f => f.IsAlive()).ToList();
            if (aliveEnemies.Count == 0)
            {
                targetNr = 0;
                return;
            }
            targetNr = User.getEnemies().IndexOf(aliveEnemies[Global.random.Next(0, aliveEnemies.Count)]);
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            return User.stats.PP >= PPCost;
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            //Psynergy Handling
            List<string> log = new List<string>();

            //Get enemies and targeted enemies
            double[] actualSpread = new double[2 * range - 1];
            List<ColossoFighter> enemyTeam = User.battle.getTeam(User.enemies);
            List<ColossoFighter> targets = getTarget(User);

            int ii = 0;
            foreach (var t in targets)
            {
                if (!t.IsAlive())
                {
                    continue;
                }

                //Effects that trigger before damage
                effects
                    .Where(e => e.timeToActivate == IEffect.TimeToActivate.beforeDamge)
                    .ToList()
                    .ForEach(e => log.AddRange(e.Apply(User, t)));

                if (!t.IsAlive())
                {
                    continue;
                }

                if (t.isImmuneToPsynergy)
                {
                    log.Add($"{t.name} protects themselves with a magical barrier.");
                    return log;
                }

                var baseDmg = (new Random()).Next(0, 4);
                var dmg = attackBased ?
                    Math.Max(0,
                    ((int)User.stats.Atk * User.MultiplyBuffs("Attack") - (int)t.stats.Def * t.ignoreDefense * t.MultiplyBuffs("Defense")) / 2)
                    : (int)power;

                var elMult = 1 + Math.Max(0.0, (int)User.elstats.GetPower(element) * User.MultiplyBuffs("Power") - (int)t.elstats.GetRes(element) * t.MultiplyBuffs("Resistance")) / (attackBased ? 400 : 200);
                var distFromCenter = Math.Abs(enemyTeam.IndexOf(t) - targetNr);
                var spreadMult = spread[distFromCenter];
                var prctdmg = (uint)(t.stats.maxHP * percentageDamage / 100);
                var realDmg = (uint)((baseDmg + dmg + addDamage) * dmgMult * elMult * spreadMult * t.defensiveMult * User.offensiveMult + prctdmg);
                var punctuation = "!";

                if (t.elstats.GetRes(element) == t.elstats.highestRes())
                {
                    punctuation = ".";
                }

                if (t.elstats.GetRes(element) == t.elstats.leastRes())
                {
                    punctuation = "!!!";
                    if (User is PlayerFighter)
                    {
                        ((PlayerFighter)User).battleStats.attackedWeakness++;
                    }
                }

                if (realDmg == 0)
                {
                    realDmg = 1;
                }

                log.AddRange(t.DealDamage(realDmg, punctuation));
                effects
                    .Where(e => e.timeToActivate == IEffect.TimeToActivate.afterDamage)
                    .ToList()
                    .ForEach(e => log.AddRange(e.Apply(User, t)));

                if (User is PlayerFighter)
                {
                    ((PlayerFighter)User).avatar.dealtDmg(realDmg);
                    if (!t.IsAlive())
                    {
                        if (attackBased && range == 1)
                        {
                            ((PlayerFighter)User).battleStats.killsByHand++;
                        } ((PlayerFighter)User).battleStats.kills++;
                    }
                }

                //Counter
                ii++;
            }

            return log;
        }

        public override string ToString()
        {
            return $"Attack {(targetType == Target.otherSingle ? "an enemy" : (targetType == Target.otherAll ? $"all Enemies" : $"up to {range * 2 - 1} Targets"))} with a base damage of {(attackBased ? "a normal physical Attack" : $"{power}")}{(addDamage > 0 ? $" plus an additional {addDamage} Points" : "")}{(dmgMult != 1 ? $" multiplied by {dmgMult}" : "")}.";
        }
    }
}