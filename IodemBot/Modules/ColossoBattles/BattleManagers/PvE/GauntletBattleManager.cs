using Discord;
using System;
using System.Collections.Generic;
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

        private class Dungeon
        {
            public List<DungeonMatchup> matchups;
            public string flavourText;
            public string image;
        }

        private class DungeonMatchup
        {
            public List<NPCEnemy> Enemy;
            public string flavourText;
            public string Reward;
            public int RewardProbability;
        }
    }
}