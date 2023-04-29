﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using IodemBot.Core.UserManagement;
using IodemBot.Discords;
using IodemBot.Discords.Actions;

namespace IodemBot.Modules
{
    public class OptionActions : IodemBotCommandAction
    {
        public override IActionSlashCommandProperties SlashCommandProperties => base.SlashCommandProperties;

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            FillParametersAsync = null,
            RefreshAsync = RefreshAsync
        };

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: GetOptionsEmbed(account),
                components: GetOptionsComponent(account));
        }

        public async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var user = EntityConverter.ConvertUser(Context.User);

            msgProps.Embed = GetOptionsEmbed(user);
            msgProps.Components = GetOptionsComponent(user);
            await Task.CompletedTask;
        }

        public static Embed GetOptionsEmbed(UserAccount account)
        {
            EmbedBuilder builder = new();
            builder.WithDescription($"Options for {account.Name}");
            builder.AddField("Show Labels", $"{(account.Preferences.ShowButtonLabels ? "Yes" : "No")}");
            var autoSell = account.Preferences.AutoSell.Count == 0
                ? "None"
                : string.Join(", ", account.Preferences.AutoSell);
            builder.AddField("Auto Sell:", $"{autoSell}");

            builder.AddField("HP Bar Theme", $"{account.Preferences.BarThemeHP} {Utilities.GetProgressBar(75, 3, account.Preferences.BarThemeHP)}");
            builder.AddField("PP Bar Theme", $"{account.Preferences.BarThemePP} {Utilities.GetProgressBar(75, 3, account.Preferences.BarThemePP)}");
            if (!account.isSupporter)
                builder.AddField("Want to customize your bars?", "By becoming a Golden Sun Supporter, you will get the choice to change your bar theme. You can become a supporter either by supporting Floowey on Kofi (see i!credits) or boosting the Golden Sun discord server");
            return builder.Build();
        }

        public static MessageComponent GetOptionsComponent(UserAccount account)
        {
            ComponentBuilder builder = new();
            var labels = account.Preferences.ShowButtonLabels;
            builder.WithButton(labels ? "Status" : null, $"#{nameof(StatusAction)}", ButtonStyle.Primary,
                Emotes.GetEmote("StatusAction"), row: 0);
            builder.WithButton("Show Labels", $"{nameof(ToggleButtonLabelsAction)}", ButtonStyle.Secondary,
                Emotes.GetEmote(labels ? "LabelsOn" : "LabelsOff"));

            List<SelectMenuOptionBuilder> options = new();
            var rarities = Enum.GetValues<ItemRarity>();
            foreach (var rarity in rarities)
                options.Add(new SelectMenuOptionBuilder
                {
                    Label = rarity.ToString(),
                    Value = rarity.ToString(),
                    IsDefault = account.Preferences.AutoSell.Contains(rarity)
                });
            builder.WithSelectMenu($"{nameof(SelectAutoSellOptionsAction)}", options, placeholder: "Select item rarities to autosell", minValues: 0,
                maxValues: rarities.Length);

            List<SelectMenuOptionBuilder> barOptions = new();
            var themes = Utilities.progressBars.Keys;

            foreach (var theme in themes)
                barOptions.Add(new SelectMenuOptionBuilder
                {
                    Label = theme,
                    Value = theme,
                    IsDefault = account.Preferences.BarThemeHP.Equals(theme),
                    Emote = Emote.Parse(Utilities.GetProgressBar(75, 1, theme))
                });
            builder.WithSelectMenu($"{nameof(ChooseHPBarThemeAction)}", barOptions, placeholder: "Select HP progress bar theme",
                disabled: !account.isSupporter);

            barOptions.Clear();
            foreach (var theme in themes)
                barOptions.Add(new SelectMenuOptionBuilder
                {
                    Label = theme,
                    Value = theme,
                    IsDefault = account.Preferences.BarThemePP.Equals(theme),
                    Emote = Emote.Parse(Utilities.GetProgressBar(75, 1, theme))
                });
            builder.WithSelectMenu($"{nameof(ChoosePPBarThemeAction)}", barOptions, placeholder: "Select PP progress bar theme",
                disabled: !account.isSupporter);

            return builder.Build();
        }
    }

    internal class SelectAutoSellOptionsAction : BotComponentAction
    {
        private List<ItemRarity> _rarities = new();
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => false;

        public override GuildPermissions? RequiredPermissions => null;

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            account.Preferences.AutoSell = _rarities;
            UserAccountProvider.StoreUser(account);
            await Context.UpdateReplyAsync(m =>
            {
                m.Embed = OptionActions.GetOptionsEmbed(account);
                m.Components = OptionActions.GetOptionsComponent(account);
            });
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            return Task.FromResult((true, (string)null));
        }

        public override async Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if (selectOptions != null) _rarities = selectOptions.Select(Enum.Parse<ItemRarity>).ToList();

            await Task.CompletedTask;
        }
    }

    internal class ChooseHPBarThemeAction : BotComponentAction
    {
        private string barTheme = "classic";
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => false;

        public override GuildPermissions? RequiredPermissions => null;

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            account.Preferences.BarThemeHP = barTheme;
            UserAccountProvider.StoreUser(account);
            await Context.UpdateReplyAsync(m =>
            {
                m.Embed = OptionActions.GetOptionsEmbed(account);
                m.Components = OptionActions.GetOptionsComponent(account);
            });
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            return Task.FromResult((true, (string)null));
        }

        public override async Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if (selectOptions != null) barTheme = selectOptions.FirstOrDefault();

            await Task.CompletedTask;
        }
    }

    internal class ChoosePPBarThemeAction : BotComponentAction
    {
        private string barTheme = "classic";
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => false;

        public override GuildPermissions? RequiredPermissions => null;

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            account.Preferences.BarThemePP = barTheme;
            UserAccountProvider.StoreUser(account);
            await Context.UpdateReplyAsync(m =>
            {
                m.Embed = OptionActions.GetOptionsEmbed(account);
                m.Components = OptionActions.GetOptionsComponent(account);
            });
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            return Task.FromResult((true, (string)null));
        }

        public override async Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if (selectOptions != null) barTheme = selectOptions.FirstOrDefault();

            await Task.CompletedTask;
        }
    }

    internal class ToggleButtonLabelsAction : BotComponentAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => false;

        public override GuildPermissions? RequiredPermissions => null;

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            account.Preferences.ShowButtonLabels = !account.Preferences.ShowButtonLabels;
            UserAccountProvider.StoreUser(account);

            await Context.UpdateReplyAsync(m =>
            {
                m.Embed = OptionActions.GetOptionsEmbed(account);
                m.Components = OptionActions.GetOptionsComponent(account);
            });
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            return Task.FromResult((true, (string)null));
        }
    }
}