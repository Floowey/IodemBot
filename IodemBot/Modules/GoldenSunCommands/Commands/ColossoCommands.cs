using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Core;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules;
using IodemBot.Modules.GoldenSunMechanics;
using IodemBot.Preconditions;

namespace IodemBot.ColossoBattles
{
    [Name("Colosso")]
    public class ColossoCommands : ModuleBase<SocketCommandContext>
    {
        public ColossoBattleService BattleService { get; set; }

        public static readonly string[] NumberEmotes = new string[] { "\u0030\u20E3", "\u0031\u20E3", "\u0032\u20E3", "\u0033\u20E3", "\u0034\u20E3", "\u0035\u20E3",
            "\u0036\u20E3", "\u0037\u20E3", "\u0038\u20E3", "\u0039\u20E3" };

        private static readonly Dictionary<SocketGuildUser, DateTime> FighterRoles = new();

        public async Task SetupDungeon(string dungeonName, bool modPermission = false)
        {
            if (!BattleService.AcceptBattles)
            {
                return;
            }
            if (Context.User is not SocketGuildUser user)
            {
                return;
            }

            var acc = EntityConverter.ConvertUser(Context.User);
            var gs = GuildSettings.GetGuildSettings(Context.Guild);
            _ = RemoveFighterRoles();
            if (EnemiesDatabase.TryGetDungeon(dungeonName, out var dungeon))
            {
                if (!acc.Dungeons.Contains(dungeon.Name) && !dungeon.IsDefault && !modPermission)
                {
                    await ReplyAsync("If you can't tell me where this place is, I can't take you there. And even if you knew, they probably wouldn't let you in! Bring me a map or show to me that you have the key to enter.");
                    return;
                }

                if (!dungeon.Requirement.Applies(acc) && !modPermission)
                {
                    await ReplyAsync("I'm afraid that I can't take you to this place, it is too dangerous for you and me both.");
                    return;
                }

                var openBattle = BattleService.GetBattleEnvironment<GauntletBattleEnvironment>(b => b.IsReady && b.IsPersistent);
                if (openBattle == null)
                {
                    var gauntletFromUser = BattleService.GetBattleEnvironment<GauntletBattleEnvironment>(b => b.Name.Equals(Context.User.Username, StringComparison.CurrentCultureIgnoreCase));
                    if (gauntletFromUser != null)
                    {
                        if (gauntletFromUser.IsActive)
                        {
                            await ReplyAsync("What? You already are on an adventure!");
                            Console.WriteLine($"User Active in: {gauntletFromUser.Name}; {gauntletFromUser.BattleChannel.Id}");
                            return;
                        }
                        else
                        {
                            _ = gauntletFromUser.Reset($"{gauntletFromUser.Name} overridden");
                            //battles.Remove(gauntletFromUser);
                        }
                    }
                    openBattle = new GauntletBattleEnvironment(BattleService, $"{Context.User.Username}", GuildSettings.GetGuildSettings(Context.Guild).ColossoChannel, await BattleService.PrepareBattleChannel($"{dungeon.Name}-{Context.User.Username}", Context.Guild, persistent: false), dungeon.Name, false);

                    BattleService.AddBattleEnvironment(openBattle);
                }
                else
                {
                    openBattle.SetEnemy(dungeonName);
                }

                if (dungeon.IsOneTimeOnly && !modPermission)
                {
                    acc.Dungeons.Remove(dungeon.Name);
                    UserAccountProvider.StoreUser(acc);
                }
                _ = Context.Channel.SendMessageAsync($"{Context.User.Username}, {openBattle.BattleChannel.Mention} has been prepared for your adventure to {dungeon.Name}");

                _ = AddFighterRole();
            }
            else
            {
                await ReplyAsync("I don't know where that place is.");
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
                    if (entry.Key.Roles.Any(r => r.Id == gs.FighterRole.Id))
                    {
                        _ = entry.Key.RemoveRoleAsync(gs.FighterRole);
                    }

                    toBeRemoved.Add(entry.Key);
                }
            }
            toBeRemoved.ForEach(d => FighterRoles.Remove(d));
            await Task.CompletedTask;
        }

