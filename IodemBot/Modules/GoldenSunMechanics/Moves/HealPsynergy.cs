using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class HealPsynergy : Psynergy
    {
        public bool SingleTarget { get; set; }
        public int Percentage { get; set; }
        public int HealPower { get; set; }

        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<HealPsynergy>(serialized);
        }

        public override string ToString()
        {
            return $"Heals {(SingleTarget ? "one Player" : "the whole Party")} with a power of {HealPower} {(Percentage > 0 ? $"and additional {Percentage}%" : "")}.";
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            if (TargetType == Target.ownAll)
            {
                return;
            }

            var aliveFriends = User.GetTeam().Where(f => f.IsAlive).ToList();
            if (aliveFriends.Count == 0)
            {
                TargetNr = 0;
                return;
            }

            aliveFriends = aliveFriends.OrderBy(s => s.Stats.HP / s.Stats.MaxHP).ThenBy(s => s.Stats.HP).ToList();

            if (User.GetTeam().Any(d => d.Name.Contains("Star")))
            {
                TargetNr = User.GetTeam().IndexOf(User.GetTeam().Where(d => d.Name.Contains("Star")).Random());
            }
            else
            {
                TargetNr = User.GetTeam().IndexOf(aliveFriends.First());
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
            int Power = User.ElStats.GetPower(Element);
            List<ColossoFighter> targets = GetTarget(User);

            foreach (var p in targets)
            {
                var HPtoHeal = (uint)(HealPower * Power / 100 + p.Stats.MaxHP * Percentage / 100);
                log.AddRange(p.Heal(HPtoHeal));
                Effects.ForEach(e => log.AddRange(e.Apply(User, p)));
                if (User is PlayerFighter)
                {
                    ((PlayerFighter)User).battleStats.HPhealed += HPtoHeal;
                }
            }
            return log;
        }
    }
}