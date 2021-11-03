using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public bool AcceptBattles = true;
        private readonly List<BattleEnvironment> battles = new List<BattleEnvironment>();
        private readonly Dictionary<SocketGuildUser, DateTime> FighterRoles = new Dictionary<SocketGuildUser, DateTime>();
        private int NumberOfBattles => battles.Count;
        public ColossoBattleService(IServiceProvider services)
        {
            _services = services;
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _client.Ready += _client_Ready;
        }

        private async Task _client_Ready()
        {
            _client.Ready -= _client_Ready;
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
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

        public ulong[] ChannelIds
        {
            get => battles.Select(b => b.ChannelIds).SelectMany(item => item).Distinct().ToArray();
        }

        public BattleEnvironment GetBattleEnvironment(IMessageChannel channel)
        {
            return battles.FirstOrDefault(c => c.ChannelIds.Contains(channel.Id));
        }

        public TEnvironment GetBattleEnvironment<TEnvironment>(Func<TEnvironment, bool> filter) where TEnvironment : BattleEnvironment
        {
            return battles.OfType<TEnvironment>().FirstOrDefault(filter);
        }

        public BattleEnvironment GetBattleEnvironment(Func<BattleEnvironment, bool> filter)
        {
            return battles.FirstOrDefault(filter);
        }

        public IReadOnlyList<BattleEnvironment> GetAllBattleEnvironments()
        {
            return battles.AsReadOnly();
        }

        public PlayerFighter GetPlayer(ITextChannel channel, IUser user)
        {
            return GetBattleEnvironment(channel).GetPlayer(user.Id);
        }

        public void AddBattleEnvironment(BattleEnvironment battleEnvironment)
        {
            battles.Add(battleEnvironment);
        }

        public bool UserInBattle(UserAccount player)
        {
            return UserInBattle(player.ID);
        }

        public bool UserInBattle(ulong playerID)
        {
            return battles.Any(s => s.ContainsPlayer(playerID));
        }

        public async Task SetupInGuild(SocketGuild guild)
        {
            battles.Where(b => guild.Channels.Any(c => b.ChannelIds.Contains(c.Id))).ToList().ForEach(old => old.Dispose());
            var gs = GuildSettings.GetGuildSettings(guild);
            battles.Add(new SingleBattleEnvironment(this, "Wilds", gs.ColossoChannel, true, await PrepareBattleChannel("Weyard-Wilds", guild, persistent: true), BattleDifficulty.Easy));
            battles.Add(new SingleBattleEnvironment(this, "Woods", gs.ColossoChannel, true, await PrepareBattleChannel("Weyard-Woods", guild, persistent: true), BattleDifficulty.Medium));
            //battles.Add(new SingleBattleEnvironment("Wealds", LobbyChannel, await PrepareBattleChannel("Weyard-Wealds"), BattleDifficulty.Hard));

            battles.Add(new EndlessBattleEnvironment(this, "Endless", gs.ColossoChannel, true, await PrepareBattleChannel("Endless-Encounters", guild, persistent: true)));

            //battles.Add(new GauntletBattleEnvironment("Dungeon", LobbyChannel, await PrepareBattleChannel("deep-dungeon"), "Vale"));
            //battles.Add(new GauntletBattleEnvironment("Catabombs", LobbyChannel, await PrepareBattleChannel("chilly-catacombs"), "Vale"));
            battles.Add(new TeamBattleEnvironment(this, "PvP", gs.ColossoChannel, false, await PrepareBattleChannel("PvP-A", guild, RoomVisibility.All, persistent: true), await PrepareBattleChannel("PvP-B", guild, RoomVisibility.TeamB, true), gs.TeamBRole));

            //battles.Add(new SingleBattleEnvironment("Gold", LobbyChannel, await PrepareBattleChannel("Gold"), BattleDifficulty.Hard));
            //battles.Add(new TeamBattleManager("OneVOne", LobbyChannel, await PrepareBattleChannel("OneVOneA", PermValue.Deny), await PrepareBattleChannel("OneVOneB", PermValue.Allow), 1));

            if (Global.Client.Activity == null)
            {
                await Global.Client.SetGameAsync("in Babi's Palace.", "https://www.twitch.tv/directory/game/Golden%20Sun", ActivityType.Streaming);
            }
            AcceptBattles = true;
        }

        internal void RemoveBattleEnvironment(BattleEnvironment battleEnvironment)
        {
            battles.Remove(battleEnvironment);
        }

        public async Task<ITextChannel> PrepareBattleChannel(string Name, SocketGuild guild, RoomVisibility visibility = RoomVisibility.All, bool persistent = false)
        {
            var gs = GuildSettings.GetGuildSettings(guild);
            var colossoChannel = gs.ColossoChannel;
            var categoryID = persistent ? colossoChannel.CategoryId : (gs.CustomBattlesCateogry?.Id ?? colossoChannel.CategoryId);
            var teamB = gs.TeamBRole;
            var channel = await guild.GetOrCreateTextChannelAsync(Name, d => { d.CategoryId = categoryID; d.Position = colossoChannel.Position + battles.Count; });
            try
            {
                await channel.SyncPermissionsAsync();
            }
            catch (HttpException)
            {
                channel = await guild.GetOrCreateTextChannelAsync($"{Name}1", d => { d.CategoryId = categoryID; d.Position = colossoChannel.Position + battles.Count; });
                await channel.SyncPermissionsAsync();
            }

            switch (visibility)
            {
                case RoomVisibility.All:
                    break;
                case RoomVisibility.TeamA:
                    _ = channel.AddPermissionOverwriteAsync(teamB, new OverwritePermissions(viewChannel: PermValue.Deny));
                    break;
                case RoomVisibility.TeamB:
                    _ = channel.AddPermissionOverwriteAsync(teamB, new OverwritePermissions(viewChannel: PermValue.Allow));
                    _ = channel.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(viewChannel: PermValue.Deny, sendMessages: PermValue.Deny));
                    break;
                case RoomVisibility.Private:
                    _ = channel.AddPermissionOverwriteAsync(teamB, new OverwritePermissions(viewChannel: PermValue.Deny));
                    _ = channel.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(viewChannel: PermValue.Deny));
                    break;
            }

            var messages = await channel.GetMessagesAsync(100).FlattenAsync();
            await channel.DeleteMessagesAsync(messages.Where(m => m.Timestamp.AddDays(14) > DateTime.Now));
            return channel;
        }
    }
}
