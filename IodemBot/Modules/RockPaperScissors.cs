using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using System;
using System.Threading.Tasks;

namespace IodemBot.Modules
{
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
        [Remarks("Roll a n-sided dice!")]
        public async Task Dice([Remainder] uint sides = 6)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription($"🎲 {(new Random()).Next(0, (int)sides) + 1}");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        public enum RpsEnum { Rock, Paper, Scissors }

        [Command("rps")]
        [Cooldown(4)]
        [Remarks("Rock, Paper, Scissors")]
        public async Task RockPaperScissorsAsync([Remainder] RpsEnum choice)
        {
            string[] emotesPlayer = { "🤜", "🖐️", "✌️" };
            string[] emotesCPU = { "🤛", "🖐️", "✌️" };

            var avatar = UserAccounts.GetAccount(Context.User);
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