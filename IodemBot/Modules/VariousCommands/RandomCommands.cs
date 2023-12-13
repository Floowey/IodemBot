using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Core.Leveling;
using IodemBot.Extensions;
using IodemBot.Preconditions;

namespace IodemBot.Modules
{
    [Name("Literally random things")]
    public class RandomCommands : ModuleBase<SocketCommandContext>
    {
        private readonly string[] _oracleResults =
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

        [Command("coin")]
        [Alias("coinflip")]
        [Cooldown(4)]
        [Remarks("Heads or tails!")]
        public async Task CoinToss()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription(Global.RandomNumber(0, 2) == 0
                ? "<:Lucky_Medals:538050800342269973> Heads!"
                : "<:Gold:537214319591555073> Tails!");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("dice")]
        [Alias("d")]
        [Cooldown(4)]
        [Remarks("Roll an n-sided dice!")]
        public async Task Dice([Remainder] uint sides = 6)
        {
            var embed = new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription($"🎲 {Global.RandomNumber(0, (int)sides) + 1}");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("dice")]
        [Alias("d")]
        [Cooldown(4)]
        [Remarks("Roll an n-sided dice!")]
        public async Task Dice([Remainder] string syntax = "")
        {
            syntax = syntax.ToLower();
            var nSides = 6;
            var nThrows = 1;
            if (syntax.Contains('d'))
            {
                var parts = syntax.Split('d');
                int.TryParse(parts[0], out nThrows);
                int.TryParse(parts[1], out nSides);
            }

            var embed = new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription(
                    $"🎲 {nThrows}d{nSides}: {string.Join(", ", Enumerable.Range(0, nThrows).Select(i => Global.RandomNumber(0, nSides) + 1))}");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("8ball")]
        [Alias("Oracle", "Fortune Teller", "Seer")]
        [Cooldown(5)]
        [Remarks("Ask the Oracle about your future.")]
        public async Task Oracle([Remainder] string question)
        {
            var teller = new[] { "Seer", "Fortune Teller" }.Random();
            var sprite = Sprites.GetImageFromName(teller);
            var beginning = teller == "Seer"
                ? "Hoolabaloo! Ballabahoo! Hoolabaloola! I can see it clearly... "
                : "I see... ";
            var response = _oracleResults.Random();
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
                _ = GoldenSunCommands.AwardClassSeries("Air Seer Series", Context.User, Context.Channel);
            await Task.CompletedTask;
        }

        [Command("rps")]
        [Cooldown(4)]
        [Remarks("Rock, Paper, Scissors")]
        public async Task RockPaperScissorsAsync([Remainder] RpsEnum choice)
        {
            string[] emotesPlayer = { "🤜", ":hand_splayed:", ":v:" };
            string[] emotesCpu = { "🤛", ":hand_splayed:", ":v:" };

            var cpuChoice = Enum.GetValues<RpsEnum>().Random();
            var result = "";

            switch ((int)choice - (int)cpuChoice)
            {
                case 1:
                case -2:
                    result = "You read me like an open book! You win!";
                    _ = ServerGames.UserWonRps((SocketGuildUser)Context.User, (SocketTextChannel)Context.Channel);
                    break;

                case 0:
                    ServerGames.UserDidNotWinRps((SocketGuildUser)Context.User);
                    result = "I may not have the gift of Psynergy, but I can still match your strength!";
                    break;

                case -1:
                case 2:
                    ServerGames.UserDidNotWinRps((SocketGuildUser)Context.User);
                    result = "Ahah! I may forever remember the day I beat an Adept in a fair game!";
                    break;
            }

            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription($"{emotesPlayer[(int)choice]} vs {emotesCpu[(int)cpuChoice]}");
            embed.AddField("Result:", result);

            _ = Context.Channel.SendMessageAsync("", false, embed.Build());
            await Task.CompletedTask;
        }

        [Command("choose")]
        [Alias("pick")]
        [Cooldown(15)]
        [Summary("Choose from several words or group of words seperated by ','")]
        public async Task Choose([Remainder] string s)
        {
            var choices = s.Split(' ');
            if (s.Contains(',')) choices = s.Split(',');
            foreach (var c in choices) c.Trim();
            var choice = choices.Random();
            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription($"➡️ {choice}")
                .Build());
        }
    }
}