using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
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
    public class DungeonAction : IodemBotCommandAction
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
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: GetDungeonEmbed(user), components: GetDungeonComponent(user));
        }

        public static Embed GetDungeonEmbed(UserAccount user)
        {
            EmbedBuilder builder = new();
            var defaultDungeons = EnemiesDatabase.DefaultDungeons.Where(d => !d.Requirement.IsLocked(user));
            var availableDefaultDungeons = defaultDungeons.Where(d => d.Requirement.Applies(user)).Select(s => s.Name).ToArray();
            var unavailableDefaultDungeons = defaultDungeons.Where(d => !d.Requirement.Applies(user)).Select(s => s.Name).ToArray();

            var unlockedDungeons = user.Dungeons.Where(s => EnemiesDatabase.HasDungeon(s)).Select(s => EnemiesDatabase.GetDungeon(s)).Where(d => !d.Requirement.IsLocked(user));
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

            var availableOneTimeUnlocks = unlockedDungeons.Where(d => d.IsOneTimeOnly && d.Requirement.FulfilledRequirements(user)).Select(s => s.Name).ToArray();
            var unavailableOneTimeUnlocks = unlockedDungeons.Where(d => d.IsOneTimeOnly && !d.Requirement.FulfilledRequirements(user)).Select(s => s.Name).ToArray();

           
            builder.WithTitle("Dungeons");

            if (availablePermUnlocks.Count() > 0)
            {
                builder.AddField("<:mapopen:606236181503410176> Places Discovered", $"Available: {string.Join(", ", availablePermUnlocks)} \nUnavailable: {string.Join(", ", unavailablePermUnlocks)}");
            }
            if (availableOneTimeUnlocks.Count() + unavailableOneTimeUnlocks.Count() > 0)
            {
                builder.AddField("<:cave:607402486562684944> Dungeon Keys", $"Available: {string.Join(", ", availableOneTimeUnlocks)} \nUnavailable: {string.Join(", ", unavailableOneTimeUnlocks)}");
            }
            return builder.Build();
        }

        public static MessageComponent GetDungeonComponent(UserAccount user)
        {
            ComponentBuilder builder = new();
            var labels = user.Preferences.ShowButtonLabels;
            var defaultDungeons = DefaultDungeons.Where(d => !d.Requirement.IsLocked(user));
            var availableDefaultDungeons = defaultDungeons.Where(d => d.Requirement.Applies(user)).Select(s => s.Name).ToArray();
            var unavailableDefaultDungeons = defaultDungeons.Where(d => !d.Requirement.Applies(user)).Select(s => s.Name).ToArray();

            var unlockedDungeons = user.Dungeons.Where(s => HasDungeon(s)).Select(s => GetDungeon(s)).Where(d => !d.Requirement.IsLocked(user));
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

            var availableOneTimeUnlocks = unlockedDungeons.Where(d => d.IsOneTimeOnly && d.Requirement.FulfilledRequirements(user)).Select(s => s.Name).ToArray();
            var unavailableOneTimeUnlocks = unlockedDungeons.Where(d => d.IsOneTimeOnly && !d.Requirement.FulfilledRequirements(user)).Select(s => s.Name).ToArray();

            List<SelectMenuOptionBuilder> availableLocations = new();
            foreach (var dungeon in availablePermUnlocks)
            {
                availableLocations.Add(new() { Label = dungeon, Value = dungeon });
            }
            if (availableLocations.Count > 0)
                builder.WithSelectMenu($"{nameof(OpenDungeonAction)}.L", availableLocations, "Select a location to visit");


            List<SelectMenuOptionBuilder> availableDungeons = new();
            foreach (var dungeon in availableOneTimeUnlocks)
            {
                availableDungeons.Add(new() { Label = dungeon, Value = dungeon });
            }
            if (availableDungeons.Count > 0)
                builder.WithSelectMenu($"{nameof(OpenDungeonAction)}.D", availableDungeons, "Select a dungeon to visit (consumes key)");

            builder.WithButton(labels ? "Status" : null, $"#{nameof(StatusAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("StatusAction"));
            return builder.Build();
        }
    }

    public class OpenDungeonAction : BotComponentAction
    {
        [ActionParameterComponent(Required =true)]
        private string SelectedDungeonName { get; set; }
        private Dungeon Dungeon;
        private ColossoBattleService BattleService;
        public override EphemeralRule EphemeralRule =>EphemeralRule.EphemeralOrFail;
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

            var openBattle = new GauntletBattleEnvironment(BattleService, $"{Context.User.Username}", gs.ColossoChannel, 
                await BattleService.PrepareBattleChannel($"{Dungeon.Name}-{Context.User.Username}", 
                Context.Guild, persistent: false), Dungeon.Name, false);

            BattleService.AddBattleEnvironment(openBattle);
            await Context.Channel.SendMessageAsync($"{Context.User.Username}, {openBattle.BattleChannel.Mention} has been prepared for your adventure to {Dungeon.Name}");
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            var acc = EntityConverter.ConvertUser(Context.User);
            if (!TryGetDungeon(SelectedDungeonName, out Dungeon))
                return Task.FromResult((false, $"I don't know where that place is."));
            
            if (!acc.Dungeons.Contains(Dungeon.Name) && !Dungeon.IsDefault)
                return Task.FromResult((false,$"If you can't tell me where this place is, I can't take you there. And even if you knew, they probably wouldn't let you in! Bring me a map or show to me that you have the key to enter."));
            

            if (!Dungeon.Requirement.Applies(acc))
                return Task.FromResult((false, $"I'm afraid that I can't take you to this place, it is too dangerous for you and me both."));
            

            var gauntletFromUser = BattleService.GetBattleEnvironment<GauntletBattleEnvironment>(b => b.Name.Equals(Context.User.Username, StringComparison.CurrentCultureIgnoreCase));
            if (gauntletFromUser != null)
            {
                if (gauntletFromUser.IsActive)
                    return Task.FromResult((false, $"What? You already are on an adventure!"));
                else
                {
                    _ = gauntletFromUser.Reset($"{gauntletFromUser.Name} overridden");
                    //battles.Remove(gauntletFromUser);
                }
            }

            if (Dungeon.IsOneTimeOnly)
            {
                acc.Dungeons.Remove(Dungeon.Name);
                UserAccountProvider.StoreUser(acc);
            }

            return SuccessFullResult;
        }

        public override Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if (selectOptions != null && selectOptions.Any())
                SelectedDungeonName = selectOptions.FirstOrDefault();

            BattleService = ServiceProvider.GetRequiredService<ColossoBattleService>();
            return Task.CompletedTask;
        }
    }
}
