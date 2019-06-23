using Discord;
using System;
using System.Threading.Tasks;

namespace IodemBot.Modules.ColossoBattles
{
    internal class GauntletBattleManager : PvEBattleManager
    {
        public GauntletBattleManager(string Name, ITextChannel lobbyChannel, ITextChannel BattleChannel) : base(Name, lobbyChannel, BattleChannel)
        {
        }

        public override BattleDifficulty Difficulty => throw new NotImplementedException();

        public override void SetEnemy(string Enemy)
        {
            throw new NotImplementedException();
        }

        public override void SetNextEnemy()
        {
            throw new NotImplementedException();
        }

        protected override Task GameOver()
        {
            throw new NotImplementedException();
        }
    }
}