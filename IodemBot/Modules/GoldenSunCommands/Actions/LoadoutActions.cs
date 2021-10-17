using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Discords;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Discords.Contexts;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Modules
{
    public class LoadoutAction : IodemBotCommandAction
    {
        public override Task RunAsync()
        {
            throw new NotImplementedException();
        }

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            FillParametersAsync = null,
            RefreshAsync = RefreshAsync
        };

        public async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            msgProps.Embed = GetLoadoutEmbed(account);
            msgProps.Components = GetLoadoutComponent(account);
            await Task.CompletedTask;
        }

        public static Embed GetLoadoutEmbed(UserAccount account)
        {
            var builder = new EmbedBuilder();

            var curLoadout = Loadout.GetLoadout(account);
            curLoadout.LoadoutName = "Current Loadout";
            var curitems = curLoadout.Gear.Count > 0 ? string.Join("", curLoadout.Gear.Select(i => account.Inv.GetItem(i)?.Icon ?? "-")) : "no gear";
            var curdjinn = curLoadout.Djinn.Count > 0 ? string.Join("", curLoadout.Djinn.Select(d => account.DjinnPocket.GetDjinn(d)?.Emote ?? "-")) : "no Djinn";
            builder.AddField(curLoadout.LoadoutName,
                $"{Emotes.GetIcon(curLoadout.Element)} {curLoadout.ClassSeries}\n" +
                $"{curitems}\n" +
                $"{curdjinn}"
                , inline: false);

            foreach (var loadout in account.Loadouts.loadouts)
            {
                var items = loadout.Gear.Count > 0 ? string.Join("", loadout.Gear.Select(i => account.Inv.GetItem(i)?.Icon ?? "-")) : "no gear";
                var djinn = loadout.Djinn.Count > 0 ? string.Join("", loadout.Djinn.Select(d => account.DjinnPocket.GetDjinn(d)?.Emote ?? "-")) : "no Djinn";
                builder.AddField(loadout.LoadoutName,
                    $"{Emotes.GetIcon(loadout.Element)} {loadout.ClassSeries}\n" +
                    $"{items}\n" +
                    $"{djinn}"
                    , inline: true);
            }

            if (account.Loadouts.loadouts.Count == 0)
            {
                builder.AddField("No Loadouts", "You currently don't have any Loadouts. Save your ucrrent loadout using ()");
            }
            return builder.Build();
        }

        public static MessageComponent GetLoadoutComponent(UserAccount account)
        {
            var builder = new ComponentBuilder();

            builder.WithButton("Status", customId: $"#{nameof(StatusAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("StatusAction"));
            builder.WithButton("Classes", $"#{nameof(ClassAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("ClassAction"));
            builder.WithButton("Save current Loadout", $"#{nameof(LoadoutSaveAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("SaveLoadoutAction"));
            List<SelectMenuOptionBuilder> options = new();

            if (account.Loadouts.loadouts.Count == 0)
            {
                return builder.Build();
            }
            foreach (var item in account.Loadouts.loadouts)
            {
                options.Add(new() { Label = $"{item.LoadoutName}", Value = $"{item.LoadoutName}", Emote = Emotes.GetEmote(item.Element) });
            }
            builder.WithSelectMenu("Loadout", $"{nameof(LoadoutTakeAction)}", options, placeholder: "Select a Loadout to change into");

            return builder.Build();
        }
    }

    public class LoadoutTakeAction : BotComponentAction
    {
        [ActionParameterComponent(Order = 0, Name = "Loadout", Description = "Loadout", Required = false)]
        public string SelectedLoadout { get; set; }
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => true;

        public override GuildPermissions? RequiredPermissions => null;

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            return Task.FromResult((true, (string)null));
        }
        public override async Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if (selectOptions != null && selectOptions.Any())
            {
                SelectedLoadout = selectOptions.FirstOrDefault();
            };
            await Task.CompletedTask;
        }

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var loadedLoadout = account.Loadouts.GetLoadout(SelectedLoadout);
            if (loadedLoadout != null)
            {
                await ChangeAdeptAction.ChangeAdeptAsync(Context, loadedLoadout.Element);
                account = EntityConverter.ConvertUser(Context.User);
                loadedLoadout.ApplyLoadout(account);
                UserAccountProvider.StoreUser(account);
            }

            await Task.CompletedTask;
        }
    }
    public class LoadoutSaveAction : IodemBotCommandAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => true;

        public override GuildPermissions? RequiredPermissions => null;

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            return Task.FromResult((true, (string)null));
        }
        private static Dictionary<Element, string[]> Prompts = new()
        {
            { Element.none, new[] { "Boring" } },
            { Element.Venus, new[] { "Muddy", "Earthy", "Dirty", "Venus", "Gaia", "Green", "Growing", "Rocky", "Steady", "Rooted" } },
            { Element.Mars, new[] { "Fiery", "Hot", "Heated", "Spicy", "Burning", "Flaming", "Glowing", "Magma", "Mars" } },
            { Element.Jupiter, new[] { "Sparky", "Windy", "Boony", "Thunderous", "Tempest", "Howling", "Blowing", "Air", "Jupiter" } },
            { Element.Mercury, new[] { "Flowing", "Freezing", "Snowy", "Oceanic", "Aqua", "Mercury", "Blue", "Raining" } }
        };
        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            SaveLoadout(account);
            UserAccountProvider.StoreUser(account);

            await Task.CompletedTask;
        }

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            FillParametersAsync = null,
            RefreshAsync = RefreshAsync
        };

        public async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            SaveLoadout(account);
            UserAccountProvider.StoreUser(account);
            msgProps.Embed = LoadoutAction.GetLoadoutEmbed(account);
            msgProps.Components = LoadoutAction.GetLoadoutComponent(account);
            await Task.CompletedTask;
        }

        private void SaveLoadout(UserAccount account)
        {
            var loadout = Loadout.GetLoadout(account);
            loadout.LoadoutName = $"{Prompts[loadout.Element].Random()} {loadout.ClassSeries[0..^7]}";
            account.Loadouts.SaveLoadout(loadout);
        }
    }
}
