using Discord.Commands;
using IodemBot.Core;
using System;
using System.Threading.Tasks;

namespace Iodembot.Preconditions
{
    // Inherit from PreconditionAttribute
    public class RequireUserServer : PreconditionAttribute
    {
        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (GuildSettings.GetGuildSettings(context.Guild).isUserServer)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("You must not be in a non-approved server to run this."));
            }
        }
    }
}