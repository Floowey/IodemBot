using Discord;

namespace IodemBot.Modules.ColossoBattles
{
    internal class TeamBattleEnvironment : PvPEnvironment
    {
        public TeamBattleEnvironment(string Name, ITextChannel lobbyChannel, bool isPersistent, ITextChannel teamAChannel, ITextChannel teamBChannel, IRole TeamBRole, uint playersToStart = 3) : base(Name, lobbyChannel, isPersistent, teamAChannel, teamBChannel, TeamBRole, playersToStart, playersToStart)
        {
            _ = Reset();
        }
    }
}