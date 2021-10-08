using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Authentication.ExtendedProtection;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Discord.Rest;
using Discord.Commands.Builders;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Discords.Services;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Contexts;

namespace IodemBot.Discords
{
    [ModuleOrder(2)]
    public class GeneratedActionCommands : MessageCommandBase
    {
        // DI Services
        public ActionService ActionService { get; set; }
        public IServiceProvider ServiceProvider { get; set; }


        protected override void OnModuleBuilding(CommandService commandService, ModuleBuilder builder)
        {
            base.OnModuleBuilding(commandService, builder);

            using var scope = ServiceProvider.CreateScope();
            ActionService = ServiceProvider.GetRequiredService<ActionService>();
            //Get phrases from items as aliases for the referenced command.
            foreach (var action in ActionService.GetAll().OfType<BotCommandAction>().Where(s => s.TextCommandProperties != null))
            {
                foreach (var textProperties in action.TextCommandProperties)
                {
                    builder.AddCommand(textProperties.Name, RunActionFromTextCommand,
                        builder =>
                        {
                            if (textProperties.Aliases != null)
                                builder.AddAliases(textProperties.Aliases.ToArray());
                            builder.Summary = textProperties.Summary;
                            if (action.GuildsOnly)
                                builder.AddPrecondition(new RequireContextAttribute(ContextType.Guild));
                            if (action.RequiredPermissions.HasValue)
                            {
                                var requiredPermissions = action.RequiredPermissions.Value.ToList();
                                if (!requiredPermissions.Any())
                                    builder.AddPrecondition(new RequireOwnerAttribute());
                                else
                                {
                                    GuildPermission permissions = 0;
                                    foreach (var permission in requiredPermissions)
                                        permissions |= permission;

                                    builder.AddPrecondition(new RequireUserPermissionAttribute(permissions));
                                }
                            }
                            if (textProperties.Priority.HasValue)
                                builder.Priority = textProperties.Priority.Value;

                            var parameters = action.GetParameters<ActionParameterTextAttribute>()?.Where(p => p.Attribute.FilterCommandNames == null || p.Attribute.FilterCommandNames.Contains(textProperties.Name)).OrderBy(p => p.Attribute.Order);
                            if (parameters != null)
                            {
                                foreach (var p in parameters)
                                {
                                    builder.AddParameter(p.Attribute.Name, p.Attribute.ParameterType, pb =>
                                    {
                                        pb.Summary = p.Attribute.Description;
                                        pb.IsMultiple = p.Attribute.IsMultiple;
                                        pb.IsRemainder = p.Attribute.IsRemainder;
                                        pb.DefaultValue = p.Attribute.DefaultValue;
                                        pb.IsOptional = !p.Attribute.Required;
                                    });
                                }
                            }

                            textProperties.ModifyBuilder?.Invoke(scope.ServiceProvider, builder);
                        }
                    );
                }
            }
        }

        public async Task RunActionFromTextCommand(ICommandContext commandContext, object[] parmValues, IServiceProvider services, CommandInfo commandInfo)
        {
            var context = new RequestCommandContext(commandContext as SocketCommandContext);
            var actionRunFactory = ActionRunFactory.Find(services, context, commandInfo, parmValues);
            if (actionRunFactory != null)
                await actionRunFactory.RunActionAsync();
        }
    }
}