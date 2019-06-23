using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot.Modules.ColossoBattles
{
    public enum BattleDifficulty { Tutorial = 0, Easy = 1, Medium = 2, MediumRare = 3, Hard = 4, Adept = 5 };

    [Group("colosso")]
    public class ColossoPvE : ModuleBase<SocketCommandContext>
    {
        public static string[] numberEmotes = new string[] { "\u0030\u20E3", "\u0031\u20E3", "\u0032\u20E3", "\u0033\u20E3", "\u0034\u20E3", "\u0035\u20E3",
            "\u0036\u20E3", "\u0037\u20E3", "\u0038\u20E3", "\u0039\u20E3" };

        private static List<BattleManager> battles = new List<BattleManager>();

        public static SocketTextChannel LobbyChannel { get; private set; }

        [Command("setup")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task SetupColosso()
        {
            LobbyChannel = (SocketTextChannel)Context.Channel;
            PvPBattleManager.TeamBRole = Context.Guild.GetRole(592413472277528589);

            await Context.Message.DeleteAsync();
            _ = Setup();
        }

        private async Task Setup()
        {
            battles.ForEach(old => old.Dispose());
            battles.Clear();
            //battles.Add(new SingleBattleManager("Bronze", LobbyChannel, await PrepareBattleChannel("Bronze"), BattleDifficulty.Easy));

            //battles.Add(new SingleBattleManager("Silver", LobbyChannel, await PrepareBattleChannel("Silver"), BattleDifficulty.Medium));

            //battles.Add(new SingleBattleManager("Gold", LobbyChannel, await PrepareBattleChannel("Gold"), BattleDifficulty.Hard));

            //battles.Add(new EndlessBattleManager("Showdown", LobbyChannel, await PrepareBattleChannel("Showdown")));
            battles.Add(new TeamBattleManager("OneVOne", LobbyChannel, await PrepareBattleChannel("OneVOneA", PermValue.Deny), await PrepareBattleChannel("OneVOneB", PermValue.Allow), 1));
            battles.Add(new TeamBattleManager("PvPTeam", LobbyChannel, await PrepareBattleChannel("PvPTeamA", PermValue.Deny), await PrepareBattleChannel("PvPTeamB", PermValue.Allow)));
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
                //a.SetEnemy(enemy);
            }
        }

        private async Task<ITextChannel> PrepareBattleChannel(string Name, PermValue teamBperm = PermValue.Inherit)
        {
            var channel = await Context.Guild.GetOrCreateTextChannelAsync("colosso-" + Name);
            await channel.ModifyAsync(c =>
            {
                c.CategoryId = ((ITextChannel)Context.Channel).CategoryId;
                c.Position = ((ITextChannel)Context.Channel).Position + battles.Count + 1;
            });
            await channel.SyncPermissionsAsync();

            if (teamBperm == PermValue.Allow)
            {
                await channel.AddPermissionOverwriteAsync(PvPBattleManager.TeamBRole, new OverwritePermissions(viewChannel: PermValue.Allow));
                await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(viewChannel: PermValue.Deny));
            }

            if (teamBperm == PermValue.Deny)
            {
                await channel.AddPermissionOverwriteAsync(PvPBattleManager.TeamBRole, new OverwritePermissions(viewChannel: PermValue.Deny));
            }
            var messages = await channel.GetMessagesAsync(100).FlattenAsync();
            await channel.DeleteMessagesAsync(messages);
            return channel;
        }
    }
}