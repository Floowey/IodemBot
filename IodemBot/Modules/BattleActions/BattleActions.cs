using System.Threading.Tasks;
using Discord;
using IodemBot.ColossoBattles;
using IodemBot.Discords;
using IodemBot.Discords.Actions;
using Microsoft.Extensions.DependencyInjection;

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

            if (battle.isProcessing)
                return Task.FromResult((false, "Too fast."));

            if (!player.IsAlive)
                return Task.FromResult((false, "You are dead."));

            return baseResult;
        }
    }
}