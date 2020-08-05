using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core.Leveling;
using IodemBot.Extensions;

namespace IodemBot.Modules
{
    [Name("Literally random things")]
    public class RockPaperScissors : ModuleBase<SocketCommandContext>
    {
        [Command("coin"), Alias("coinflip")]
        [Cooldown(4)]
        [Remarks("Heads or tails!")]
        public async Task CoinToss()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription((new Random()).Next(0, 2) == 1 ? "<:Lucky_Medals:538050800342269973> Heads!" : "<:Gold:537214319591555073> Tails!");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("dice"), Alias("d")]
        [Cooldown(4)]
        [Remarks("Roll an n-sided dice!")]
        public async Task Dice([Remainder] uint sides = 6)
        {
            var embed = new EmbedBuilder()
            .WithColor(Colors.Get("Iodem"))
            .WithDescription($"🎲 {(new Random()).Next(0, (int)sides) + 1}");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        private readonly string[] oracleResults = new[]
        {
            "It is certain.",
            "It is decidedly so.",
            "Without a doubt.",
            "Yes, definitely.",
            "You may rely on it.",
            "As I see it, yes.",
            "Most likely.",
            "The outlook is good.",
            "Yes.",
            "The spirits point to yes.",
            "The spirits are hazy, try again",
            "*zzzZZzzz*... Ask again later...",
            "Better not tell you now",
            "Don't count on it.",
            "My reply is no.",
            "The spirits say no.",
            "The outlook is not so good.",
            "You will see...",
            "Very doubtful"
        };
        [Command("8ball"), Alias("Oracle", "Fortune Teller", "Seer")]
        [Cooldown(5)]
        [Remarks("Ask the Oracle about your future.")]
        public async Task Oracle([Remainder] string question)
        {
            var teller = new[] { "Seer", "Fortune Teller" }.Random();
            var sprite = Sprites.GetImageFromName(teller);
            var beginning = teller == "Seer" ? "Hoolabaloo! Ballabahoo! Hoolabaloola! I can see it clearly... " : "I see... ";
            var response = oracleResults.Random();
            if (!question.EndsWith('?'))
            {
                _ = ReplyAsync(embed: new EmbedBuilder()
                    .WithDescription("So, you'd like to hear your fortune, would you?")
                    .WithAuthor(teller, sprite)
                    .Build());
                return;
            }
            _ = ReplyAsync(embed: new EmbedBuilder()
                .WithDescription(beginning + response)
                .WithAuthor(teller, sprite)
                .Build());

            if (teller == "Seer" && response.Contains("spirits"))
            {
                _ = GoldenSun.AwardClassSeries("Air Seer Series", Context.User, Context.Channel);
            }
            await Task.CompletedTask;
        }

        [Command("rps")]
        [Cooldown(4)]
        [Remarks("Rock, Paper, Scissors")]
        public async Task RockPaperScissorsAsync([Remainder] RpsEnum choice)
        {
            string[] emotesPlayer = { "🤜", ":hand_splayed:", ":v:" };
            string[] emotesCPU = { "🤛", ":hand_splayed:", ":v:" };

            RpsEnum cpuChoice = (RpsEnum)((new Random()).Next(0, 1000) % 3);
            string result = "";

            switch ((int)choice - (int)cpuChoice)
            {
                case 1:
                case -2:
                    result = "You read me like an open book! You win!";
                    await ServerGames.UserWonRPS((SocketGuildUser)Context.User, (SocketTextChannel)Context.Channel);
                    break;

                case 0:
                    ServerGames.UserDidNotWinRPS((SocketGuildUser)Context.User);
                    result = "I may not have the gift of Psynergy, but I can still match your strength!";
                    break;

                case -1:
                case 2:
                    ServerGames.UserDidNotWinRPS((SocketGuildUser)Context.User);
                    result = "Ahah! I may forever remember the day I beat an Adept in a fair game!";
                    break;
            }

            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription($"{emotesPlayer[(int)choice]} vs {emotesCPU[(int)cpuChoice]}");
            embed.AddField("Result:", result);

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }
    }
}