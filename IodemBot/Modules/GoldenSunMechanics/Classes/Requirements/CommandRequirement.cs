using IodemBot.Core.UserManagement;
using System;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class CommandRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return (int)Math.Floor(Math.Sqrt(user.ServerStats.CommandsUsed) / 12);
        }
    }
}