using Discord;

namespace IodemBot.ColossoBattles
{
    internal class TeamBattleEnvironment : PvPEnvironment
    {
        public TeamBattleEnvironment(ColossoBattleService battleService, string name, ITextChannel lobbyChannel,
            bool isPersistent, ITextChannel teamAChannel, ITextChannel teamBChannel, IRole teamBRole,
            uint playersToStart = 3) : base(battleService, name, lobbyChannel, isPersistent, teamAChannel, teamBChannel,
            teamBRole, playersToStart, playersToStart)
        {
            _ = Reset("init");
        }
    }
}