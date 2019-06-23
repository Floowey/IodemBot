using Discord;

namespace IodemBot.Modules.ColossoBattles
{
    internal class TeamBattleManager : PvPBattleManager
    {
        public TeamBattleManager(string Name, ITextChannel lobbyChannel, ITextChannel teamAChannel, ITextChannel teamBChannel, uint playersToStart = 3) : base(Name, lobbyChannel, teamAChannel, teamBChannel, playersToStart, playersToStart)
        {
        }
    }
}