using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace IodemBot.Preconditions
{
    // Inherit from PreconditionAttribute
    public class RequireModerator : PreconditionAttribute
    {
        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User is SocketGuildUser gUser)
            {
                if (gUser.Roles.Any(r => r.Name is "Admin" or "Moderators") || gUser.Id == 300339714311847936)
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
                else
                {
                    return Task.FromResult(PreconditionResult.FromError("You must be moderator to run this command."));
                }
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("You must be in /r/GoldenSun server to run this command."));
            }
        }
    }
}