        private async Task AddFighterRole()
        {
            var gs = GuildSettings.GetGuildSettings(Context.Guild);
            if (Context.User is not SocketGuildUser user)
            {
                return;
            }

            if (!user.Roles.Any(r => r.Id == gs.FighterRole.Id) && !BattleService.UserInBattle(user.Id))
            {
                _ = user.AddRoleAsync(gs.FighterRole);
                FighterRoles.Add(user, DateTime.Now);
            }
            else
            {
                if (FighterRoles.Remove(user))
                {
                    FighterRoles.Add(user, DateTime.Now);
                }
            }
            await Task.CompletedTask;
        }

        [Command("DungeonInfo"), Alias("dgi")]
        [Summary("Get information about a dungeon")]
        public async Task DungeonInfo([Remainder] string dungeonName)
        {
            if (EnemiesDatabase.TryGetDungeon(dungeonName, out var dungeon))
            {
                var rewardTablesWithDjinn = dungeon.Matchups
                    .SelectMany(m => m.RewardTables.Where(t => t.OfType<DefaultReward>().Any(r => r.Djinn != "")));

                var djinnTotal = rewardTablesWithDjinn.SelectMany(t => t.OfType<DefaultReward>().Where(r => r.Djinn != ""));

                var limittedDjinn = djinnTotal.GroupBy(d => d.Tag)
                    .Select(k => k.OrderByDescending(d => d.Obtainable).First());

                var unlimittedDjinn = djinnTotal.Where(d => d.Obtainable == 0);

                var avatar = EntityConverter.ConvertUser(Context.User);
                var djinnobtained = avatar.Tags.Count(t => limittedDjinn.Any(r => r.Tag.Contains(t)));

                var probability = djinnTotal.Any() ? 1 - 1.0 / rewardTablesWithDjinn
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
                    .AddField("Info", $"{(dungeon.IsDefault ? "Default " : "")}{(dungeon.IsOneTimeOnly ? "<:dungeonkey:606237382047694919> Dungeon" : "<:mapopen:606236181503410176> Town")} for up to {dungeon.MaxPlayer} {(dungeon.MaxPlayer == 1 ? "player" : "players")}. {dungeon.Matchups.Count} stages.")
                    .AddField("Requirement", $"{dungeon.Requirement.GetDescription()}")
                    .AddField("Djinn", $"{(djinnTotal.Any() ? $"{djinnobtained}/{limittedDjinn.Sum(d => d.Obtainable)}{(unlimittedDjinn.Any() ? "+" : "")} ({probability * 100:N0}% success rate)" : "none")}")
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
            _ = BattleService.SetupInGuild(Context.Guild);
        }

        [Command("c reset")]
        [RequireStaff]
        [RequireUserServer]
        public async Task Reset(string name)
        {
            var a = BattleService.GetBattleEnvironment(b => Context.Guild.Channels.Any(c => b.ChannelIds.Contains(c.Id))
                && b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (a != null)
                _ = a.Reset("manual reset");

            await Task.CompletedTask;
        }

        [Command("c reset")]
        [RequireStaff]
        [RequireUserServer]
        public async Task Reset(IMessageChannel channel)
        {
            _ = Reset(channel.Id);
            await Task.CompletedTask;
        }

        [Command("c reset")]
        [RequireStaff]
        [RequireUserServer]
        public async Task Reset(ulong id)
        {
            var a = BattleService.GetBattleEnvironment(b => b.ChannelIds.Contains(id));
            if (a != null)
                _ = a.Reset("manual reset");

            await Task.CompletedTask;
        }

        [Command("c setEnemy")]
        [RequireStaff]
        [RequireUserServer]
        public async Task SetEnemy(string name, [Remainder] string enemy)
        {
            await Context.Message.DeleteAsync();
            var a = BattleService.GetBattleEnvironment(b => b.ChannelIds.Contains(Context.Channel.Id)
                && b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (a is PvEEnvironment pve)
                pve.SetEnemy(enemy);
        }

        [Command("modendless")]
        [RequireStaff]
        public async Task ModColossoEndless(int round = 1)
        {
            if (!BattleService.AcceptBattles)
            {
                return;
            }

            if (Context.User is not SocketGuildUser)
            {
                return;
            }

            var guild = Context.Guild;
            var gs = GuildSettings.GetGuildSettings(guild);
            _ = RemoveFighterRoles();

            var openBattle = new EndlessBattleEnvironment(BattleService, $"{Context.User.Username}", gs.ColossoChannel, false, await BattleService.PrepareBattleChannel($"Endless-{Context.User.Username}", guild, persistent: false));
            openBattle.SetStreak(round);

            BattleService.AddBattleEnvironment(openBattle);
            _ = Context.Channel.SendMessageAsync($"{Context.User.Username}, {openBattle.BattleChannel.Mention} has been prepared for an endless adventure!");

            _ = AddFighterRole();
            await Task.CompletedTask;
        }

        [Command("modgoliath")]
        [RequireStaff]
        public async Task ModGoliathBattle()
        {
            if (!BattleService.AcceptBattles)
            {
                return;
            }

            if (Context.User is not SocketGuildUser)
            {
                return;
            }

            var guild = Context.Guild;
            var gs = GuildSettings.GetGuildSettings(guild);
            _ = RemoveFighterRoles();

            var openBattle = new GoliathBattleEnvironment(BattleService,
                $"Goliath-{Context.User.Username}", gs.ColossoChannel, false,
               await BattleService.PrepareBattleChannel("Goliath-B", guild, RoomVisibility.All, true),
                await BattleService.PrepareBattleChannel("Goliath-A", guild, RoomVisibility.TeamB, true), gs.TeamBRole);

            BattleService.AddBattleEnvironment(openBattle);
            _ = Context.Channel.SendMessageAsync($"Goliath Battle Ready.");
            await Task.CompletedTask;
        }

        public enum FastTrackOption
        { SlowTrack, FastTrack };

        [Command("endless")]
        [Summary("Prepare a channel for an endless gamemode. 'Legacy' will be without djinn. Endless unlocks at level 50 or once you completed the Colosso Finals! Using `i!endless default true` will let you skip ahead to round 13 for a fee of 10.000 coins")]
        [RequireUserServer]
        public async Task ColossoEndless(EndlessMode mode = EndlessMode.Default, FastTrackOption fastTrackOption = FastTrackOption.SlowTrack)
        {
            if (!BattleService.AcceptBattles)
            {
                return;
            }

            if (Context.User is not SocketGuildUser user)
            {
                return;
            }

            var guild = Context.Guild;
            var gs = GuildSettings.GetGuildSettings(guild);
            _ = RemoveFighterRoles();
            var endlessFromUser = BattleService.GetBattleEnvironment(b =>
                Context.Guild.Channels.Any(c => b.ChannelIds.Contains(c.Id)) &&
                b.Name.Equals(Context.User.Username, StringComparison.CurrentCultureIgnoreCase));
            var acc = EntityConverter.ConvertUser(Context.User);
            if (acc.LevelNumber < 50 && !acc.Tags.Contains("ColossoCompleted"))
            {
                return;
            }

            if (endlessFromUser != null)
            {
                if (endlessFromUser.IsActive)
                {
                    await ReplyAsync("What? You already are on an adventure!");
                    return;
                }
                else
                {
                    await endlessFromUser.Reset("Battle override");
                    //battles.Remove(endlessFromUser);
                }
            }
            EndlessBattleEnvironment openBattle;
            if (mode == EndlessMode.Default)
            {
                openBattle = new EndlessBattleEnvironment(BattleService, $"{Context.User.Username}", gs.ColossoChannel, false, await BattleService.PrepareBattleChannel($"Endless-{Context.User.Username}", guild, persistent: false));
                if (fastTrackOption == FastTrackOption.FastTrack && acc.Inv.RemoveBalance(10000))
                {
                    UserAccountProvider.StoreUser(acc);
                    openBattle.SetStreak(12);
                }
            }
            else
            {
                openBattle = new EndlessBattleEnvironment(BattleService, $"{Context.User.Username}", gs.ColossoChannel, false, await BattleService.PrepareBattleChannel($"Endless-Legacy-{Context.User.Username}", guild, persistent: false), EndlessMode.Legacy);
            }
            BattleService.AddBattleEnvironment(openBattle);
            _ = Context.Channel.SendMessageAsync($"{Context.User.Username}, {openBattle.BattleChannel.Mention} has been prepared for an endless adventure!");

            _ = AddFighterRole();
            await Task.CompletedTask;
        }

        [Command("dungeon"), Alias("dg")]
        [Summary("Prepare a channel for an adventure to a specified dungeon")]
        [RequireUserServer]
        public async Task Dungeon([Remainder] string dungeonName)
        { _ = SetupDungeon(dungeonName, false); await Task.CompletedTask; }

        [Command("Tutorial")]
        [Summary("Enter the Tutorial and start your adventure!")]
        [RequireUserServer]
        public async Task Tutorial()
        { _ = SetupDungeon("Tutorial", false); await Task.CompletedTask; }

        [Command("moddungeon")]
        [RequireStaff]
        [RequireUserServer]
        public async Task ModDungeon([Remainder] string dungeonName)
        { _ = SetupDungeon(dungeonName, true); await Task.CompletedTask; }

        [Command("alldungeons")]
        [RequireStaff]
        public async Task AllDungeon()
        {
            await ReplyAsync(string.Join("\n", EnemiesDatabase.Dungeons.Values.Select(d => d.Name)));
        }

        [Command("givedungeon")]
        [RequireStaff]
        public async Task GiveDungeon(SocketGuildUser user, [Remainder] string dungeonName)
        {
            if (EnemiesDatabase.TryGetDungeon(dungeonName, out var dungeon))
            {
                var acc = EntityConverter.ConvertUser(user);
                acc.Dungeons.Add(dungeon.Name);
                UserAccountProvider.StoreUser(acc);
                _ = ReplyAsync($"{user.DisplayName()} got access to {dungeon.Name}");
            }
            await Task.CompletedTask;
        }

        [Command("c status")]
        [RequireStaff]
        [RequireUserServer]
        public async Task StatusOfBattle(string name = "")
        {
            var a = BattleService.GetBattleEnvironment(b => Context.Guild.Channels.Any(c => b.ChannelIds.Contains(c.Id))
                && b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (a != null)
            {
                await Context.Channel.SendMessageAsync(a.GetStatus());
            }
            if (name == "")
            {
                EmbedBuilder embed = new();
                foreach (var b in BattleService.GetAllBattleEnvironments())
                {
                    embed.AddField($"{b.Name} {(b.IsPersistent ? "(Permanent)" : "")}",
                    $"{b.GetType().Name}\n" +
                    $"Channels:{string.Join(",", b.ChannelIds.Select(id => $"<#{id}>"))}");
                }

                if (embed.Fields.Count == 0)
                    embed.WithDescription("No registered battles.");
                await ReplyAsync(embed: embed.Build());
            }
        }

        [Command("c status")]
        [RequireStaff]
        [RequireUserServer]
        public async Task StatusOfBattle(ulong id)
        {
            var a = BattleService.GetBattleEnvironment(b => b.ChannelIds.Contains(id));
            if (a != null)
            {
                await Context.Channel.SendMessageAsync(a.GetStatus());
            }
        }

        [Command("c status")]
        [RequireStaff]
        [RequireUserServer]
        public async Task StatusOfBattle(IMessageChannel channel)
        {
            _ = StatusOfBattle(channel.Id);
            await Task.CompletedTask;
        }

        [Command("c AcceptBattles")]
        [RequireStaff]
        [RequireUserServer]
        public async Task SetAcceptBattles(bool acceptBattles)
        {
            BattleService.AcceptBattles = acceptBattles;
            await ReplyAsync($"AcceptBattles: {acceptBattles}");
        }

        [Command("train")]
        [Cooldown(15)]
        [Summary("Prove your strength by battling a random opponent in Colosso")]
        [RequireUserServer]
        public async Task ColossoTrain()
        {
            await ReplyAsync(embed: RandomColossoBattlesCommands.ColossoTrain((SocketGuildUser)Context.User, Context.Channel));
        }
    }
}