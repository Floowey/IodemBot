using System.Collections.Generic;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class StatusPsynergy : Psynergy
    {
        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<StatusPsynergy>(serialized);
            //return MemberwiseClone();
        }

        public override void InternalChooseBestTarget(ColossoFighter user)
        {
            TargetNr = Effects.Count > 0 ? Effects[0].ChooseBestTarget(OnEnemy ? user.Enemies : user.Party) : 0;
        }

        public override bool InternalValidSelection(ColossoFighter user)
        {
            if (!base.InternalValidSelection(user))
            {
                return false;
            }

            if (Effects.Count > 0)
            {
                return Effects[0].ValidSelection(user);
            }

            return true;
        }

        protected override List<string> InternalUse(ColossoFighter user)
        {
            List<string> log = new List<string>();
            //Get enemies and targeted enemies
            List<ColossoFighter> targets = GetTarget(user);

            foreach (var t in targets)
            {
                if (PpCost > 1 && user.Enemies.Contains(t) && t.IsImmuneToPsynergy)
                {
                    log.Add($"{t.Name} protects themselves with a magical barrier.");
                    continue;
                }
                log.AddRange(Effects.ApplyAll(user, t));
                if (user is PlayerFighter p)
                {
                    p.BattleStats.Supported++;
                }
            }

            return log;
        }

        public override string ToString()
        {
            string target = "";
            switch (TargetType)
            {
                case TargetType.PartySelf: target = "the User"; break;
                case TargetType.PartySingle: target = "a party member"; break;
                case TargetType.PartyAll: target = "the party"; break;
                case TargetType.EnemyRange: target = Range == 1 ? "an enemy" : $"a range of {Range * 2 - 1} enemies"; break;
                case TargetType.EnemyAll: target = "all enemies"; break;
            }
            return $"Apply an Effect to {target}.";
        }
    }
}