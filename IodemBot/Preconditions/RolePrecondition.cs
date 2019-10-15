using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

// Inherit from PreconditionAttribute
public class RequireRoleAttribute : PreconditionAttribute
{
    // Create a field to store the specified name
    private readonly string _name = "";

    private readonly ulong _roleid = 0;
    private static readonly ulong serverID = 355558866282348574;

    // Create a constructor so the name can be specified
    public RequireRoleAttribute(string name) => _name = name;

    public RequireRoleAttribute(ulong roleid) => _roleid = roleid;

    // Override the CheckPermissions method
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        if (context.User is SocketGuildUser gUser && gUser.Guild.Id == serverID)
        {
            if (gUser.Roles.Any(r => r.Name == _name || r.Id == _roleid))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError($"You must have a role named {_name} to run this command."));
            }
        }
        else
        {
            return Task.FromResult(PreconditionResult.FromError("You must be in /r/GoldenSun server to run this command."));
        }
    }
}