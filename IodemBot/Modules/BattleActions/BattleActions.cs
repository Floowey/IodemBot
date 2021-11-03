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
        protected BattleEnvironment Battle;

        protected ColossoBattleService BattleService;
        protected IServiceScope Scope;
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => true;

        public override GuildPermissions? RequiredPermissions => null;

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            Scope = ServiceProvider.CreateScope();
            BattleService = Scope.ServiceProvider.GetRequiredService<ColossoBattleService>();

            Battle = BattleService.GetBattleEnvironment(Context.Channel);
            if (Battle == null)
                return Task.FromResult((false, "Battle not found"));

            return Task.FromResult(guildResult);
        }
    }

    public abstract class InBattleAction : BattleAction
    {
        protected PlayerFighter Player;
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => true;

        public override GuildPermissions? RequiredPermissions => null;

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var baseResult = base.CheckCustomPreconditionsAsync();
            if (!baseResult.Result.Success)
                return baseResult;

            if (!BattleService.UserInBattle(Context.User.Id))
                return Task.FromResult((false, "You aren't in a battle."));

            Player = Battle.GetPlayer(Context.User.Id);
            if (Player == null)
                return Task.FromResult((false, "You aren't in this battle."));

            if (!Battle.IsUsersMessage(Player, Context.Message))
                return Task.FromResult((false, "Click your own message!"));

            if (Battle.IsProcessing)
                return Task.FromResult((false, "Too fast."));

            if (!Player.IsAlive)
                return Task.FromResult((false, "You are dead."));

            return baseResult;
        }
    }
}