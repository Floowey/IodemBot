using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot.Modules.ColossoBattles
{
    public enum BattleDifficulty { Tutorial = 0, Easy = 1, Medium = 2, MediumRare = 3, Hard = 4, Adept = 5 };

    [Group("colosso"), Alias("c")]
    public class ColossoPvE : ModuleBase<SocketCommandContext>
    {
        public static string[] numberEmotes = new string[] { "\u0030\u20E3", "\u0031\u20E3", "\u0032\u20E3", "\u0033\u20E3", "\u0034\u20E3", "\u0035\u20E3",
            "\u0036\u20E3", "\u0037\u20E3", "\u0038\u20E3", "\u0039\u20E3" };

        private static List<BattleEnvironment> battles = new List<BattleEnvironment>();

        public static SocketTextChannel LobbyChannel { get; private set; }

        [Command("setup")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task SetupColosso()
        {
            LobbyChannel = (SocketTextChannel)Context.Channel;
            PvPEnvironment.TeamBRole = Context.Guild.GetRole(592413472277528589) ?? Context.Guild.GetRole(602241074261524498); //Test: 592413472277528589, GS: 602241074261524498

            await Context.Message.DeleteAsync();
            _ = Setup();
        }

        private async Task Setup()
        {
            battles.ForEach(old => old.Dispose());
            battles.Clear();
            battles.Add(new SingleBattleEnvironment("Bronze", LobbyChannel, await PrepareBattleChannel("Bronze"), BattleDifficulty.Easy));
            battles.Add(new SingleBattleEnvironment("Silver", LobbyChannel, await PrepareBattleChannel("Silver"), BattleDifficulty.Medium));
            battles.Add(new SingleBattleEnvironment("Gold", LobbyChannel, await PrepareBattleChannel("Gold"), BattleDifficulty.Hard));
            battles.Add(new EndlessBattleEnvironment("Showdown", LobbyChannel, await PrepareBattleChannel("Showdown")));
            battles.Add(new TeamBattleEnvironment("PvPTeam", LobbyChannel, await PrepareBattleChannel("PvPTeamA", RoomVisibility.Private), await PrepareBattleChannel("PvPTeamB", RoomVisibility.TeamB)));
            //battles.Add(new TeamBattleManager("OneVOne", LobbyChannel, await PrepareBattleChannel("OneVOneA", PermValue.Deny), await PrepareBattleChannel("OneVOneB", PermValue.Allow), 1));
            //battles.Add(new GauntletBattleManager("Gauntlet", LobbyChannel, await PrepareBattleChannel("Dungeon"), "Vale"));
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
            var a = battles.OfType<PvEEnvironment>().Where(b => b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (a != null)
            {
                a.SetEnemy(enemy);
                //_ = a.Reset();
            }
        }

        [Command("dungeon")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Dungeon([Remainder] string DungeonName)
        {
            var User = UserAccounts.GetAccount(Context.User);
            if (!EnemiesDatabase.HasDungeon(DungeonName))
            {
                //invalid Dungeon
                return;
            }
            if (User.Dungeons.Contains(DungeonName, StringComparer.InvariantCulture) || EnemiesDatabase.DefaultDungeons.Any(d => d.Name.ToLower().Equals(DungeonName.ToLower())))
            {
                var Dungeon = EnemiesDatabase.GetDungeon(DungeonName);
                var openBattle = (battles.OfType<GauntletBattleEnvironment>().Where(b => !b.IsActive).FirstOrDefault());
                if (openBattle == null)
                {
                    //No room available
                    return;
                }

                openBattle.SetEnemy(DungeonName);
                _ = openBattle.Reset();

                if (Dungeon.IsOneTimeOnly)
                {
                    User.Dungeons.Remove(Dungeon.Name);
                }
            }
            await Task.CompletedTask;
        }

        [Command("Status")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task StatusOfBattle(string name)
        {
            await Context.Message.DeleteAsync();
            var a = battles.Where(b => b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (a != null)
            {
                await Context.Channel.SendMessageAsync(a.GetStatus());
            }
        }

        private enum RoomVisibility { All, TeamA, TeamB, Private }

        private async Task<ITextChannel> PrepareBattleChannel(string Name, RoomVisibility visibility = RoomVisibility.All)
        {
            var channel = await Context.Guild.GetOrCreateTextChannelAsync("colosso-" + Name);
            await channel.ModifyAsync(c =>
            {
                c.CategoryId = ((ITextChannel)Context.Channel).CategoryId;
                c.Position = ((ITextChannel)Context.Channel).Position + battles.Count + 1;
            });
            await channel.SyncPermissionsAsync();

            if (visibility == RoomVisibility.TeamB)
            {
                await channel.AddPermissionOverwriteAsync(PvPEnvironment.TeamBRole, new OverwritePermissions(viewChannel: PermValue.Allow));
                await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(viewChannel: PermValue.Deny));
            }

            if (visibility == RoomVisibility.TeamA)
            {
                await channel.AddPermissionOverwriteAsync(PvPEnvironment.TeamBRole, new OverwritePermissions(viewChannel: PermValue.Deny));
            }

            if (visibility == RoomVisibility.Private)
            {
                await channel.AddPermissionOverwriteAsync(PvPEnvironment.TeamBRole, new OverwritePermissions(viewChannel: PermValue.Deny));
                await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(viewChannel: PermValue.Deny));
            }
            var messages = await channel.GetMessagesAsync(100).FlattenAsync();
            await channel.DeleteMessagesAsync(messages);
            return channel;
        }

        public static bool UserInBattle(UserAccount player)
        {
            return battles.Any(s => s.ContainsPlayer(player.ID));
        }
    }
}