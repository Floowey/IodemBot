using Xunit;
using Newtonsoft.Json;
using static IodemBot.ColossoBattles.EnemiesDatabase;

namespace IodemBotTest
{
    public class JsonValidity
    {
        [Theory]
        [InlineData("Resources/GoldenSun/Moves/healpsy.json")]
        [InlineData("Resources/GoldenSun/Moves/statpsy.json")]
        [InlineData("Resources/GoldenSun/Moves/offpsy.json")]
        [InlineData("Resources/GoldenSun/DjinnAndSummons/Summons.json")]
        [InlineData("Resources/GoldenSun/DjinnAndSummons/Djinn.json")]
        [InlineData("Resources/GoldenSun/Battles/tutorialFighters.json")]
        [InlineData("Resources/GoldenSun/Battles/bronzeFighters.json")]
        [InlineData("Resources/GoldenSun/Battles/silverFighters.json")]
        [InlineData("Resources/GoldenSun/Battles/goldFighters.json")]
        [InlineData("Resources/GoldenSun/Battles/enemies.json")]
        [InlineData("Resources/GoldenSun/Battles/dungeons.json")]
        [InlineData("Resources/GoldenSun/items.json")]
        public void ValidateJson(string x)
        {
            var exception = Record.Exception(() =>
            {
                var json = File.ReadAllText(x);
                var o = JsonConvert.DeserializeObject(json);
            }
            );
            Assert.True(exception is null, $"Error in file {x}: {exception?.Message}");
        }

        [Fact]
        public void AllDungeonEnemiesRegistered()
        {
            var json = File.ReadAllText("Resources/GoldenSun/Battles/dungeons.json");
            var Dungeons = JsonConvert.DeserializeObject<Dictionary<string, Dungeon>>(json);

            var exception = Record.Exception(() =>
            Dungeons.Values.ToList().SelectMany(d => d.Matchups.SelectMany(m => m.Enemy)).ToList());
            Assert.IsNotType<ArgumentException>(exception);
        }
    }
}