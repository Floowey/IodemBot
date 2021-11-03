using System.Threading.Tasks;
using Discord;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.ColossoBattles
{
    public class GoliathBattleEnvironment : PvPEnvironment
    {
        public GoliathBattleEnvironment(ColossoBattleService battleService, string Name, ITextChannel lobbyChannel, bool isPersistent, ITextChannel teamAChannel, ITextChannel teamBChannel, IRole TeamBRole, uint playersToStart = 4) : base(battleService, Name, lobbyChannel, isPersistent, teamAChannel, teamBChannel, TeamBRole, playersToStart, 1)
        {
            _ = Reset("init");
        }

        public override async Task AddPlayer(PlayerFighter player, Team team)
        {
            if (team == Team.B)
            {
                player.Stats *= new Stats(1000, 100, 200, 200, 10);
                player.Stats *= 0.01;
                player.Name = $"Goliath {player.Name}";
                player.IsImmuneToOHKO = true;
                player.IsImmuneToHPtoOne = true;
                player.AddCondition(Condition.DeathCurse);
                player.DeathCurseCounter = 10;
            }
            await base.AddPlayer(player, team);
        }
    }
}