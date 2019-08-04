using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class HealPsynergy : Psynergy
    {
        public bool singleTarget;
        public int percentage;
        public int healPower;

        public HealPsynergy(string name, string emote, Target targetType, uint range, List<EffectImage> effectImages, Element element, uint PPCost, int healPower, int percentage, bool singleTarget) : base(name, emote, targetType, range, effectImages, element, PPCost)
        {
            this.percentage = percentage;
            this.healPower = healPower;
            this.singleTarget = singleTarget;
        }

        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<HealPsynergy>(serialized);
        }

        public override string ToString()
        {
            return $"Heals {(singleTarget ? "one Player" : "the whole Party")} with a power of {healPower} {(percentage > 0 ? $"and additional {percentage}%" : "")}.";
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            if (targetType == Target.ownAll)
            {
                return;
            }

            var aliveFriends = User.GetTeam().Where(f => f.IsAlive).ToList();
            if (aliveFriends.Count == 0)
            {
                targetNr = 0;
                return;
            }

            aliveFriends = aliveFriends.OrderBy(s => s.Stats.HP / s.Stats.MaxHP).ThenBy(s => s.Stats.HP).ToList();

            if (User.GetTeam().Any(d => d.Name.Contains("Star")))
            {
                targetNr = User.GetTeam().IndexOf(User.GetTeam().Where(d => d.Name.Contains("Star")).Random());
            }
            else
            {
                targetNr = User.GetTeam().IndexOf(aliveFriends.First());
            }
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            if (!base.InternalValidSelection(User))
            {
                return false;
            }
            return (User.battle.turn == 1 ||
                User.GetTeam().Where(f => f.IsAlive).Any(f => (100 * f.Stats.HP) / f.Stats.MaxHP < 85));
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            List<string> log = new List<string>();
            int Power = User.ElStats.GetPower(element);
            List<ColossoFighter> targets = GetTarget(User);

            foreach (var p in targets)
            {
                var HPtoHeal = (uint)(healPower * Power / 100 + p.Stats.MaxHP * percentage / 100);
                log.AddRange(p.Heal(HPtoHeal));
                effects.ForEach(e => log.AddRange(e.Apply(User, p)));
                if (User is PlayerFighter)
                {
                    ((PlayerFighter)User).battleStats.HPhealed += HPtoHeal;
                }
            }
            return log;
        }
    }
}