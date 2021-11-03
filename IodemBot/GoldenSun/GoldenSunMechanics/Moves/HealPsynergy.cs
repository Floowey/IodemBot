using System.Collections.Generic;
using System.Linq;
using IodemBot.Extensions;
using IodemBot.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class HealPsynergy : Psynergy
    {
        public int Percentage { get; set; }
        public int HealPower { get; set; }
        public int PPHeal { get; set; }
        public int PPPercent { get; set; }

        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<HealPsynergy>(serialized);
        }

        public override string ToString()
        {
            return $"Heals {(TargetType == TargetType.PartySelf ? "the user" : TargetType == TargetType.PartySingle ? "a teammate" : TargetType == TargetType.PartyAll ? "the users party" : "someone else")}. {(HealPower > 0 ? $"Heals HP with a Power of {HealPower}." : "")} {(Percentage > 0 ? $"Heals HP by {Percentage}%." : "")}{(PPHeal > 0 ? $"Heals PP with a Power of {PPHeal}." : "")}{(PPPercent > 0 ? $"Heals PP by {PPPercent}%." : "")}";
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            if (TargetType == TargetType.PartyAll)
            {
                return;
            }
            var party = User.Party;
            var aliveFriends = party.Where(f => f.IsAlive).ToList();
            if (!aliveFriends.Any())
            {
                TargetNr = 0;
                return;
            }

            aliveFriends = aliveFriends.OrderBy(s => s.Stats.HP / s.Stats.MaxHP).ThenBy(s => s.Stats.HP).ToList();

            if (aliveFriends.Any(d => d.Name.Contains("Star")))
            {
                TargetNr = party.IndexOf(party.Where(d => d.Name.Contains("Star")).Random());
            }
            else
            {
                TargetNr = party.IndexOf(aliveFriends.First());
            }
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            if (!base.InternalValidSelection(User))
            {
                return false;
            }
            return (User.Party.Where(f => f.IsAlive).Any(f => (100 * f.Stats.HP) / f.Stats.MaxHP < 85));
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            List<string> log = new List<string>();
            int Power = (int)(User.ElStats.GetPower(Element) * User.MultiplyBuffs("Power"));
            List<ColossoFighter> targets = GetTarget(User);

            foreach (var t in targets)
            {
                var HPtoHeal = (uint)(HealPower * Power / 100 + t.Stats.MaxHP * Percentage / 100);
                if (HPtoHeal > 0)
                {
                    log.AddRange(t.Heal(HPtoHeal));
                }

                var PPToHeal = (uint)(PPHeal * Power / 100 + t.Stats.MaxPP * PPPercent / 100);
                if (PPToHeal > 0)
                {
                    log.AddRange(t.RestorePP(PPToHeal));
                }

                log.AddRange(Effects.ApplyAll(User, t));

                if (User is PlayerFighter p)
                {
                    p.battleStats.HPhealed += HPtoHeal;
                }
            }
            return log;
        }
    }
}
