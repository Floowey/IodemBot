using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot.Modules.ColossoBattles
{
    public class ColossoPvE : ModuleBase<SocketCommandContext>
    {
        public static string[] numberEmotes = new string[] { "\u0030\u20E3", "\u0031\u20E3", "\u0032\u20E3", "\u0033\u20E3", "\u0034\u20E3", "\u0035\u20E3",
            "\u0036\u20E3", "\u0037\u20E3", "\u0038\u20E3", "\u0039\u20E3" };

        private static List<BattleEnvironment> battles = new List<BattleEnvironment>();

        public static ulong[] ChannelIds
        {
            get
            {
                return battles.Select(b => b.GetIds).SelectMany(item => item).Distinct().ToArray();
            }
        }

        public async Task setupDungeon(string DungeonName, bool ModPermission = false)
        {
            var User = UserAccounts.GetAccount(Context.User);
            if (EnemiesDatabase.TryGetDungeon(DungeonName, out var Dungeon))
            {
                if (!User.Dungeons.Contains(Dungeon.Name) && !Dungeon.IsDefault && !ModPermission)
                {
                    await ReplyAsync($"If you can't tell me where this place is, I can't take you there. And even if you knew, they probably wouldn't let you in! Bring me a map or show to me that you have the key to enter.");
                    return;
                }

                if (!Dungeon.Requirement.Applies(User) && !ModPermission)
                {
                    await ReplyAsync($"I'm afraid that I can't take you to this place, it is too dangerous for you and me both.");
                    return;
                }

                var openBattle = battles.OfType<GauntletBattleEnvironment>().Where(b => b.IsReady && !b.IsDeleted).FirstOrDefault();
                if (openBattle == null)
                {
                    var gauntletFromUser = battles.Where(b => b.Name.Equals(Context.User.Username, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                    if (gauntletFromUser != null && gauntletFromUser.IsActive)
                    {
                        if (gauntletFromUser.IsActive)
                        {
                            await ReplyAsync($"What? You already are on an adventure!");
                            return;
                        }
                        else
                        {
                            await gauntletFromUser.Reset();
                            battles.Remove(gauntletFromUser);
                        }
                    }
                    openBattle = new GauntletBattleEnvironment($"{Context.User.Username}", GuildSetups.GetAccount(Context.Guild).ColossoChannel, await PrepareBattleChannel($"{Dungeon.Name}-{Context.User.Username}"), Dungeon.Name, true);

                    battles.Add(openBattle);
                }
                else
                {
                    openBattle.SetEnemy(DungeonName);
                }

                if (Dungeon.IsOneTimeOnly && !ModPermission)
                {
                    User.Dungeons.Remove(Dungeon.Name);
                }
                _ = Context.Message.DeleteAsync();
                _ = Context.Channel.SendMessageAsync($"{openBattle.Name} has been prepared for your adventure to {Dungeon.Name}");
            }
            else
            {
                await ReplyAsync($"I don't know where that place is.");
            }
            await Task.CompletedTask;
        }

        internal void removeBattle(string name)
        {
            battles.RemoveAll(b => b.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        [Command("c setup"), Alias("colosso setup")]
        [RequireStaff]
        [RequireUserServer]
        public async Task SetupColosso()
        {
            PvPEnvironment.TeamBRole = Context.Guild.GetRole(592413472277528589) ?? Context.Guild.GetRole(602241074261524498); //Test: 592413472277528589, GS: 602241074261524498

            await Context.Message.DeleteAsync();
            _ = Setup();
        }

        private async Task Setup()
        {
            battles.ForEach(old => old.Dispose());
            battles.Clear();
            battles.Add(new SingleBattleEnvironment("Wilds", GuildSetups.GetAccount(Context.Guild).ColossoChannel, await PrepareBattleChannel("Weyard-Wilds"), BattleDifficulty.Easy));
            //battles.Add(new SingleBattleEnvironment("Woods", LobbyChannel, await PrepareBattleChannel("Weyard-Woods"), BattleDifficulty.Medium));
            //battles.Add(new SingleBattleEnvironment("Wealds", LobbyChannel, await PrepareBattleChannel("Weyard-Wealds"), BattleDifficulty.Hard));

            //battles.Add(new EndlessBattleEnvironment("Endless", LobbyChannel, await PrepareBattleChannel("Endless-Encounters")));

            //battles.Add(new GauntletBattleEnvironment("Dungeon", LobbyChannel, await PrepareBattleChannel("deep-dungeon"), "Vale"));
            //battles.Add(new GauntletBattleEnvironment("Catabombs", LobbyChannel, await PrepareBattleChannel("chilly-catacombs"), "Vale"));
            //battles.Add(new TeamBattleEnvironment("PvP", LobbyChannel, await PrepareBattleChannel("PvP-A", RoomVisibility.Private), await PrepareBattleChannel("PvP-B", RoomVisibility.TeamB)));

            //battles.Add(new SingleBattleEnvironment("Gold", LobbyChannel, await PrepareBattleChannel("Gold"), BattleDifficulty.Hard));
            //battles.Add(new TeamBattleManager("OneVOne", LobbyChannel, await PrepareBattleChannel("OneVOneA", PermValue.Deny), await PrepareBattleChannel("OneVOneB", PermValue.Allow), 1));

            if (Global.Client.Activity == null)
            {
                await Global.Client.SetGameAsync("in Babi's Palace.", "https://www.twitch.tv/directory/game/Golden%20Sun", ActivityType.Streaming);
            }
        }

        [Command("c reset")]
        [RequireStaff]
        [RequireUserServer]
        public async Task Reset(string name)
        {
            await Context.Message.DeleteAsync();
            var a = battles.Where(b => b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (a != null)
            {
                _ = a.Reset();
            }
        }

        [Command("c setEnemy")]
        [RequireStaff]
        [RequireUserServer]
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
        [RequireUserServer]
        public async Task Dungeon([Remainder] string DungeonName)
        { _ = setupDungeon(DungeonName, false); await Task.CompletedTask; }

        [Command("moddungeon")]
        [RequireStaff]
        [RequireUserServer]
        public async Task ModDungeon([Remainder] string DungeonName)

        { _ = setupDungeon(DungeonName, true); await Task.CompletedTask; }

        [Command("alldungeons")]
        [RequireStaff]
        public async Task AllDungeon()
        {
            await ReplyAsync(string.Join("\n", EnemiesDatabase.dungeons.Values.Select(d => d.Name)));
        }

        [Command("givedungeon")]
        [RequireStaff]
        public async Task GiveDungeon(SocketGuildUser user, [Remainder]string dungeonName)
        {
            if (EnemiesDatabase.TryGetDungeon(dungeonName, out var dungeon))
            {
                UserAccounts.GetAccount(user).Dungeons.Add(dungeon.Name);
                _ = ReplyAsync($"{user.DisplayName()} got access to {dungeon.Name}");
            }
            await Task.CompletedTask;
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

        private async Task<ITextChannel> PrepareBattleChannel(string Name, RoomVisibility visibility = RoomVisibility.All)
        {
            var channel = await Context.Guild.GetOrCreateTextChannelAsync(Name);
            await channel.ModifyAsync(c =>
            {
                c.CategoryId = GuildSetups.GetAccount(Context.Guild).ColossoChannel.CategoryId;
                c.Position = GuildSetups.GetAccount(Context.Guild).ColossoChannel.Position + battles.Count;
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
            await channel.DeleteMessagesAsync(messages.Where(m => m.Timestamp.AddDays(14) > DateTime.Now));
            return channel;
        }

        public static bool UserInBattle(UserAccount player)
        {
            return battles.Any(s => s.ContainsPlayer(player.ID));
        }
    }
}