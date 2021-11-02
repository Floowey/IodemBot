using Discord;

namespace IodemBot.ColossoBattles
{
    internal class TeamBattleEnvironment : PvPEnvironment
    {
        public TeamBattleEnvironment(ColossoBattleService battleService, string Name, ITextChannel lobbyChannel, bool isPersistent, ITextChannel teamAChannel, ITextChannel teamBChannel, IRole TeamBRole, uint playersToStart = 3) : base(battleService, Name, lobbyChannel, isPersistent, teamAChannel, teamBChannel, TeamBRole, playersToStart, playersToStart)
        {
            _ = Reset("init");
        }
    }
}