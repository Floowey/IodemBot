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
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Modules
{
    public class DjinnAction : IodemBotCommandAction
    {
        public override bool GuildsOnly => false;

        [ActionParameterComponent(Order = 0, Name = "Detail", Description = "...", Required = false)]
        public DjinnDetail detail { get; set; } = DjinnDetail.None;

        public int page { get; set; } = 0;
        public static int pageLimit = 24;

        public override async Task RunAsync()
        {
            var user = EntityConverter.ConvertUser(Context.User);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: GetDjinnEmbed(user, detail), components: GetDjinnComponent(user, detail));
        }

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            RefreshAsync = RefreshAsync,
            FillParametersAsync = (stringOptions, idOptions) =>
            {
                if (idOptions != null && idOptions.Any())
                    detail = Enum.Parse<DjinnDetail>((string)idOptions.FirstOrDefault());

                return Task.CompletedTask;
            }
        };
        public async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var user = EntityConverter.ConvertUser(Context.User);

            msgProps.Embed = GetDjinnEmbed(user, detail);
            msgProps.Components = GetDjinnComponent(user, detail);
            await Task.CompletedTask;
        }

        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "djinn",
            Description = "Show your djinn",
            FillParametersAsync = null
        };

        public static Embed GetDjinnEmbed(UserAccount user, DjinnDetail detail = DjinnDetail.None)
        {
            EmbedBuilder builder = new();
            var djinnPocket = user.DjinnPocket;
            var equippedstring = string.Join("", djinnPocket.GetDjinns().Select(d => d.Emote));
            if (equippedstring.IsNullOrEmpty())
            {
                equippedstring = "-";
            }
            builder.AddField("Equipped", equippedstring);

            foreach (Element e in new[] { Element.Venus, Element.Mars, Element.none, Element.Jupiter, Element.Mercury, Element.none })
            {
                if (e == Element.none)
                {
                    builder.AddField("\u200b", "\u200b", true);
                }
                else
                {
                    var djinnString = djinnPocket.Djinn.OfElement(e).GetDisplay(detail);
                    builder.AddField($"{e} Djinn", djinnString, true);
                }
            }
            var eventDjinn = djinnPocket.Djinn.Count(d => d.IsEvent);
            builder.WithFooter($"{djinnPocket.Djinn.Count()}/{djinnPocket.PocketSize}{(eventDjinn > 0 ? $"(+{eventDjinn})" : "")} Upgrade: {(djinnPocket.PocketUpgrades + 1) * 3000}");

            var summonString = string.Join(detail == DjinnDetail.Names ? ", " : "", djinnPocket.Summons.Select(s => $"{s.Emote}{(detail == DjinnDetail.Names ? $" {s.Name}" : "")}"));
            if (summonString.IsNullOrEmpty())
            {
                summonString = "-";
            }
            builder.AddField("Summons", summonString);
            return builder.Build();
        }

        public static MessageComponent GetDjinnComponent(UserAccount user, DjinnDetail detail = DjinnDetail.None)
        {
            ComponentBuilder builder = new();
            var djinnPocket = user.DjinnPocket;
            bool labels = user.Preferences.ShowButtonLabels;
            var moneyneeded = (uint)(djinnPocket.PocketUpgrades + 1) * 3000;
            builder.WithButton(labels ? "Status" : null, $"#{nameof(StatusAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("StatusAction"));
            builder.WithButton(labels ? "Upgrade" : null, $"{nameof(UpgradeDjinnAction)}", style: ButtonStyle.Success, disabled:!user.Inv.HasBalance(moneyneeded), emote: Emotes.GetEmote("UpgradeDjinnAction"));
            if (detail == DjinnDetail.None)
                builder.WithButton(labels ? "Show Names" : null, $"#{nameof(DjinnAction)}.Names", ButtonStyle.Secondary, Emotes.GetEmote("LabelsOn"), row: 0);
            else
                builder.WithButton(labels ? "Hide Names" : null, $"#{nameof(DjinnAction)}.None", ButtonStyle.Secondary, Emotes.GetEmote("LabelsOff"), row: 0);

            var classSeries = AdeptClassSeriesManager.GetClassSeries(user);
            foreach (var element in classSeries.Elements)
            {
                List<SelectMenuOptionBuilder> options = new();
                foreach (var djinn in djinnPocket.Djinn.OfElement(element).Take(24))
                {
                    var isSelected = false;// djinnPocket.GetDjinns().Any(d => d.Name == djinn.Name);
                    var desc = djinn.Name != djinn.Djinnname ? djinn.Djinnname : null;
                    if (!options.Any(o => o.Value.Equals(djinn.Name)))
                        options.Add(new() { Label = djinn.Name, Value = djinn.Name, Description= desc, Emote = djinn.GetEmote(), IsDefault=isSelected });
                }
                if(options.Count>0)
                    builder.WithSelectMenu($"#{nameof(DjinnEquipAction)}.{element}", options, maxValues: Math.Min(options.Count,2));
            }
            return builder.Build();
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            return SuccessFullResult;
        }
    }

    internal class DjinnEquipAction : IodemBotCommandAction
    {
        [ActionParameterSlash(Order = 1, Name = "djinn1", Description = "The first djinn to equip", Required = true, Type = ApplicationCommandOptionType.String)]
        public string FirstDjinn { get; set; } = "";

        [ActionParameterSlash(Order = 2, Name = "djinn2", Description = "The second djinn to equip", Required = false, Type = ApplicationCommandOptionType.String)]
        public string SecondDjinn { get; set; } = "";

        [ActionParameterComponent(Order = 1, Name = "Djinn", Description = "djinn.", Required = true)]
        public List<string> SelectedDjinn { get; set; } = new();

        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "takedjinn",
            Description = "Select one or two djinn to take with you",
            FillParametersAsync = options =>
            {
                if(options != null && options.Any())
                {
                    FirstDjinn = (string)options.FirstOrDefault().Value;
                    if(options.Count() > 1)
                        SecondDjinn= (string)options.ElementAt(1).Value;


                    if (!FirstDjinn.IsNullOrEmpty())
                        SelectedDjinn.Add(FirstDjinn);

                    if (!SecondDjinn.IsNullOrEmpty())
                        SelectedDjinn.Add(SecondDjinn);
                }
                return Task.CompletedTask;
            }
        };

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            RefreshAsync = RefreshAsync,
            FillParametersAsync = (selectOptions, idOptions) =>
            {
                if (selectOptions != null)
                    SelectedDjinn = selectOptions.ToList();
                return Task.CompletedTask;
            }
        };
        public async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            EquipDjinn();
            var user = EntityConverter.ConvertUser(Context.User);
            msgProps.Embed = DjinnAction.GetDjinnEmbed(user);
            msgProps.Components = DjinnAction.GetDjinnComponent(user);
            await Task.CompletedTask;
        }

        public override async Task RunAsync()
        {
            EquipDjinn();
            var user = EntityConverter.ConvertUser(Context.User);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: DjinnAction.GetDjinnEmbed(user), components: DjinnAction.GetDjinnComponent(user));
        }

        private void EquipDjinn()
        {
            var user = EntityConverter.ConvertUser(Context.User);
            var userDjinn = user.DjinnPocket;
            var userclass = AdeptClassSeriesManager.GetClassSeries(user);
            var chosenDjinn = SelectedDjinn
                .Select(n => userDjinn.GetDjinn(n))
                .Where(d => d != null)
                .OfElement(userclass.Elements)
                .Take(DjinnPocket.MaxDjinn)
                .ToList();

            chosenDjinn.ForEach(d =>
            {
                userDjinn.Djinn.Remove(d);
                userDjinn.Djinn = userDjinn.Djinn.Prepend(d).ToList();
                userDjinn.DjinnSetup = userDjinn.DjinnSetup.Prepend(d.Element).ToList();
            });
            userDjinn.DjinnSetup = userDjinn.DjinnSetup.Take(2).ToList();
            UserAccountProvider.StoreUser(user);
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            return SuccessFullResult;
        }
    }

    public class UpgradeDjinnAction : BotComponentAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override bool GuildsOnly => false;

        public override GuildPermissions? RequiredPermissions => null;

        public override async Task RunAsync()
        {
            _ = UpgradeDjinn();
            await Task.CompletedTask;
        }
        private async Task UpgradeDjinn()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var inv = account.Inv;
            var djinnPocket = account.DjinnPocket;
            var moneyneeded = (uint)(djinnPocket.PocketUpgrades + 1) * 3000;
            if (inv.RemoveBalance(moneyneeded))
            {
                djinnPocket.PocketUpgrades++;
                UserAccountProvider.StoreUser(account);

                await Context.UpdateReplyAsync(msgProps =>
                {
                    msgProps.Embed = DjinnAction.GetDjinnEmbed(account);
                    msgProps.Components = DjinnAction.GetDjinnComponent(account);
                });
                await Context.ReplyWithMessageAsync(EphemeralRule, "Successfully upgraded djinn pocket.");
            }
        }
        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            var user = Context.User;
            var account = EntityConverter.ConvertUser(user);
            var djinnPocket = account.DjinnPocket;
            var moneyneeded = (uint)(djinnPocket.PocketUpgrades + 1) * 3000; ;
            if (djinnPocket.PocketSize>= 70)
                return Task.FromResult((false, "Max upgrades reached"));

            if (!account.Inv.HasBalance(moneyneeded))
                return Task.FromResult((false, "Not enough money"));

            return Task.FromResult((true, (string)null));
        }
    }

    public class DjinnRenameAction : IodemBotCommandAction
    {
        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var djinnPocket = account.DjinnPocket;
            var djinn = djinnPocket.GetDjinn(DjinnToRename);
            djinn.Nickname = NewName;
            UserAccountProvider.StoreUser(account);
            await Context.ReplyWithMessageAsync(EphemeralRule, $"Renamed {djinn.Emote} {djinn.Djinnname} to {NewName}");
        }
        [ActionParameterSlash(Order = 0, Name = "djinn", Description = "The djinn to rename", Required = true, Type = ApplicationCommandOptionType.String)]
        public string DjinnToRename { get; set; }

        [ActionParameterSlash(Order = 1, Name = "name", Description = "The name to rename it to", Required = false, Type = ApplicationCommandOptionType.String)]

        public string NewName { get; set; }
        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "renamedjinn",
            Description = "Rename one of your djinn",
            FillParametersAsync = options =>
            {
                if (options != null)
                    DjinnToRename = (string)options.FirstOrDefault().Value;
                if (options.Count() > 1)
                    NewName = ((string)options.ElementAt(1)?.Value ?? "").Trim();

                return Task.CompletedTask;
            }
        };

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            var djinn = EntityConverter.ConvertUser(Context.User).DjinnPocket.GetDjinn(DjinnToRename);
            if (djinn== null)
                return Task.FromResult((false, "Couldn't find that djinn in your djinn pocket"));

            if (string.IsNullOrWhiteSpace(NewName))
                NewName = djinn.Djinnname;

            return Task.FromResult((true, (string)null));
        }
    }

    // Djinn info, Summon Info
    // Djinn release
}
