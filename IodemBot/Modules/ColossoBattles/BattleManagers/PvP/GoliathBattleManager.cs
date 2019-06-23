using Discord;
using System.Threading.Tasks;

namespace IodemBot.Modules.ColossoBattles
{
    public class GoliathBattleManager : PvPBattleManager
    {
        public GoliathBattleManager(string Name, ITextChannel lobbyChannel, ITextChannel teamAChannel, ITextChannel teamBChannel, uint playersToStart = 4) : base(Name, lobbyChannel, teamAChannel, teamBChannel, playersToStart, 1)
        {
        }

        protected override async Task AddPlayer(PlayerFighter player, ColossoBattle.Team team)
        {
            if (team == ColossoBattle.Team.B)
            {
                player.stats *= new GoldenSunMechanics.Stats(1000, 100, 200, 200, 10);
                player.stats *= 0.01;
                player.name = $"Goliath {player.name}";
                player.IsImmuneToOHKO = true;
                player.IsImmuneToHPtoOne = true;
                player.AddCondition(Condition.DeathCurse);
                player.DeathCurseCounter = 10;
            }
            await base.AddPlayer(player, team);
        }
    }
}