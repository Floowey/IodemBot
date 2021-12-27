using System.Collections.Generic;
using System.ComponentModel;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;

namespace IodemBot.ColossoBattles
{
    public class NpcEnemy : ColossoFighter
    {
        [JsonConstructor]
        public NpcEnemy(string name, string imgUrl, Stats stats, ElementalStats elstats, string[] movepool,
            bool hasAttack, bool hasDefend)
        {
            Name = name;
            ImgUrl = imgUrl;
            Stats = stats;
            ElStats = elstats;

            Moves = PsynergyDatabase.GetMovepool(movepool, hasAttack, hasDefend);

            Movepool = movepool;
            HasAttack = hasAttack;
            HasDefend = hasDefend;
        }

        [JsonProperty] private int ExtraTurns { get; set; }
        [JsonProperty] private string[] Movepool { get; set; }

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        private bool HasAttack { get; set; } = true;

        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        private bool HasDefend { get; set; } = true;

        public override List<string> ExtraTurn()
        {
            var log = new List<string>();
            if (SelectedMove is Nothing) // don't have an extra turn if it was previously set to nothing from Killing
                return log;
            for (var i = 0; i < ExtraTurns; i++)
            {
                SelectRandom(false);
                log.AddRange(MainTurn());
            }

            return log;
        }

        public override List<string> EndTurn()
        {
            var log = new List<string>();
            if (IsAlive)
            {
                SelectRandom();
            }
            else
            {
                SelectedMove = new Nothing();
                HasSelected = true;
            }

            log.AddRange(base.EndTurn());
            return log;
        }

        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<NpcEnemy>(serialized);
        }
    }
}