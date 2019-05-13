using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace IodemBot.Modules.ColossoBattles
{
    public class NPCEnemy : ColossoFighter
    {
        [JsonProperty] private int ExtraTurns { get; set; } = 0;
        [JsonProperty] private string[] Movepool { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        private bool HasAttack { get; set; } = true;

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        private bool HasDefend { get; set; } = true;

        [JsonConstructor]
        public NPCEnemy(string name, string imgUrl, Stats stats, ElementalStats elstats, string[] movepool, bool hasAttack, bool hasDefend) : base(name, imgUrl, stats, elstats, PsynergyDatabase.GetMovepool(movepool, hasAttack, hasDefend))
        {
            this.Movepool = movepool;
            this.HasAttack = hasAttack;
            this.HasDefend = hasDefend;
        }

        public override List<string> ExtraTurn()
        {
            List<string> log = new List<string>();
            for (int i = 0; i < ExtraTurns; i++)
            {
                SelectRandom();
                log.AddRange(MainTurn());
            }
            return log;
        }

        public override List<string> EndTurn()
        {
            List<string> log = new List<string>();
            if (IsAlive())
            {
                SelectRandom();
            }
            else
            {
                selected = new Nothing();
                hasSelected = true;
            }
            log.AddRange(base.EndTurn());
            return log;
        }

        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<NPCEnemy>(serialized);
        }
    }
}