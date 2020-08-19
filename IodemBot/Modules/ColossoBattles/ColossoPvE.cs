using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Modules.ColossoBattles
{
    [Name("Colosso")]
    public class ColossoPvE : ModuleBase<SocketCommandContext>
    {
        public static string[] numberEmotes = new string[] { "\u0030\u20E3", "\u0031\u20E3", "\u0032\u20E3", "\u0033\u20E3", "\u0034\u20E3", "\u0035\u20E3",
            "\u0036\u20E3", "\u0037\u20E3", "\u0038\u20E3", "\u0039\u20E3" };

        private static readonly List<BattleEnvironment> battles = new List<BattleEnvironment>();
        private static bool AcceptBattles = true;
        private static Dictionary<SocketGuildUser, DateTime> FighterRoles = new Dictionary<SocketGuildUser, DateTime>();
        public static ulong[] ChannelIds
        {
            get => battles.Select(b => b.GetChannelIds).SelectMany(item => item).Distinct().ToArray();
        }

        public async Task SetupDungeon(string DungeonName, bool ModPermission = false)
        {
            if (!AcceptBattles) return;
            if (!(Context.User is SocketGuildUser user)) return;
            var acc = UserAccounts.GetAccount(Context.User);
            var gs = GuildSettings.GetGuildSettings(Context.Guild);
            _ = RemoveFighterRoles();
            if (EnemiesDatabase.TryGetDungeon(DungeonName, out var Dungeon))
            {
                if (!acc.Dungeons.Contains(Dungeon.Name) && !Dungeon.IsDefault && !ModPermission)
                {
                    await ReplyAsync($"If you can't tell me where this place is, I can't take you there. And even if you knew, they probably wouldn't let you in! Bring me a map or show to me that you have the key to enter.");
                    return;
                }

                if (!Dungeon.Requirement.Applies(acc) && !ModPermission)
                {
                    await ReplyAsync($"I'm afraid that I can't take you to this place, it is too dangerous for you and me both.");
                    return;
                }

                var openBattle = battles.OfType<GauntletBattleEnvironment>().Where(b => b.IsReady && b.IsPersistent).FirstOrDefault();
                if (openBattle == null)
                {
                    var gauntletFromUser = battles.OfType<GauntletBattleEnvironment>().Where(b => b.Name.Equals(Context.User.Username, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                    if (gauntletFromUser != null)
                    {
                        if (gauntletFromUser.IsActive)
                        {
                            await ReplyAsync($"What? You already are on an adventure!");
                            Console.WriteLine($"User Active in: {gauntletFromUser.Name}; {gauntletFromUser.BattleChannel.Id}");
                            return;
                        }
                        else
                        {
                            _ = gauntletFromUser.Reset();
                            battles.Remove(gauntletFromUser);
                        }
                    }
                    openBattle = new GauntletBattleEnvironment($"{Context.User.Username}", GuildSettings.GetGuildSettings(Context.Guild).ColossoChannel, await PrepareBattleChannel($"{Dungeon.Name}-{Context.User.Username}", Context.Guild, persistent: false), Dungeon.Name, false);

                    battles.Add(openBattle);
                }
                else
                {
                    openBattle.SetEnemy(DungeonName);
                }

                if (Dungeon.IsOneTimeOnly && !ModPermission)
                {
                    acc.Dungeons.Remove(Dungeon.Name);
                }
                _ = Context.Channel.SendMessageAsync($"{Context.User.Username}, {openBattle.BattleChannel.Mention} has been prepared for your adventure to {Dungeon.Name}");

                _ = AddFighterRole();
            }
            else
            {
                await ReplyAsync($"I don't know where that place is.");
            }

            await Task.CompletedTask;
        }

        private async Task RemoveFighterRoles()
        {
            var gs = GuildSettings.GetGuildSettings(Context.Guild);
            List<SocketGuildUser> toBeRemoved = new List<SocketGuildUser>();
            foreach (var entry in FighterRoles)
            {
                if ((DateTime.Now - entry.Value).TotalMinutes > 10)
                {
                    if(entry.Key.Roles.Any(r => r.Id == gs.FighterRole.Id))
                    _ = entry.Key.RemoveRoleAsync(gs.FighterRole);
                    toBeRemoved.Add(entry.Key);
                }
            }
            toBeRemoved.ForEach(d => FighterRoles.Remove(d));
            await Task.CompletedTask;
        }

        private async Task AddFighterRole()
        {
            var gs = GuildSettings.GetGuildSettings(Context.Guild);
            if (!(Context.User is SocketGuildUser user)) return;
            if (!user.Roles.Any(r => r.Id == gs.FighterRole.Id))
            {
                _ = user.AddRoleAsync(gs.FighterRole);
                FighterRoles.Add(user, DateTime.Now);
            } else
            {
                if (FighterRoles.Remove(user))
                {
                    FighterRoles.Add(user, DateTime.Now);
                }
            }
            await Task.CompletedTask;
        }

        internal void RemoveBattle(string name)
        {
            battles.RemoveAll(b => b.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        [Command("DungeonInfo"), Alias("dgi")]
        [Summary("Get information about a dungeon")]
        public async Task DungeonInfo([Remainder] string DungeonName)
        {
            if (EnemiesDatabase.TryGetDungeon(DungeonName, out var dungeon))
            {
                var RewardTablesWithDjinn = dungeon.Matchups
                    .SelectMany(m => m.RewardTables.Where(t => t.OfType<DefaultReward>().Any(r => r.Djinn != "")));


                var djinnTotal = RewardTablesWithDjinn.SelectMany(t => t.OfType<DefaultReward>().Where(r => r.Djinn != ""));

                var limittedDjinn = djinnTotal.GroupBy(d => d.Tag)
                    .Select(k => k.OrderByDescending(d => d.Obtainable).First());

                var unlimittedDjinn = djinnTotal.Where(d => d.Obtainable == 0);

                var avatar = UserAccounts.GetAccount(Context.User);
                var djinnobtained = avatar.Tags.Count(t => limittedDjinn.Any(r => r.Tag.Contains(t)));

                var probability = djinnTotal.Count() > 0 ? 1 - 1.0 / RewardTablesWithDjinn
                    .Select(
                        r => 1.0 / (1 - (
                            r.Where(
                                k =>
                                k is DefaultReward dr
                                && dr.Djinn != ""
                                && dr.RequireTag.All(t => avatar.Tags.Contains(t))
                                && (dr.Obtainable == 0 || avatar.Tags.Count(t => t.Equals(dr.Tag)) < dr.Obtainable)
                            )
                            .Select(r => r.Weight).Sum()
                            / (double)r.Select(d => d.Weight).Sum()
                            )
                        )
                    )
                    .Aggregate((p, c) => p *= c)
                    : 0.0;

                _ = ReplyAsync(embed: new EmbedBuilder()
                    .WithTitle(dungeon.Name)
                    .WithDescription(dungeon.FlavourText)
                    .WithThumbnailUrl(dungeon.Image)
                    .AddField("Info", $"{(dungeon.IsDefault ? "Default " : "")}{(dungeon.IsOneTimeOnly ? "<:dungeonkey:606237382047694919> Dungeon" : "<:mapopen:606236181503410176> Town")} for up to {dungeon.MaxPlayer} {(dungeon.MaxPlayer == 1 ? "player" : "players")}. {dungeon.Matchups.Count()} stages.")
                    .AddField("Requirement", $"{dungeon.Requirement.GetDescription()}")
                    .AddField("Djinn", $"{(djinnTotal.Count() > 0 ? $"{djinnobtained}/{limittedDjinn.Sum(d => d.Obtainable)}{(unlimittedDjinn.Count() > 0 ? "+" : "")} ({probability * 100:N0}% success rate)" : "none")}")
                    .Build());
                await Task.CompletedTask;
            }
        }

        [Command("c setup"), Alias("colosso setup")]
        [RequireStaff]
        [RequireUserServer]
        public async Task SetupColosso()
        {
            await Context.Message.DeleteAsync();
            _ = Setup(Context.Guild);
        }

        public static async Task Setup(SocketGuild guild)
        {
            battles.Where(b => guild.Channels.Any(c => b.GetChannelIds.Contains(c.Id))).ToList().ForEach(old => old.Dispose());
            var gs = GuildSettings.GetGuildSettings(guild);
            battles.Add(new SingleBattleEnvironment("Wilds", gs.ColossoChannel, true, await PrepareBattleChannel("Weyard-Wilds", guild), BattleDifficulty.Easy));
            battles.Add(new SingleBattleEnvironment("Woods", gs.ColossoChannel, true, await PrepareBattleChannel("Weyard-Woods", guild), BattleDifficulty.Medium));
            //battles.Add(new SingleBattleEnvironment("Wealds", LobbyChannel, await PrepareBattleChannel("Weyard-Wealds"), BattleDifficulty.Hard));

            battles.Add(new EndlessBattleEnvironment("Endless", gs.ColossoChannel, true, await PrepareBattleChannel("Endless-Encounters", guild)));

            //battles.Add(new GauntletBattleEnvironment("Dungeon", LobbyChannel, await PrepareBattleChannel("deep-dungeon"), "Vale"));
            //battles.Add(new GauntletBattleEnvironment("Catabombs", LobbyChannel, await PrepareBattleChannel("chilly-catacombs"), "Vale"));
            battles.Add(new TeamBattleEnvironment("PvP", gs.ColossoChannel, false, await PrepareBattleChannel("PvP-A", guild, RoomVisibility.All), await PrepareBattleChannel("PvP-B", guild, RoomVisibility.TeamB), gs.TeamBRole));

            //battles.Add(new SingleBattleEnvironment("Gold", LobbyChannel, await PrepareBattleChannel("Gold"), BattleDifficulty.Hard));
            //battles.Add(new TeamBattleManager("OneVOne", LobbyChannel, await PrepareBattleChannel("OneVOneA", PermValue.Deny), await PrepareBattleChannel("OneVOneB", PermValue.Allow), 1));

            if (Global.Client.Activity == null)
            {
                await Global.Client.SetGameAsync("in Babi's Palace.", "https://www.twitch.tv/directory/game/Golden%20Sun", ActivityType.Streaming);
            }
            AcceptBattles = true;
        }

        [Command("c reset")]
        [RequireStaff]
        [RequireUserServer]
        public async Task Reset(string name)
        {
            var a = battles.Where(b => Context.Guild.Channels.Any(c => b.GetChannelIds.Contains(c.Id))
                && b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();
            if (a != null)
            {
                _ = a.Reset();
                await Context.Message.DeleteAsync();
            }
        }

        [Command("c reset")]
        [RequireStaff]
        [RequireUserServer]
        public async Task Reset(ulong id)
        {
            var a = battles.OfType<PvEEnvironment>().Where(b => Context.Guild.Channels.Any(c => b.GetChannelIds.Contains(c.Id))
                && b.BattleChannel.Id == id)
                .FirstOrDefault();
            if (a != null)
            {
                _ = a.Reset();
                await Context.Message.DeleteAsync();
            }
        }

        [Command("c setEnemy")]
        [RequireStaff]
        [RequireUserServer]
        public async Task SetEnemy(string name, [Remainder] string enemy)
        {
            await Context.Message.DeleteAsync();
            var a = battles.OfType<PvEEnvironment>()
                .Where(b => b.BattleChannel.GuildId == Context.Guild.Id 
                && b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();
            if (a != null)
            {
                a.SetEnemy(enemy);
                //_ = a.Reset();
            }
        }

        [Command("endless")]
        [Summary("Prepare a channel for an endless gamemode. 'Legacy' will be without djinn. Endless unlocks at level 50 or once you completed the Colosso Finals!")]
        [RequireUserServer]
        public async Task ColossoEndless(EndlessMode mode = EndlessMode.Default)
        {
            if (!AcceptBattles) return;
            if (!(Context.User is SocketGuildUser user)) return;
            var guild = Context.Guild;
            var gs = GuildSettings.GetGuildSettings(guild);
            _ = RemoveFighterRoles();
            var gauntletFromUser = battles.Where(b => 
                Context.Guild.Channels.Any(c => b.GetChannelIds.Contains(c.Id)) &&
                b.Name.Equals(Context.User.Username, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
            var acc = UserAccounts.GetAccount(Context.User);
            if (acc.LevelNumber < 50 && !acc.Tags.Contains("ColossoCompleted")) return;

            if (gauntletFromUser != null)
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
            PvEEnvironment openBattle;
            if (mode == EndlessMode.Default)
            {
                openBattle = new EndlessBattleEnvironment($"{Context.User.Username}", gs.ColossoChannel, false, await PrepareBattleChannel($"Endless-{Context.User.Username}", guild, persistent: false));
            }
            else
            {
                openBattle = new EndlessBattleEnvironment($"{Context.User.Username}", gs.ColossoChannel, false, await PrepareBattleChannel($"Endless-Legacy-{Context.User.Username}", guild, persistent:false), EndlessMode.Legacy);
            }
            battles.Add(openBattle);
            _ = Context.Channel.SendMessageAsync($"{Context.User.Username}, {openBattle.BattleChannel.Mention} has been prepared for an endless adventure!");

            _ = AddFighterRole();
            await Task.CompletedTask;

        }

        [Command("dungeon"), Alias("dg")]
        [Summary("Prepare a channel for an adventure to a specified dungeon")]
        [RequireUserServer]
        public async Task Dungeon([Remainder] string DungeonName)
        { _ = SetupDungeon(DungeonName, false); await Task.CompletedTask; }

        [Command("Tutorial")]
        [Summary("Enter the Tutorial and start your adventure!")]
        [RequireUserServer]
        public async Task Tutorial()
        { _ = SetupDungeon("Tutorial", false); await Task.CompletedTask; }

        [Command("moddungeon")]
        [RequireStaff]
        [RequireUserServer]
        public async Task ModDungeon([Remainder] string DungeonName)
        { _ = SetupDungeon(DungeonName, true); await Task.CompletedTask; }

        [Command("alldungeons")]
        [RequireStaff]
        public async Task AllDungeon()
        {
            await ReplyAsync(string.Join("\n", EnemiesDatabase.dungeons.Values.Select(d => d.Name)));
        }

        [Command("givedungeon")]
        [RequireStaff]
        public async Task GiveDungeon(SocketGuildUser user, [Remainder] string dungeonName)
        {
            if (EnemiesDatabase.TryGetDungeon(dungeonName, out var dungeon))
            {
                UserAccounts.GetAccount(user).Dungeons.Add(dungeon.Name);
                _ = ReplyAsync($"{user.DisplayName()} got access to {dungeon.Name}");
            }
            await Task.CompletedTask;
        }

        [Command("c status")]
        [RequireStaff]
        [RequireUserServer]
        public async Task StatusOfBattle(string name = "")
        {
            await Context.Message.DeleteAsync();
            var a = battles.Where(b => Context.Guild.Channels.Any(c => b.GetChannelIds.Contains(c.Id))
                && b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();
            if (a != null)
            {
                await Context.Channel.SendMessageAsync(a.GetStatus());
            }
            if(name == "")
            {
                await ReplyAsync(string.Join("\n", 
                    battles.Where(b => Context.Guild.Channels.Any(c => b.GetChannelIds.Contains(c.Id)))
                    .Select(b => $"Name: {b.Name} ({b.GetType()}) Ids:{b.GetChannelIds[0]} Active:{b.IsActive} Permanent:{b.IsPersistent}")));
            }
        }

        [Command("c status")]
        [RequireStaff]
        [RequireUserServer]
        public async Task StatusOfBattle(ulong id)
        {
            await Context.Message.DeleteAsync();
            var a = battles.OfType<PvEEnvironment>().Where(b => Context.Guild.Channels.Any(c => b.GetChannelIds.Contains(c.Id))
                && b.BattleChannel.Id == id)
                .FirstOrDefault();
            if (a != null)
            {
                await Context.Channel.SendMessageAsync(a.GetStatus());
            }
        }

        [Command("c AcceptBattles")]
        [RequireStaff]
        [RequireUserServer]
        public async Task SetAcceptBattles(bool acceptBattles)
        {
            AcceptBattles = acceptBattles;
            await ReplyAsync($"AcceptBattles: {acceptBattles}");
        }

        [Command("train")]
        [Cooldown(15)]
        [Summary("Prove your strength by battling a random opponent in Colosso")]
        [RequireUserServer]
        public async Task ColossoTrain()
        {
            await ReplyAsync(embed: Colosso.ColossoTrain((SocketGuildUser)Context.User, Context.Channel));
        }

        private static async Task<ITextChannel> PrepareBattleChannel(string Name, SocketGuild guild, RoomVisibility visibility = RoomVisibility.All, bool persistent = false)
        {
            var gs = GuildSettings.GetGuildSettings(guild);
            var colossoChannel = gs.ColossoChannel;
            var categoryID = persistent ? colossoChannel.Id : gs.CustomBattlesCateogry?.Id ?? colossoChannel.Id;
            var teamB = gs.TeamBRole;
            var channel = await guild.GetOrCreateTextChannelAsync(Name, d => { d.CategoryId = categoryID; d.Position = colossoChannel.Position + battles.Count(); });
            try
            {
                await channel.SyncPermissionsAsync();
            } catch (Discord.Net.HttpException e)
            {
                channel = await guild.GetOrCreateTextChannelAsync($"{Name}1", d => { d.CategoryId = categoryID; d.Position = colossoChannel.Position + battles.Count(); });
                await channel.SyncPermissionsAsync();
            }

            if (visibility == RoomVisibility.TeamB)
            {
                await channel.AddPermissionOverwriteAsync(teamB, new OverwritePermissions(viewChannel: PermValue.Allow));
                await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(viewChannel: PermValue.Deny));
            }

            if (visibility == RoomVisibility.TeamA)
            {
                await channel.AddPermissionOverwriteAsync(teamB, new OverwritePermissions(viewChannel: PermValue.Deny));
            }

            if (visibility == RoomVisibility.Private)
            {
                await channel.AddPermissionOverwriteAsync(teamB, new OverwritePermissions(viewChannel: PermValue.Deny));
                await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(viewChannel: PermValue.Deny));
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