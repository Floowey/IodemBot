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

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            if (Effects.Count > 0)
            {
                TargetNr = Effects[0].ChooseBestTarget(OnEnemy ? User.Enemies : User.Party);
            }
            else
            {
                TargetNr = 0;
            }
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            if (!base.InternalValidSelection(User))
            {
                return false;
            }

            if (Effects.Count > 0)
            {
                return Effects[0].ValidSelection(User);
            }

            return true;
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            List<string> log = new List<string>();
            //Get enemies and targeted enemies
            List<ColossoFighter> targets = GetTarget(User);

            foreach (var t in targets)
            {
                if (PPCost > 1 && User.Enemies.Contains(t) && t.IsImmuneToPsynergy)
                {
                    log.Add($"{t.Name} protects themselves with a magical barrier.");
                    continue;
                }
                log.AddRange(Effects.ApplyAll(User, t));
                if (User is PlayerFighter p)
                {
                    p.battleStats.Supported++;
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
                case TargetType.PartyAll: target = "the Party"; break;
                case TargetType.EnemyRange: target = Range == 1 ? "an enemy" : $"a range of {Range * 2 - 1} enemies"; break;
                case TargetType.EnemyAll: target = "all enemies"; break;
            }
            return $"Apply an Effect to {target}.";
        }
    }
}