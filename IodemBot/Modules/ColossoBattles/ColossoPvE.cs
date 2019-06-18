using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot.Modules.ColossoBattles
{
    [Group("colosso")]
    public class ColossoPvE : ModuleBase<SocketCommandContext>
    {
        public static string[] numberEmotes = new string[] { "\u0030\u20E3", "1⃣", "\u0032\u20E3", "\u0033\u20E3", "\u0034\u20E3", "\u0035\u20E3",
            "\u0036\u20E3", "\u0037\u20E3", "\u0038\u20E3", "\u0039\u20E3" };

        private static List<BattleCollector> battles = new List<BattleCollector>();

        public static SocketTextChannel LobbyChannel { get; private set; }

        [Command("setup")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task SetupColosso()
        {
            LobbyChannel = (SocketTextChannel)Context.Channel;
            await Context.Message.DeleteAsync();
            _ = Setup();
        }

        private async Task Setup()
        {
            battles = new List<BattleCollector>();
            var b = await GetBattleCollector(Context, "Bronze", BattleDifficulty.Easy);
            battles.Add(b);

            b = await GetBattleCollector(Context, "Silver", BattleDifficulty.Medium);
            battles.Add(b);

            b = await GetBattleCollector(Context, "Gold", BattleDifficulty.Hard);
            battles.Add(b);

            b = await GetBattleCollector(Context, "Showdown", BattleDifficulty.Easy);
            b.IsEndless = true;

            battles.Add(b);
        }

        [Command("reset")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Reset(string name)
        {
            await Context.Message.DeleteAsync();
            var a = battles.Where(b => b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (a != null)
            {
                _ = a.Reset();
            }
        }

        [Command("setEnemy")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task SetEnemy(string name, [Remainder] string enemy)
        {
            await Context.Message.DeleteAsync();
            var a = battles.Where(b => b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (a != null)
            {
                a.SetEnemy(enemy);
            }
        }

        private async Task<BattleCollector> GetBattleCollector(SocketCommandContext Context, string Name, BattleDifficulty diff)
        {
            var channel = await Context.Guild.GetOrCreateTextChannelAsync("colosso-" + Name);
            await channel.ModifyAsync(c =>
            {
                c.CategoryId = ((ITextChannel)Context.Channel).CategoryId;
                c.Position = ((ITextChannel)Context.Channel).Position + battles.Count + 1;
            });
            await channel.SyncPermissionsAsync();
            var messages = await channel.GetMessagesAsync(100).FlattenAsync();
            await channel.DeleteMessagesAsync(messages);

            var b = new BattleCollector()
            {
                Name = Name,
                Diff = diff,
                BattleChannel = channel,
                EnemyMsg = await channel.SendMessageAsync($"Welcome to {Name} Battle!\n\nReact with <:Fight:536919792813211648> to join the {Name} Battle and press <:Battle:536954571256365096> when you are ready to battle!")
            };
            await b.Reset();
            return b;
        }

        public enum BattleDifficulty { Tutorial = 0, Easy = 1, Medium = 2, MediumRare = 3, Hard = 4, Adept = 5 };
    }
}