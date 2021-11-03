using System;
using System.Collections.Generic;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Defend : Move
    {
        public Defend(string emote = "<:Defend:536919830507552768>")
        {
            Name = "Defend";
            this.Emote = emote;
            TargetType = TargetType.PartySelf;
            HasPriority = true;
        }

        public override object Clone()
        {
            return new Defend();
        }

        public override void InternalChooseBestTarget(ColossoFighter user)
        {
            TargetNr = 0;
        }

        public override bool InternalValidSelection(ColossoFighter user)
        {
            return true;
        }

        protected override List<string> InternalUse(ColossoFighter user)
        {
            user.DefensiveMult = Math.Min(0.5, user.DefensiveMult);
            if (user is PlayerFighter p)
            {
                p.BattleStats.Defends++;
            }
            return new List<string>() { $"{Emote} {user.Name} is defending." };
        }
    }
}