using System;
using System.Threading.Tasks;
using Discord.Commands;
using IodemBot.Core;

namespace Iodembot.Preconditions
{
    // Inherit from PreconditionAttribute
    public class RequireUserServer : PreconditionAttribute
    {
        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Guild != null && GuildSettings.GetGuildSettings(context.Guild).isUserServer)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("You must be in an approved server to run this."));
            }
        }
    }
}