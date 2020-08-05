using System;
using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class CommandRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return (int)Math.Floor(Math.Sqrt(user.ServerStats.CommandsUsed) / 12);
        }
    }
}