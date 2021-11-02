using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using IodemBot.Core.UserManagement;
using IodemBot.Discords;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;

namespace IodemBot.Modules
{
    public class OptionActions : IodemBotCommandAction
    {
        public override IActionSlashCommandProperties SlashCommandProperties => base.SlashCommandProperties;

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: GetOptionsEmbed(account), components: GetOptionsComponent(account));
        }

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            FillParametersAsync = null,
            RefreshAsync = RefreshAsync
        };
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
            builder.AddField($"Show Labels", $"{(account.Preferences.ShowButtonLabels ? "Yes" : "No")}");
            var autoSell = account.Preferences.AutoSell.Count == 0 ? "None" : string.Join(", ", account.Preferences.AutoSell);
            builder.AddField($"Auto Sell:", $"{autoSell}");
            return builder.Build();
        }

        public static MessageComponent GetOptionsComponent(UserAccount account)
        {
            ComponentBuilder builder = new();
            var labels = account.Preferences.ShowButtonLabels;
            builder.WithButton(labels? "Status" : null, customId: $"#{nameof(StatusAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("StatusAction"), row: 0);
            builder.WithButton($"Show Labels", $"{nameof(ToggleButtonLabelsAction)}", style: ButtonStyle.Secondary, Emotes.GetEmote(labels? "LabelsOn" : "LabelsOff"));
            List<SelectMenuOptionBuilder> options = new();
            var rarities = Enum.GetValues<ItemRarity>();
            foreach (var rarity in rarities)
            {
                options.Add(new() { Label = rarity.ToString(), Value = rarity.ToString(), IsDefault=account.Preferences.AutoSell.Contains(rarity) });
            }
            builder.WithSelectMenu($"{nameof(SelectAutoSellOptionsAction)}", options, minValues: 0, maxValues: rarities.Length);
            return builder.Build();
        }
    }

    internal class SelectAutoSellOptionsAction : BotComponentAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => false;

        public override GuildPermissions? RequiredPermissions => null;

        private List<ItemRarity> rarities = new();

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            account.Preferences.AutoSell = rarities;
            UserAccountProvider.StoreUser(account);
            await Context.UpdateReplyAsync(m => { m.Embed = OptionActions.GetOptionsEmbed(account); m.Components = OptionActions.GetOptionsComponent(account); });
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            return Task.FromResult((true, (string)null));
        }

        public override async Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if(selectOptions != null)
            {
                rarities = selectOptions.Select(s => Enum.Parse<ItemRarity>(s)).ToList();
            }

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

            await Context.UpdateReplyAsync(m => { m.Embed = OptionActions.GetOptionsEmbed(account); m.Components = OptionActions.GetOptionsComponent(account); });
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            return Task.FromResult((true, (string)null));
        }
    }
}
