using System;
using System.Collections.Generic;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Defend : Move
    {
        public Defend(string Emote = "<:Defend:536919830507552768>")
        {
            Name = "Defend";
            this.Emote = Emote;
            TargetType = Target.self;
            HasPriority = true;
        }

        public override object Clone()
        {
            return new Defend();
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            TargetNr = 0;
            return;
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            return User.battle.log.Count < 10;
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            User.defensiveMult = Math.Min(0.5, User.defensiveMult);
            if (User is PlayerFighter p)
            {
                p.battleStats.Defends++;
            }
            return new List<string>() { $"{Emote} {User.Name} is defending." };
        }
    }
}