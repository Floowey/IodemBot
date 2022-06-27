using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using IodemBot.ColossoBattles;
using IodemBot.Core.UserManagement;
using IodemBot.Discords;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Discords.Contexts;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Modules
{
    public class StatusAction : IodemBotCommandAction
    {
        public override bool GuildsOnly => false;
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        [ActionParameterComponent(Name = "Page", Description = "page", Order = 0, Required = false)]
        public int StatusPage { get; set; } = 0;

        private static readonly int NPages = 3;

        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "status",
            Description = "Show your Status",
            FillParametersAsync = null
        };

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            RefreshAsync = RefreshAsync,
            FillParametersAsync = (selectOptions, idOptions) =>
            {
                if (idOptions != null && idOptions.Any())
                    StatusPage = int.Parse((string)idOptions.FirstOrDefault());
                return Task.CompletedTask;
            }
        };

        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            msgProps.Embed = GetStatusEmbed(account, StatusPage);
            msgProps.Components = GetStatusComponent(account, StatusPage);
            await Task.CompletedTask;
        }

        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var embed = GetStatusEmbed(account, StatusPage);
            var component = GetStatusComponent(account, StatusPage);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: embed, components: component);
        }

        private static readonly Dictionary<Detail, char> Split = new()
        {
            { Detail.None, '>' },
            { Detail.Names, ',' },
            { Detail.NameAndPrice, '\n' }
        };

        internal static Embed GetStatusEmbed(UserAccount account, int statusPage = 0)
        {
            var factory = new PlayerFighterFactory();
            var p = factory.CreatePlayerFighter(account);

            var author = new EmbedAuthorBuilder();
            author.WithName($"{account.Name}");
            author.WithIconUrl(account.ImgUrl);

            var embed = new EmbedBuilder()
            .WithColor(Colors.Get(account.Element.ToString()))
            .WithAuthor(author)
            .WithTitle($"Level {account.LevelNumber} {account.GsClass} {string.Join("", account.TrophyCase.Trophies.Select(t => t.Icon))} (Rank {UserAccounts.GetRank(account) + 1})");

            switch (statusPage)
            {
                case 0: // Overview
                    embed
                        .AddField("Current Equip", account.Inv.GearToString(AdeptClassSeriesManager.GetClassSeries(account).Archtype), true)
                        .AddField("Psynergy", p.GetMoves(false), true)
                        .AddField("Djinn", account.DjinnPocket.GetDjinns().GetDisplay(DjinnDetail.None), true)

                        .AddField("Stats", p.Stats.ToString(), true)
                        .AddField("Elemental Stats", p.ElStats.ToString(), true)
                        .AddField("Unleash Rate", $"{p.UnleashRate}%", true)
                        .AddField("XP", $"{account.Xp} - next in {account.XPneeded}{(account.NewGames >= 1 ? $"\n({account.TotalXp} total | {account.NewGames} resets)" : "")}", true);
                    break;

                case 1: // Stats
                    embed
                        .AddField("Unlocked Classes", account.BonusClasses.Count == 0 ? "none" : string.Join(", ", account.BonusClasses))
                        .AddField("Colosso wins | Dungeon Wins", $"{account.ServerStats.ColossoWins} | {account.ServerStats.DungeonsCompleted}", true)
                        .AddField("Endless Streaks", $"Solo: {account.ServerStats.EndlessStreak.Solo} | Duo: {account.ServerStats.EndlessStreak.Duo} \nTrio: {account.ServerStats.EndlessStreak.Trio} | Quad: {account.ServerStats.EndlessStreak.Quad}", true)
                        .AddField("Damage Dealt", account.BattleStats.DamageDealt, true)
                        .AddField("HP Healed", account.BattleStats.HPhealed, true)
                        .AddField("Highest Damage", account.BattleStats.HighestDamage, true)
                        .AddField("Revives", account.BattleStats.Revives, true)
                        .AddField("Kills by Hand", account.BattleStats.KillsByHand, true);

                    break;

                case 2: // Total Statistics
                    var allTimeBestStreak = account.ServerStats.EndlessStreak + account.ServerStatsTotal.EndlessStreak;
                    embed
                        .AddField("Resets", account.NewGames, true)
                        .AddField("Total XP", account.TotalXp, true)
                        .AddField("Colosso wins | Dungeon Wins", $"{account.ServerStatsTotal.ColossoWins} | {account.ServerStatsTotal.DungeonsCompleted}", true)
                        .AddField("Endless Streaks", $"Solo: {allTimeBestStreak.Solo} | Duo: {allTimeBestStreak.Duo} \nTrio: {allTimeBestStreak.Trio} | Quad: {allTimeBestStreak.Quad}", true);

                    break;

                default:
                    embed
                        .WithDescription("Something went wrong.");
                    break;
            }

            return embed.Build();
        }

        internal static MessageComponent GetStatusComponent(UserAccount account, int statusPage = 0)
        {
            var inv = account.Inv;
            var builder = new ComponentBuilder();
            var labels = account.Preferences.ShowButtonLabels;

            builder.WithButton(labels ? "Classes" : null, $"#{nameof(ClassAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("ClassAction"));
            builder.WithButton(labels ? "Loadouts" : null, $"#{nameof(LoadoutAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("LoadoutAction"));
            builder.WithButton(labels ? "Inventory" : null, $"#{nameof(InventoryAction)}", style: ButtonStyle.Success, emote: Emotes.GetEmote("InventoryAction"));
            builder.WithButton(labels ? "Djinn" : null, $"#{nameof(DjinnAction)}", style: ButtonStyle.Success, emote: Emotes.GetEmote("DjinnAction"));

            var prevPage = statusPage - 1;
            var nextPage = statusPage + 1;

            builder.WithButton(labels ? "Dungeons" : null, $"#{nameof(DungeonsAction)}.", style: ButtonStyle.Secondary, emote: Emotes.GetEmote("DungeonAction"), row: 1);
            builder.WithButton(labels ? "Options" : null, $"#{nameof(OptionActions)}", style: ButtonStyle.Secondary, emote: Emotes.GetEmote("OptionAction"), row: 1);
            builder.WithButton(labels ? "Reveal to others" : null, $"{nameof(RevealEphemeralAction)}", ButtonStyle.Secondary,
                Emotes.GetEmote("RevealEphemeralAction"), row: 1);
            builder.WithButton("◀️", $"#{nameof(StatusAction)}.{prevPage}", style: ButtonStyle.Secondary, disabled: prevPage < 0, row: 1);
            builder.WithButton("▶️", $"#{nameof(StatusAction)}.{nextPage}", style: ButtonStyle.Secondary, disabled: nextPage >= NPages, row: 1);
            return builder.Build();
        }
    }

    public class ChangeAdeptAction : IodemBotCommandAction
    {
        [ActionParameterSlash(Order = 0, Name = "element", Description = "el", Required = true, Type = ApplicationCommandOptionType.String)]
        [ActionParameterOptionString(Name = "Venus", Order = 1, Value = "Venus")]
        [ActionParameterOptionString(Name = "Mars", Order = 1, Value = "Mars")]
        [ActionParameterOptionString(Name = "Windy Boi", Order = 3, Value = "Jupiter")]
        [ActionParameterOptionString(Name = "Mercury", Order = 4, Value = "Mercury")]
        [ActionParameterOptionString(Name = "Exathi", Order = 0, Value = "none")]
        [ActionParameterComponent(Order = 0, Name = "element", Description = "Element", Required = true)]
        public Element SelectedElement { get; set; }

        [ActionParameterSlash(Order = 1, Name = "class", Description = "class", Required = false, AutoComplete = true, Type = ApplicationCommandOptionType.String)]
        [ActionParameterComponent(Order = 1, Name = "class", Description = "class", Required = false)]
        public string SelectedClass { get; set; }

        public override bool GuildsOnly => true;
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override ActionAutoCompleteProperties AutoCompleteProperties => new()
        {
            AutoComplete = AutoComplete
        };

        private IEnumerable<AutocompleteResult> AutoComplete(AutocompleteOption current, IReadOnlyCollection<AutocompleteOption> options)
        {
            var el = Enum.Parse<Element>((string)options.First().Value);
            var value = (string)current.Value;
            var user = EntityConverter.ConvertUser(Context.User);
            var allClasses = AdeptClassSeriesManager.AllClasses;
            var availableClasses = allClasses.Where(c => (c.IsDefault || user.BonusClasses.Contains(c.Name)) && c.Elements.Contains(el));

            return allClasses.Where(c => c.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase) || c.Classes.Any(c => c.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase)))
                .Take(20).Select(s => new AutocompleteResult(s.Name, s.Name));
        }

        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "element",
            Description = "Change your element",
            FillParametersAsync = options =>
            {
                if (options != null)
                {
                    SelectedElement = Enum.Parse<Element>((string)options.FirstOrDefault().Value);
                    if (options.Count() > 1)
                        SelectedClass = (string)options.ElementAt(1).Value;
                }

                return Task.CompletedTask;
            }
        };

        public override async Task RunAsync()
        {
            await ChangeAdeptAsync(Context, SelectedElement, SelectedClass);
            var user = EntityConverter.ConvertUser(Context.User);
            await Context.ReplyWithMessageAsync(EphemeralRule, $"{user.Name} is a {user.Element} {user.GsClass} now.");
        }

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            RefreshAsync = RefreshAsync,
            FillParametersAsync = (selectOptions, idOptions) =>
            {
                if (idOptions != null && idOptions.Any() && (idOptions.FirstOrDefault() is string s && !s.IsNullOrEmpty()))
                {
                    SelectedElement = Enum.Parse<Element>((string)idOptions.FirstOrDefault());
                    if (idOptions.Count() == 2)
                        SelectedClass = (string)idOptions.ElementAt(1);
                }

                if (selectOptions != null && selectOptions.Any())
                {
                    try
                    {
                        SelectedElement = Enum.Parse<Element>(selectOptions.FirstOrDefault());
                    }
                    catch
                    {
                        SelectedElement = EntityConverter.ConvertUser(Context.User).Element;
                        SelectedClass = selectOptions.FirstOrDefault();
                    }
                }

                return Task.CompletedTask;
            }
        };

        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            await ChangeAdeptAsync(Context, SelectedElement, SelectedClass);
            if (intoNew)
            {
                msgProps.Content = $"Welcome to the {SelectedElement} Clan, {Context.User.Mention}";
            }
            else
            {
                var user = EntityConverter.ConvertUser(Context.User);
                msgProps.Embed = ClassAction.GetClassEmbed(user);
                msgProps.Components = ClassAction.GetClassComponent(user);
            }

            await Task.CompletedTask;
        }

        public static async Task ChangeAdeptAsync(RequestContext context, Element selectedElement, string selectedClass = null)
        {
            var guser = (SocketGuildUser)context.User;
            await GiveElementRole(guser, selectedElement, context);
            await ChangeAdept(guser, selectedElement, selectedClass, context);

            //loadedLoadout.ApplyLoadout(user);
        }

        private static async Task GiveElementRole(SocketGuildUser user, Element chosenElement, RequestContext context)
        {
            var role = context.Guild.Roles.FirstOrDefault(x => x.Name == chosenElement.ToString() + " Adepts");
            if (chosenElement == Element.None)
            {
                role = context.Guild.Roles.FirstOrDefault(r => r.Name == "Exathi");
            }
            if (role == null)
                return;

            var venusRole = context.Guild.Roles.FirstOrDefault(x => x.Name == "Venus Adepts");
            var marsRole = context.Guild.Roles.FirstOrDefault(x => x.Name == "Mars Adepts");
            var jupiterRole = context.Guild.Roles.FirstOrDefault(x => x.Name == "Jupiter Adepts");
            var mercuryRole = context.Guild.Roles.FirstOrDefault(x => x.Name == "Mercury Adepts");
            var exathi = context.Guild.Roles.FirstOrDefault(x => x.Name == "Exathi") ?? venusRole;
            var roles = new[] { venusRole, marsRole, jupiterRole, mercuryRole, exathi };
            var userRoles = user.Roles.Where(r => roles.Contains(r));
            if (userRoles.Count() == 1 && userRoles.First() == role)
                return;

            await user.RemoveRolesAsync(userRoles);
            _ = user.AddRoleAsync(role);
        }

        private static async Task ChangeAdept(IGuildUser guser, Element chosenElement, string classSeriesName, RequestContext context)
        {
            var user = EntityConverter.ConvertUser(guser);
            await ChangeElement(user, chosenElement, context);
            ChangeClass(user, classSeriesName);
            UserAccountProvider.StoreUser(user);
        }

        private static async Task ChangeElement(UserAccount user, Element chosenElement, RequestContext context)
        {
            if (user.Element == chosenElement)
            {
                return;
            }
            foreach (string removed in user.Inv.UnequipExclusiveTo(user.Element))
            {
                var removedEmbed = new EmbedBuilder();
                removedEmbed.WithDescription($"<:Exclamatory:571309036473942026> Your {removed} was unequipped.");
                _ = context.ReplyWithMessageAsync(EphemeralRule.EphemeralOrFail, embed: removedEmbed.Build());
            }

            user.Element = chosenElement;
            var tags = new[] { "VenusAdept", "MarsAdept", "JupiterAdept", "MercuryAdept" };
            user.Tags.RemoveAll(s => tags.Contains(s));
            if ((int)chosenElement < tags.Length)
            {
                user.Tags.Add(tags[(int)chosenElement]);
            }
            await Task.CompletedTask;
        }

        private static void ChangeClass(UserAccount user, string classSeriesName = "")
        {
            user.ClassToggle = 0;
            if (!classSeriesName.IsNullOrEmpty())
                AdeptClassSeriesManager.SetClass(user, classSeriesName);

            var series = AdeptClassSeriesManager.GetClassSeries(user);
            if (series != null && !user.DjinnPocket.DjinnSetup.All(d => series.Elements.Contains(d)))
            {
                user.DjinnPocket.DjinnSetup.Clear();
                user.DjinnPocket.DjinnSetup.Add(user.Element);
                user.DjinnPocket.DjinnSetup.Add(user.Element);
            }
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            return SuccessFullResult;
        }
    }

    public class ClassAction : IodemBotCommandAction
    {
        public override bool GuildsOnly => true;

        public override async Task RunAsync()
        {
            var user = EntityConverter.ConvertUser(Context.User);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: GetClassEmbed(user), components: GetClassComponent(user));
        }

        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "class",
            Description = "Change your element and class",
            FillParametersAsync = null
        };

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            FillParametersAsync = null,
            RefreshAsync = RefreshAsync
        };

        public async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var user = EntityConverter.ConvertUser(Context.User);

            msgProps.Embed = GetClassEmbed(user);
            msgProps.Components = GetClassComponent(user);
            await Task.CompletedTask;
        }

        public static Embed GetClassEmbed(UserAccount account)
        {
            var allClasses = AdeptClassSeriesManager.AllClasses;
            var allAvailableClasses = allClasses.Where(c => c.IsDefault || account.BonusClasses.Any(bc => bc.Equals(c.Name)));
            var ofElement = allAvailableClasses.Where(c => c.Elements.Contains(account.Element)).Select(c => c.Name).OrderBy(n => n);

            var embed = new EmbedBuilder();
            embed.WithTitle("Classes");
            embed.WithColor(Colors.Get(account.Element.ToString()));
            embed.AddField("Current Class", AdeptClassSeriesManager.GetClass(account).Name);
            embed.AddField($"Available as {Emotes.GetIcon(account.Element)} {account.Element} Adept:", string.Join(", ", ofElement));
            embed.AddField("Others Unlocked:", string.Join(", ", allAvailableClasses.Select(c => c.Name).Except(ofElement).OrderBy(n => n)));
            embed.WithFooter($"Total: {allAvailableClasses.Count()}/{allClasses.Count}");
            return embed.Build();
        }

        public static MessageComponent GetClassComponent(UserAccount account)
        {
            var allClasses = AdeptClassSeriesManager.AllClasses;
            var allAvailableClasses = allClasses.Where(c => c.IsDefault || account.BonusClasses.Any(bc => bc.Equals(c.Name)));
            var ofElement = allAvailableClasses.Where(c => c.Elements.Contains(account.Element)).OrderBy(n => n.Name);

            var builder = new ComponentBuilder();
            var labels = account.Preferences.ShowButtonLabels;

            List<SelectMenuOptionBuilder> elementOptions = new();
            foreach (var element in new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury })
            {
                elementOptions.Add(new()
                {
                    Label = element.ToString(),
                    Value = $"{element}",
                    IsDefault = account.Element == element,
                    Emote = Emotes.GetEmote(element)
                });
            }
            builder.WithSelectMenu($"#{nameof(ChangeAdeptAction)}.", elementOptions);

            List<SelectMenuOptionBuilder> classOptions = new();
            foreach (var series in ofElement)
            {
                classOptions.Add(new()
                {
                    Label = series.Name,
                    Value = $"{series.Name}",
                    IsDefault = series.Classes.Any(c => c.Name.Equals(account.GsClass))
                });
            }
            builder.WithSelectMenu($"#{nameof(ChangeAdeptAction)}", classOptions);
            builder.WithButton(labels ? "Status" : null, customId: $"#{nameof(StatusAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("StatusAction"), row: 3);
            builder.WithButton(labels ? "Loadouts" : null, $"#{nameof(LoadoutAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("LoadoutAction"));
            builder.WithButton(labels ? "Reveal to others" : null, $"{nameof(RevealEphemeralAction)}", ButtonStyle.Secondary,
                Emotes.GetEmote("RevealEphemeralAction"));
            return builder.Build();
        }
    }
}