using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Defend : Move
    {
        public Defend() : base("Defend", "<:Defend:536919830507552768>", Target.self, 1, new List<EffectImage>())
        {
            hasPriority = true;
        }

        public override object Clone()
        {
            return new Defend();
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            targetNr = 0;
            return;
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            return User.battle.log.Count < 3;
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            User.defensiveMult *= 0.5;
            if (User is PlayerFighter)
            {
                ((PlayerFighter)User).battleStats.Defends++;
            }
            return new List<string>() { $"{emote} {User.Name} is defending." };
        }
    }
}