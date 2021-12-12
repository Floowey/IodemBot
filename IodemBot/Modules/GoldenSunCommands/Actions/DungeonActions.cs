using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using IodemBot.ColossoBattles;
using IodemBot.Core;
using IodemBot.Core.UserManagement;
using IodemBot.Discords;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Discords.Contexts;
using Microsoft.Extensions.DependencyInjection;
using static IodemBot.ColossoBattles.EnemiesDatabase;

namespace IodemBot.Modules
{
    public class DungeonsAction : IodemBotCommandAction
    {
        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "dungeons",
            Description = "View and enter your dungeons"
        };

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            RefreshAsync = RefreshAsync
        };

        public async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var user = EntityConverter.ConvertUser(Context.User);
            msgProps.Embed = GetDungeonEmbed(user);
            msgProps.Components = GetDungeonComponent(user);
            await Task.CompletedTask;
        }

        public override async Task RunAsync()
        {
            var user = EntityConverter.ConvertUser(Context.User);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: GetDungeonEmbed(user),
                components: GetDungeonComponent(user));
        }

        public static Embed GetDungeonEmbed(UserAccount user)
        {
            EmbedBuilder builder = new();
            var defaultDungeons = DefaultDungeons.Where(d => !d.Requirement.IsLocked(user));
            var availableDefaultDungeons =
                defaultDungeons.Where(d => d.Requirement.Applies(user)).Select(s => s.Name).ToArray();
            var unavailableDefaultDungeons =
                defaultDungeons.Where(d => !d.Requirement.Applies(user)).Select(s => s.Name).ToArray();

            var unlockedDungeons = user.Dungeons.Where(HasDungeon).Select(GetDungeon)
                .Where(d => !d.Requirement.IsLocked(user));
            var availablePermUnlocks = availableDefaultDungeons
                .Concat(unlockedDungeons.Where(d =>
                        !d.IsOneTimeOnly &&
                        d.Requirement.FulfilledRequirements(user))
                    .Select(s => s.Name)
                    .ToArray());
            var unavailablePermUnlocks = unavailableDefaultDungeons
                .Concat(unlockedDungeons.Where(d =>
                        !d.IsOneTimeOnly &&
                        !d.Requirement.FulfilledRequirements(user))
                    .Select(s => s.Name)
                    .ToArray());

            var availableOneTimeUnlocks = unlockedDungeons
                .Where(d => d.IsOneTimeOnly && d.Requirement.FulfilledRequirements(user)).Select(s => s.Name).ToArray();
            var unavailableOneTimeUnlocks = unlockedDungeons
                .Where(d => d.IsOneTimeOnly && !d.Requirement.FulfilledRequirements(user)).Select(s => s.Name)
                .ToArray();

            builder.WithTitle("Dungeons");

            if (availablePermUnlocks.Any())
                builder.AddField("<:mapopen:606236181503410176> Places Discovered",
                    $"Available: {string.Join(", ", availablePermUnlocks)} \nUnavailable: {string.Join(", ", unavailablePermUnlocks)}");
            if (availableOneTimeUnlocks.Length + unavailableOneTimeUnlocks.Length > 0)
                builder.AddField("<:cave:607402486562684944> Dungeon Keys",
                    $"Available: {string.Join(", ", availableOneTimeUnlocks)} \nUnavailable: {string.Join(", ", unavailableOneTimeUnlocks)}");
            return builder.Build();
        }

        public static MessageComponent GetDungeonComponent(UserAccount user)
        {
            ComponentBuilder builder = new();
            var labels = user.Preferences.ShowButtonLabels;
            var defaultDungeons = DefaultDungeons.Where(d => !d.Requirement.IsLocked(user));
            var availableDefaultDungeons =
                defaultDungeons.Where(d => d.Requirement.Applies(user)).Select(s => s.Name).ToArray();
            var unavailableDefaultDungeons =
                defaultDungeons.Where(d => !d.Requirement.Applies(user)).Select(s => s.Name).ToArray();

            var unlockedDungeons = user.Dungeons.Where(HasDungeon).Select(GetDungeon)
                .Where(d => !d.Requirement.IsLocked(user));
            var availablePermUnlocks = availableDefaultDungeons
                .Concat(unlockedDungeons.Where(d =>
                        !d.IsOneTimeOnly &&
                        d.Requirement.FulfilledRequirements(user))
                    .Select(s => s.Name)
                    .ToArray());
            var unavailablePermUnlocks = unavailableDefaultDungeons
                .Concat(unlockedDungeons.Where(d =>
                        !d.IsOneTimeOnly &&
                        !d.Requirement.FulfilledRequirements(user))
                    .Select(s => s.Name)
                    .ToArray());

            var availableOneTimeUnlocks = unlockedDungeons
                .Where(d => d.IsOneTimeOnly && d.Requirement.FulfilledRequirements(user)).Select(s => s.Name).ToArray();
            var unavailableOneTimeUnlocks = unlockedDungeons
                .Where(d => d.IsOneTimeOnly && !d.Requirement.FulfilledRequirements(user)).Select(s => s.Name)
                .ToArray();

            List<SelectMenuOptionBuilder> availableLocations = new();
            foreach (var dungeon in availablePermUnlocks)
                availableLocations.Add(new SelectMenuOptionBuilder { Label = dungeon, Value = dungeon });
            if (availableLocations.Count > 0)
                builder.WithSelectMenu($"{nameof(OpenDungeonAction)}.L", availableLocations,
                    "Select a location to visit");

            List<SelectMenuOptionBuilder> availableDungeons = new();
            foreach (var dungeon in availableOneTimeUnlocks)
                availableDungeons.Add(new SelectMenuOptionBuilder { Label = dungeon, Value = dungeon });
            if (availableDungeons.Count > 0)
                builder.WithSelectMenu($"{nameof(OpenDungeonAction)}.D", availableDungeons,
                    "Select a dungeon to visit (consumes key)");

            builder.WithButton(labels ? "Status" : null, $"#{nameof(StatusAction)}", ButtonStyle.Primary,
                Emotes.GetEmote("StatusAction"));
            builder.WithButton(labels ? "Reveal to others" : null, $"{nameof(RevealEphemeralAction)}", ButtonStyle.Secondary,
        Emotes.GetEmote("RevealEphemeralAction"), row: 0);
            return builder.Build();
        }
    }

    public class OpenDungeonAction : BotComponentAction
    {
        private ColossoBattleService _battleService;

        private Dungeon _dungeon;

        [ActionParameterComponent(Required = true)]
        private string SelectedDungeonName { get; set; }

        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;
        public override bool GuildsOnly => true;

        public override GuildPermissions? RequiredPermissions => null;

        public override async Task RunAsync()
        {
            var gs = GuildSettings.GetGuildSettings(Context.Guild);

            try
            {
                if (Context is RequestInteractionContext r)
                    await r.OriginalInteraction.DeferAsync();
            }
            catch (HttpException)
            {
            }

            var openBattle = new GauntletBattleEnvironment(_battleService, $"{Context.User.Username}",
                gs.ColossoChannel,
                await _battleService.PrepareBattleChannel($"{_dungeon.Name}-{Context.User.Username}",
                    Context.Guild, persistent: false), _dungeon.Name, false);

            _battleService.AddBattleEnvironment(openBattle);
            await Context.Channel.SendMessageAsync(
                $"{Context.User.Username}, {openBattle.BattleChannel.Mention} has been prepared for your adventure to {_dungeon.Name}");
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            var acc = EntityConverter.ConvertUser(Context.User);
            if (!TryGetDungeon(SelectedDungeonName, out _dungeon))
                return Task.FromResult((false, "I don't know where that place is."));

            if (!acc.Dungeons.Contains(_dungeon.Name) && !_dungeon.IsDefault)
                return Task.FromResult((false,
                    "If you can't tell me where this place is, I can't take you there. And even if you knew, they probably wouldn't let you in! Bring me a map or show to me that you have the key to enter."));

            if (!_dungeon.Requirement.Applies(acc))
                return Task.FromResult((false,
                    "I'm afraid that I can't take you to this place, it is too dangerous for you and me both."));

            var gauntletFromUser = _battleService.GetBattleEnvironment<GauntletBattleEnvironment>(b =>
                b.Name.Equals(Context.User.Username, StringComparison.CurrentCultureIgnoreCase));
            if (gauntletFromUser != null)
            {
                if (gauntletFromUser.IsActive)
                    return Task.FromResult((false, "What? You already are on an adventure!"));
                _ = gauntletFromUser.Reset($"{gauntletFromUser.Name} overridden");
            }

            if (_dungeon.IsOneTimeOnly)
            {
                acc.Dungeons.Remove(_dungeon.Name);
                UserAccountProvider.StoreUser(acc);
            }

            return SuccessFullResult;
        }

        public override Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if (selectOptions != null && selectOptions.Any())
                SelectedDungeonName = selectOptions.FirstOrDefault();

            _battleService = ServiceProvider.GetRequiredService<ColossoBattleService>();
            return Task.CompletedTask;
        }
    }
}