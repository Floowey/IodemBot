using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class HealPsynergy : Psynergy
    {
        public int Percentage { get; set; }
        public int HealPower { get; set; }
        public int PpHeal { get; set; }
        public int PpPercent { get; set; }

        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<HealPsynergy>(serialized);
        }

        public override string ToString()
        {
            return
                $"Heals {(TargetType == TargetType.PartySelf ? "the user" : TargetType == TargetType.PartySingle ? "a teammate" : TargetType == TargetType.PartyAll ? "the users party" : "someone else")}. {(HealPower > 0 ? $"Heals HP with a Power of {HealPower}." : "")} {(Percentage > 0 ? $"Heals HP by {Percentage}%." : "")}{(PpHeal > 0 ? $"Heals PP with a Power of {PpHeal}." : "")}{(PpPercent > 0 ? $"Heals PP by {PpPercent}%." : "")}";
        }

        public override void InternalChooseBestTarget(ColossoFighter user)
        {
            if (TargetType == TargetType.PartyAll) return;
            var party = user.Party;
            var aliveFriends = party.Where(f => f.IsAlive).ToList();
            if (!aliveFriends.Any())
            {
                TargetNr = 0;
                return;
            }

            aliveFriends = aliveFriends.OrderBy(s => s.Stats.HP / s.Stats.MaxHP).ThenBy(s => s.Stats.HP).ToList();

            TargetNr = party.IndexOf(aliveFriends.Any(d => d.Name.Contains("Star"))
                ? party.Where(d => d.Name.Contains("Star")).Random()
                : aliveFriends.First());
        }

        public override bool InternalValidSelection(ColossoFighter user)
        {
            if (!base.InternalValidSelection(user)) return false;
            return user.Party.Where(f => f.IsAlive).Any(f => 100 * f.Stats.HP / f.Stats.MaxHP < 85);
        }

        protected override List<string> InternalUse(ColossoFighter user)
        {
            var log = new List<string>();
            var power = (int)(user.ElStats.GetPower(Element) * user.MultiplyBuffs("Power"));
            var targets = GetTarget(user);

            foreach (var t in targets)
            {
                var hPtoHeal = (uint)(HealPower * power / 100 + t.Stats.MaxHP * Percentage / 100);
                if (hPtoHeal > 0) log.AddRange(t.Heal(hPtoHeal));

                var ppToHeal = (uint)(PpHeal * power / 100 + t.Stats.MaxPP * PpPercent / 100);
                if (ppToHeal > 0) log.AddRange(t.RestorePp(ppToHeal));

                log.AddRange(Effects.ApplyAll(user, t));

                if (user is PlayerFighter p) p.BattleStats.HPhealed += hPtoHeal;
            }

            return log;
        }
    }
}