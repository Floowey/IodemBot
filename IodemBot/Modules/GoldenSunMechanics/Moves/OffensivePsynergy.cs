using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class OffensivePsynergy : Psynergy
    {
        public uint power = 0;
        public uint addDamage = 0;
         public double dmgMult = 1;
        private bool attackBased;
        private double[] spread = new double[] { 1.0, 0.66, 0.5, 0.33, 0.25, 0.15, 0.1 };
        [JsonConstructor]
        public OffensivePsynergy(string name, string emote, Target targetType, uint range, Element element, uint PPCost, uint power = 0, uint addDamage = 0, double dmgMult = 1) : base(name, emote, targetType, range, element, PPCost)
        {
            
            this.power = power;
            this.addDamage = addDamage;
            this.dmgMult = dmgMult;
            if (this.dmgMult == 0) this.dmgMult = 1;
            attackBased = power == 0;
        }

        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<OffensivePsynergy>(serialized);
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            //Psynergy Handling
            List<string> log = new List<string>();
            
            //Get enemies and targeted enemies
            double[] actualSpread = new double[2*range-1];
            List<ColossoFighter> enemyTeam = User.battle.getTeam(User.enemies);
            List<ColossoFighter> targets = getTarget(User);

            int ii = 0;
            foreach (var t in targets)
            {
                if (!t.IsAlive()) continue;

                //Effects that trigger before damage

                var baseDmg = (new Random()).Next(0, 4);
                var dmg = attackBased ? 
                    Math.Max(0, 
                    ((int)User.stats.Atk * User.MultiplyBuffs("Attack") - (int)t.stats.Def * t.ignoreDefense * t.MultiplyBuffs("Defense")) / 2) 
                    : (int)power;
                

                var elMult = 1 + Math.Max(0.0, (int)User.elstats.GetPower(element)*User.MultiplyBuffs("Power") - (int)t.elstats.GetRes(element)*t.MultiplyBuffs("Resistance")) / (attackBased ? 400 : 200);
                var distFromCenter = Math.Abs(enemyTeam.IndexOf(t) - targetNr);
                var spreadMult = spread[distFromCenter];
                var realDmg = (uint) ((baseDmg + dmg + addDamage) * dmgMult * elMult * spreadMult * t.defensiveMult * User.offensiveMult);
                var punctuation = "!";

                if (t.elstats.GetRes(element) == t.elstats.highestRes()) punctuation = ".";
                if (t.elstats.GetRes(element) == t.elstats.leastRes()) punctuation = "!!!";

                log.AddRange(t.dealDamage(realDmg, punctuation));
                //log.AddRange(effects.ForEach(e => e.Apply(User, Target)));

                if (User is PlayerFighter)
                {
                    ((PlayerFighter)User).avatar.dealtDmg(realDmg);
                    if (!t.IsAlive())
                        if (attackBased && range == 1)
                            ((PlayerFighter)User).avatar.killedByHand();
                }

                //Effects that trigger after damage

                //Counter
                ii++;
            }

            return log;
        }
    }
}
