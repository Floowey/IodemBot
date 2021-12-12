using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using IodemBot.Discords;
using IodemBot.Discords.Actions;

namespace IodemBot.Modules
{
    public class RevealEphemeralAction : BotComponentAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.Permanent;

        public override bool GuildsOnly => false;

        public override GuildPermissions? RequiredPermissions => null;

        public override async Task RunAsync()
        {
            var msg = Context.Message;
            await Context.ReplyWithMessageAsync(EphemeralRule, message: $"{Context.User.Mention} revealed: {msg.Content}", embeds: msg.Embeds.ToArray(), messageReference: msg.Reference);
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            return SuccessFullResult;
        }
    }
}