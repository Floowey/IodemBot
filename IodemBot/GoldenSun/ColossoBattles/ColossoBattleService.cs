using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using IodemBot.Core;
using IodemBot.Core.UserManagement;
using Microsoft.Extensions.DependencyInjection;

namespace IodemBot.ColossoBattles
{
    public class ColossoBattleService
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly List<BattleEnvironment> _battles = new();
        private readonly Dictionary<SocketGuildUser, DateTime> _fighterRoles = new();

        public bool AcceptBattles = true;

        public ColossoBattleService(IServiceProvider services)
        {
            _services = services;
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _client.Ready += _client_Ready;
        }

        private int NumberOfBattles => _battles.Count;

        public ulong[] ChannelIds => _battles.Select(b => b.ChannelIds).SelectMany(item => item).Distinct().ToArray();

        private async Task _client_Ready()
        {
            _client.Ready -= _client_Ready;
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                foreach (var guild in _client.Guilds)
                {
                    await Task.Delay(1000);
                    var gs = GuildSettings.GetGuildSettings(guild);
                    if (gs.AutoSetup && gs.ColossoChannel != null)
                    {
                        await SetupInGuild(guild);
                        Console.WriteLine($"Setup in {gs.Name}");
                    }
                }
            });
            await Task.CompletedTask;
        }

        public BattleEnvironment GetBattleEnvironment(IMessageChannel channel)
        {
            return _battles.FirstOrDefault(c => c.ChannelIds.Contains(channel.Id));
        }

        public TEnvironment GetBattleEnvironment<TEnvironment>(Func<TEnvironment, bool> filter)
            where TEnvironment : BattleEnvironment
        {
            return _battles.OfType<TEnvironment>().FirstOrDefault(filter);
        }

        public BattleEnvironment GetBattleEnvironment(Func<BattleEnvironment, bool> filter)
        {
            return _battles.FirstOrDefault(filter);
        }

        public IReadOnlyList<BattleEnvironment> GetAllBattleEnvironments()
        {
            return _battles.AsReadOnly();
        }

        public PlayerFighter GetPlayer(ITextChannel channel, IUser user)
        {
            return GetBattleEnvironment(channel).GetPlayer(user.Id);
        }

        public void AddBattleEnvironment(BattleEnvironment battleEnvironment)
        {
            _battles.Add(battleEnvironment);
        }

        public bool UserInBattle(UserAccount player)
        {
            return UserInBattle(player.Id);
        }

        public bool UserInBattle(ulong playerId)
        {
            return _battles.Any(s => s.ContainsPlayer(playerId));
        }

        public async Task SetupInGuild(SocketGuild guild)
        {
            _battles.Where(b => guild.Channels.Any(c => b.ChannelIds.Contains(c.Id))).ToList()
                .ForEach(old => old.Dispose());
            var gs = GuildSettings.GetGuildSettings(guild);
            _battles.Add(new SingleBattleEnvironment(this, "Wilds", gs.ColossoChannel, true,
                await PrepareBattleChannel("Weyard-Wilds", guild, persistent: true), BattleDifficulty.Easy));
            _battles.Add(new SingleBattleEnvironment(this, "Woods", gs.ColossoChannel, true,
                await PrepareBattleChannel("Weyard-Woods", guild, persistent: true), BattleDifficulty.Medium));
            _battles.Add(new SingleBattleEnvironment(this, "Weards", gs.ColossoChannel, true,
                await PrepareBattleChannel("Weyard-Weards", guild, persistent: true), BattleDifficulty.Hard));

            _battles.Add(new EndlessBattleEnvironment(this, "Endless", gs.ColossoChannel, true,
                await PrepareBattleChannel("Endless-Encounters", guild, persistent: true)));

            _battles.Add(new TeamBattleEnvironment(this, "PvP", gs.ColossoChannel, false,
                await PrepareBattleChannel("PvP-A", guild, RoomVisibility.All, true),
                await PrepareBattleChannel("PvP-B", guild, RoomVisibility.TeamB, true), gs.TeamBRole));

            //battles.Add(new SingleBattleEnvironment("Gold", LobbyChannel, await PrepareBattleChannel("Gold"), BattleDifficulty.Hard));
            //battles.Add(new TeamBattleManager("OneVOne", LobbyChannel, await PrepareBattleChannel("OneVOneA", PermValue.Deny), await PrepareBattleChannel("OneVOneB", PermValue.Allow), 1));

            if (Global.Client.Activity == null)
            {
#if DEBUG
                await Global.Client.SetGameAsync("in Babi's Palace.",
                  "https://www.twitch.tv/directory/game/Golden%20Sun", ActivityType.Streaming);
#else
                await Global.Client.SetGameAsync("in Babi's Palace.",
                    "https://www.twitch.tv/directory/game/Golden%20Sun", ActivityType.Streaming);
#endif
            }
            AcceptBattles = true;
        }

        internal void RemoveBattleEnvironment(BattleEnvironment battleEnvironment)
        {
            _battles.Remove(battleEnvironment);
        }

        public async Task<ITextChannel> PrepareBattleChannel(string name, SocketGuild guild,
            RoomVisibility visibility = RoomVisibility.All, bool persistent = false)
        {
            var gs = GuildSettings.GetGuildSettings(guild);
            var colossoChannel = gs.ColossoChannel;
            var categoryId = persistent
                ? colossoChannel.CategoryId
                : gs.CustomBattlesCateogry?.Id ?? colossoChannel.CategoryId;
            var teamB = gs.TeamBRole;
            var channel = await guild.GetOrCreateTextChannelAsync(name, d =>
            {
                d.CategoryId = categoryId;
                d.Position = colossoChannel.Position + _battles.Count;
            });
            try
            {
                await channel.SyncPermissionsAsync();
            }
            catch (HttpException)
            {
                channel = await guild.GetOrCreateTextChannelAsync($"{name}1", d =>
                {
                    d.CategoryId = categoryId;
                    d.Position = colossoChannel.Position + _battles.Count;
                });
                await channel.SyncPermissionsAsync();
            }

            switch (visibility)
            {
                case RoomVisibility.All:
                    break;

                case RoomVisibility.TeamA:
                    _ = channel.AddPermissionOverwriteAsync(teamB,
                        new OverwritePermissions(viewChannel: PermValue.Deny));
                    break;

                case RoomVisibility.TeamB:
                    _ = channel.AddPermissionOverwriteAsync(teamB,
                        new OverwritePermissions(viewChannel: PermValue.Allow));
                    _ = channel.AddPermissionOverwriteAsync(guild.EveryoneRole,
                        new OverwritePermissions(viewChannel: PermValue.Deny, sendMessages: PermValue.Deny));
                    break;

                case RoomVisibility.Private:
                    _ = channel.AddPermissionOverwriteAsync(teamB,
                        new OverwritePermissions(viewChannel: PermValue.Deny));
                    _ = channel.AddPermissionOverwriteAsync(guild.EveryoneRole,
                        new OverwritePermissions(viewChannel: PermValue.Deny));
                    break;
            }

            var messages = await channel.GetMessagesAsync().FlattenAsync();
            await channel.DeleteMessagesAsync(messages.Where(m => m.Timestamp.AddDays(14) > DateTime.Now));
            return channel;
        }
    }
}