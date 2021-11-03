using System.Collections.Generic;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class UserDiesEffect : Effect
    {
        public override string Type => "UserDies";

        public UserDiesEffect()
        {
            ActivationTime = TimeToActivate.BeforeDamage;
        }

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            if (!user.IsAlive)
            {
                return new List<string>();
            }

            user.Kill();
            return new List<string>() { $"{user.Name} goes down from exhaustion." };
        }

        public override string ToString()
        {
            return "User takes itself down";
        }
    }
}