using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Discords.Actions;
using IodemBot.ColossoBattles;
using IodemBot.Discords;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Discord.WebSocket;
using IodemBot.Extensions;

namespace IodemBot.Modules
{
    public abstract class BattleAction : BotComponentAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => true;

        public override GuildPermissions? RequiredPermissions => null;

        protected ColossoBattleService BattleService;
        protected IServiceScope _scope;
        protected BattleEnvironment battle;
        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            _scope = ServiceProvider.CreateScope();
            BattleService = _scope.ServiceProvider.GetRequiredService<ColossoBattleService>();

            battle = BattleService.GetBattleEnvironment(Context.Channel);
            if (battle == null)
                return Task.FromResult((false, "Battle not found"));

            return Task.FromResult(guildResult);
        }
    }

    public abstract class InBattleAction : BattleAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => true;

        public override GuildPermissions? RequiredPermissions => null;


        protected PlayerFighter player;

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var baseResult = base.CheckCustomPreconditionsAsync();
            if (!baseResult.Result.Success)
                return baseResult;

            if (!BattleService.UserInBattle(Context.User.Id))
                return Task.FromResult((false, "You aren't in a battle."));

            player = battle.GetPlayer(Context.User.Id);
            if (player == null)
                return Task.FromResult((false, "You aren't in this battle."));

            if (!battle.IsUsersMessage(player, Context.Message))
                return Task.FromResult((false, "Click your own message!"));

            return baseResult;
        }
    }
}
