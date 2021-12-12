using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

namespace IodemBot.Discords.Actions
{
    public abstract class IodemBotCommandAction : BotCommandAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => true;

        public override GuildPermissions? RequiredPermissions => null;

        public override IActionSlashCommandProperties SlashCommandProperties => null;

        public override List<ActionTextCommandProperties> TextCommandProperties => null;

        public override IActionMessageCommandProperties MessageCommandProperties => null;

        public override IActionUserCommandProperties UserCommandProperties => null;

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            return SuccessFullResult;
        }
    }
}