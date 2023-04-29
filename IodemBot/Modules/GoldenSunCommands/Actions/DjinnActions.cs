using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;
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

        [ActionParameterComponent(Order = 0, Name = "detail", Description = "...", Required = false)]
        public DjinnDetail Detail { get; set; } = DjinnDetail.None;

        public int DjinnPage { get; set; } = 0;

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            RefreshAsync = RefreshAsync,
            FillParametersAsync = (stringOptions, idOptions) =>
            {
                if (idOptions != null && idOptions.Any())
                {
                    DjinnPage = int.Parse((string)idOptions.FirstOrDefault());
                    Detail = Enum.Parse<DjinnDetail>((string)idOptions.Skip(1).FirstOrDefault());
                }

                return Task.CompletedTask;
            }
        };

        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "djinn",
            Description = "Show your djinn",
            FillParametersAsync = null
        };

        public override async Task RunAsync()
        {
            var user = EntityConverter.ConvertUser(Context.User);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: GetDjinnEmbed(user, Detail, DjinnPage),
                components: GetDjinnComponent(user, Detail, DjinnPage));
        }

        public async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var user = EntityConverter.ConvertUser(Context.User);

            msgProps.Embed = GetDjinnEmbed(user, Detail, DjinnPage);
            msgProps.Components = GetDjinnComponent(user, Detail, DjinnPage);
            await Task.CompletedTask;
        }

        public static Embed GetDjinnEmbed(UserAccount user, DjinnDetail detail = DjinnDetail.None, int pageNr = 0)
        {
            EmbedBuilder embed = new();
            var djinnPocket = user.DjinnPocket;
            var equippedstring = string.Join("", djinnPocket.GetDjinns().Select(d => d.Emote));
            if (equippedstring.IsNullOrEmpty()) equippedstring = "-";

            if (user.Oaths.IsOathActive(Oath.Dispirited))
                equippedstring = ":chains: Locked by an Oath :chains:";
            embed.AddField("Equipped", equippedstring);

            var pages = GetDjinnPages(user);
            var page = pages[pageNr];

            embed.WithTitle("Djinn Pool");
            page.ForEach(pageEntry =>
            {
                var djinnString = pageEntry.GetDisplay(detail);
                var element = pageEntry.FirstOrDefault().Element.ToString() ?? "\u200b";
                embed.AddField($"{element} Djinn", djinnString, false);
            });

            var summonString = string.Join(detail == DjinnDetail.Names ? ", " : "",
                djinnPocket.Summons.Select(s => $"{s.Emote.ToShortEmote()}{(detail == DjinnDetail.Names ? $" {s.Name}" : "")}"));
            if (summonString.IsNullOrEmpty()) summonString = "-";

            embed.AddField("Summons", summonString);

            var eventDjinn = djinnPocket.Djinn.Count(d => d.IsEvent);
            embed.WithFooter(
                $"p.{pageNr + 1}/{GetTotalPages(user)} | {djinnPocket.Djinn.Count}/{djinnPocket.PocketSize}{(eventDjinn > 0 ? $"(+{eventDjinn})" : "")} | comp: {djinnPocket.Djinn.DistinctBy(d => d.Djinnname).Count()} / {DjinnAndSummonsDatabase.DjinnDatabase.Count - DjinnAndSummonsDatabase.Blacklist.Length} | shiny: {djinnPocket.Djinn.Where(d => d.IsShiny).DistinctBy(d => d.Djinnname).Count()} | Upgrade: {(djinnPocket.PocketUpgrades + 1) * 3000}");

            return embed.Build();
        }

        public static MessageComponent GetDjinnComponent(UserAccount user, DjinnDetail detail = DjinnDetail.None, int djinnPage = 0)
        {
            ComponentBuilder builder = new();
            var djinnPocket = user.DjinnPocket;
            var labels = user.Preferences.ShowButtonLabels;
            var moneyneeded = (uint)(djinnPocket.PocketUpgrades + 1) * 3000;

            var elements = new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury };

            builder.WithButton(labels ? "Status" : null, $"#{nameof(StatusAction)}", ButtonStyle.Primary,
                Emotes.GetEmote("StatusAction"));
            builder.WithButton(labels ? "Upgrade" : null, $"{nameof(UpgradeDjinnAction)}", ButtonStyle.Success,
                disabled: !user.Inv.HasBalance(moneyneeded), emote: Emotes.GetEmote("UpgradeDjinnAction"));

            if (detail == DjinnDetail.None)
                builder.WithButton(labels ? "Show Names" : null, $"#{nameof(DjinnAction)}.{djinnPage}.Names", ButtonStyle.Secondary,
                    Emotes.GetEmote("LabelsOn"), row: 0);
            else
                builder.WithButton(labels ? "Hide Names" : null, $"#{nameof(DjinnAction)}.{djinnPage}.None", ButtonStyle.Secondary,
                    Emotes.GetEmote("LabelsOff"), row: 0);

            if (GetDjinnPages(user).Count > 1)
            {
                var ind = GetPagesIndex(user);
                for (int i = 0; i < elements.Length; i++)
                {
                    builder.WithButton(labels ? elements[i].ToString() : null, $"#{nameof(DjinnAction)}.{ind[i]}.{detail}", style: ButtonStyle.Secondary, row: 1, emote: Emotes.GetEmote(elements[i]));
                }
                var prevPage = djinnPage - 1;
                var nextPage = djinnPage + 1;
                builder.WithButton("◀️", $"#{nameof(DjinnAction)}.{prevPage}.{detail}.N", style: ButtonStyle.Secondary, disabled: prevPage < 0, row: 0);
                builder.WithButton("▶️", $"#{nameof(DjinnAction)}.{nextPage}.{detail}.P", style: ButtonStyle.Secondary, disabled: nextPage >= GetTotalPages(user), row: 0);
            }
            builder.WithButton(labels ? "Reveal to others" : null, $"{nameof(RevealEphemeralAction)}", ButtonStyle.Secondary,
                    Emotes.GetEmote("RevealEphemeralAction"), row: 1);

            foreach (var element in user.ClassSeries.Elements)
            {
                List<SelectMenuOptionBuilder> options = new();
                foreach (var djinn in djinnPocket.Djinn.OfElement(element).Take(SelectMenuBuilder.MaxOptionCount))
                {
                    var isSelected = false; // djinnPocket.GetDjinns().Any(d => d.Name == djinn.Name);
                    var desc = djinn.Name != djinn.Djinnname ? djinn.Djinnname : null;
                    if (!options.Any(o => o.Value.Equals(djinn.Name)))
                        options.Add(new SelectMenuOptionBuilder
                        {
                            Label = djinn.Name,
                            Value = djinn.Name,
                            Description = desc,
                            Emote = djinn.GetEmote(),
                            IsDefault = isSelected
                        });
                }

                if (options.Count > 0)
                    builder.WithSelectMenu($"#{nameof(DjinnEquipAction)}.{element}", options, placeholder: $"{element} Djinn",
                        maxValues: Math.Min(options.Count, 2));
            }

            return builder.Build();
        }

        public static readonly int EmbedsPerPage = 4;

        public static int GetTotalPages(UserAccount account)
        {
            return GetPages(account).Sum();
        }

        public static int[] GetPages(UserAccount account)
        {
            var djinn = account.DjinnPocket;
            var pages = new int[4];
            var elements = new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury };

            for (int i = 0; i < elements.Length; i++)
            {
                var fields = djinn.Djinn.OfElement(elements[i]).Count() / SelectMenuBuilder.MaxOptionCount;
                pages[i] = Math.Max(1, fields / EmbedsPerPage);
            }
            return pages;
        }

        public static int[] GetPagesIndex(UserAccount account)
        {
            var pages = GetPages(account);
            var pagesIndex = new int[pages.Length];
            pagesIndex[0] = 0;
            pagesIndex[1] = pages[0];
            pagesIndex[2] = pages[0] + pages[1];
            pagesIndex[3] = pages[0] + pages[1] + pages[2];
            return pagesIndex;
        }

        public static List<List<List<Djinn>>> GetDjinnPages(UserAccount account)
        {
            var djinn = account.DjinnPocket;
            var elements = new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury };

            var pages = new List<List<List<Djinn>>>();
            foreach (var element in elements)
            {
                var ofEl = djinn.Djinn.OfElement(element).ToList();
                if (ofEl.Any())
                {
                    pages.AddRange(ofEl.ChunkBy(SelectMenuBuilder.MaxValuesCount).ChunkBy(EmbedsPerPage));
                }
                else
                {
                    pages.Add(new List<List<Djinn>>());
                }
            }
            if (pages.Select(c => c.Count).Sum() <= EmbedsPerPage)
                pages = new() { pages.SelectMany(c => c).ToList() };

            return pages;
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            return SuccessFullResult;
        }
    }

    internal class DjinnPoolAction : IodemBotCommandAction
    {
        public override bool GuildsOnly => false;
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        [ActionParameterComponent(Name = "Page", Description = "page", Order = 0, Required = false)]
        public int DjinnPage { get; set; } = 0;

        [ActionParameterComponent(Order = 0, Name = "detail", Description = "...", Required = false)]
        public DjinnDetail Detail { get; set; } = DjinnDetail.None;

        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "djinnpool",
            Description = "Show your djinnpool",
            FillParametersAsync = null
        };

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            RefreshAsync = RefreshAsync,
            FillParametersAsync = (selectOptions, idOptions) =>
            {
                if (idOptions != null && idOptions.Any())
                {
                    DjinnPage = int.Parse((string)idOptions.FirstOrDefault());
                    Detail = Enum.Parse<DjinnDetail>((string)idOptions.Skip(1).FirstOrDefault());
                }

                return Task.CompletedTask;
            }
        };

        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            msgProps.Embed = GetDjinnPoolEmbed(account, DjinnPage, Detail);
            msgProps.Components = GetDjinnPoolComponent(account, DjinnPage, Detail);
            await Task.CompletedTask;
        }

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var embed = GetDjinnPoolEmbed(account, DjinnPage, Detail);
            var component = GetDjinnPoolComponent(account, DjinnPage, Detail);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: embed, components: component);
        }

        internal static Embed GetDjinnPoolEmbed(UserAccount account, int pageNr = 0, DjinnDetail detail = DjinnDetail.None)
        {
            var embed = new EmbedBuilder();
            var djinn = account.DjinnPocket;

            return embed.Build();
        }

        internal static MessageComponent GetDjinnPoolComponent(UserAccount account, int djinnPage = 0, DjinnDetail detail = DjinnDetail.None)
        {
            ComponentBuilder builder = new();
            var labels = account.Preferences.ShowButtonLabels;
            builder.WithButton(labels ? "Djinn" : null, $"#{nameof(DjinnAction)}", ButtonStyle.Primary,
               Emotes.GetEmote("DjinnAction"));

            if (detail == DjinnDetail.None)
                builder.WithButton(labels ? "Show Names" : null, $"#{nameof(DjinnPoolAction)}.{djinnPage}.Names", ButtonStyle.Secondary,
                    Emotes.GetEmote("LabelsOn"), row: 0);
            else
                builder.WithButton(labels ? "Hide Names" : null, $"#{nameof(DjinnPoolAction)}.{djinnPage}.None", ButtonStyle.Secondary,
                    Emotes.GetEmote("LabelsOff"), row: 0);

            return builder.Build();
        }
    }

    internal class DjinnEquipAction : IodemBotCommandAction
    {
        [ActionParameterSlash(Order = 1, Name = "djinn1", Description = "The first djinn to equip", Required = true,
            Type = ApplicationCommandOptionType.String)]
        public string FirstDjinn { get; set; } = "";

        [ActionParameterSlash(Order = 2, Name = "djinn2", Description = "The second djinn to equip", Required = false,
            Type = ApplicationCommandOptionType.String)]
        public string SecondDjinn { get; set; } = "";

        [ActionParameterComponent(Order = 1, Name = "Djinn", Description = "djinn.", Required = true)]
        public List<string> SelectedDjinn { get; set; } = new();

        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "takedjinn",
            Description = "Select one or two djinn to take with you",
            FillParametersAsync = options =>
            {
                if (options != null && options.Any())
                {
                    FirstDjinn = (string)options.FirstOrDefault().Value;
                    if (options.Count() > 1)
                        SecondDjinn = (string)options.ElementAt(1).Value;

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
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: DjinnAction.GetDjinnEmbed(user),
                components: DjinnAction.GetDjinnComponent(user));
        }

        private void EquipDjinn()
        {
            var user = EntityConverter.ConvertUser(Context.User);
            var userDjinn = user.DjinnPocket;
            var chosenDjinn = SelectedDjinn
                .Select(n => userDjinn.GetDjinn(n))
                .Where(d => d != null)
                .OfElement(user.ClassSeries.Elements)
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
            var moneyneeded = (uint)(djinnPocket.PocketUpgrades + 1) * 3000;
            ;
            if (djinnPocket.PocketSize >= DjinnPocket.MaxPocketSize)
                return Task.FromResult((false, "Max upgrades reached"));

            if (!account.Inv.HasBalance(moneyneeded))
                return Task.FromResult((false, "Not enough money"));

            return Task.FromResult((true, (string)null));
        }
    }

    public class DjinnRenameAction : IodemBotCommandAction
    {
        [ActionParameterSlash(Order = 0, Name = "djinn", Description = "The djinn to rename", Required = true,
            Type = ApplicationCommandOptionType.String)]
        public string DjinnToRename { get; set; }

        [ActionParameterSlash(Order = 1, Name = "name", Description = "The name to rename it to", Required = false,
            Type = ApplicationCommandOptionType.String)]
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

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var djinnPocket = account.DjinnPocket;
            var djinn = djinnPocket.GetDjinn(DjinnToRename);
            djinn.Nickname = NewName;
            UserAccountProvider.StoreUser(account);
            await Context.ReplyWithMessageAsync(EphemeralRule, $"Renamed {djinn.Emote} {djinn.Djinnname} to {NewName}");
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            var djinn = EntityConverter.ConvertUser(Context.User).DjinnPocket.GetDjinn(DjinnToRename);
            if (djinn == null)
                return Task.FromResult((false, "Couldn't find that djinn in your djinn pocket"));

            if (string.IsNullOrWhiteSpace(NewName))
                NewName = djinn.Djinnname;

            return Task.FromResult((true, (string)null));
        }
    }

    // Djinn info, Summon Info
    // Djinn release
}