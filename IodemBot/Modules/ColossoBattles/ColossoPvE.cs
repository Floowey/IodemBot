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

        public static ulong[] ChannelIds
        {
            get
            {
                return battles.Select(b => b.GetIds).SelectMany(item => item).Distinct().ToArray();
            }
        }

        [Command("setup")]
        [RequireStaff]
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
            battles.Add(new SingleBattleEnvironment("Wilds", LobbyChannel, await PrepareBattleChannel("Weyard-Wilds"), BattleDifficulty.Easy));
            battles.Add(new SingleBattleEnvironment("Woods", LobbyChannel, await PrepareBattleChannel("Weyard-Woods"), BattleDifficulty.Medium));
            battles.Add(new SingleBattleEnvironment("Wealds", LobbyChannel, await PrepareBattleChannel("Weyard-Wealds"), BattleDifficulty.Hard));

            battles.Add(new EndlessBattleEnvironment("Endless", LobbyChannel, await PrepareBattleChannel("Endless-Encounters")));

            //battles.Add(new GauntletBattleEnvironment("Dungeon", LobbyChannel, await PrepareBattleChannel("deep-dungeon"), "Vale"));
            //battles.Add(new GauntletBattleEnvironment("Catabombs", LobbyChannel, await PrepareBattleChannel("chilly-catacombs"), "Vale"));
            battles.Add(new TeamBattleEnvironment("PvP", LobbyChannel, await PrepareBattleChannel("PvP-A", RoomVisibility.Private), await PrepareBattleChannel("PvP-B", RoomVisibility.TeamB)));

            //battles.Add(new SingleBattleEnvironment("Gold", LobbyChannel, await PrepareBattleChannel("Gold"), BattleDifficulty.Hard));
            //battles.Add(new TeamBattleManager("OneVOne", LobbyChannel, await PrepareBattleChannel("OneVOneA", PermValue.Deny), await PrepareBattleChannel("OneVOneB", PermValue.Allow), 1));

            if (Global.Client.Activity == null)
            {
                await Global.Client.SetGameAsync("in Babi's Palace.", "https://www.twitch.tv/directory/game/Golden%20Sun", ActivityType.Streaming);
            }
        }

        [Command("reset")]
        [RequireStaff]
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
        [RequireStaff]
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
        [RequireStaff]
        public async Task Dungeon([Remainder] string DungeonName)
        {
            var User = UserAccounts.GetAccount(Context.User);
            if (!EnemiesDatabase.HasDungeon(DungeonName))
            {
                await Context.Channel.SendMessageAsync($"I don't know where that place is.");
                return;
            }
            if (User.Dungeons.Contains(DungeonName, StringComparer.InvariantCultureIgnoreCase) || EnemiesDatabase.DefaultDungeons.Any(d => d.Name.ToLower().Equals(DungeonName.ToLower())))
            {
                var Dungeon = EnemiesDatabase.GetDungeon(DungeonName);
                var openBattle = battles.OfType<GauntletBattleEnvironment>().Where(b => b.IsReady).FirstOrDefault();
                if (openBattle == null)
                {
                    await Context.Channel.SendMessageAsync($"All our carriots are full, please try again in a bit!");
                    return;
                }

                openBattle.SetEnemy(DungeonName);

                if (Dungeon.IsOneTimeOnly)
                {
                    User.Dungeons.Remove(Dungeon.Name);
                }
                _ = Context.Message.DeleteAsync();
                _ = Context.Channel.SendMessageAsync($"{openBattle.Name} has been prepared for your adventure to {Dungeon.Name}");
            }
        }

        [Command("Status")]
        [RequireStaff]
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
            var channel = await Context.Guild.GetOrCreateTextChannelAsync(Name);
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