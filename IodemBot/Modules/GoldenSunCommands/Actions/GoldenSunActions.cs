using System;
using System.Collections.Generic;
using System.IO;
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
using IodemBot.Images;
using IodemBot.Modules.GoldenSunMechanics;
using Microsoft.Extensions.DependencyInjection;

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

            var trophies = string.Join("", account.TrophyCase.Trophies.Select(t => t.Icon));
            if (trophies.IsNullOrEmpty())
                trophies = "-";

            var embed = new EmbedBuilder()
            .WithColor(Colors.Get(account.Element.ToString()))
            .WithAuthor(author)
            .WithDescription(trophies)
            .WithTitle($"Level {account.LevelNumber} {account.GsClass} Rank {UserAccounts.GetRank(account) + 1}");

            switch (statusPage)
            {
                case 0: // Overview
                    embed
                        .AddField("Current Equip", account.Inv.GearToString(account.ClassSeries.Archtype), true)
                        .AddField("Psynergy", p.GetMoves(false), true)
                        .AddField("Djinn", account.DjinnPocket.GetDjinns().GetDisplay(DjinnDetail.None), true)

                        .AddField("Stats", p.Stats.ToString(), true)
                        .AddField("Elemental Stats", p.ElStats.ToString(), true)
                        .AddField("Unleash Rate", $"{p.UnleashRate}%", true)
                        .AddField("XP", $"{account.Xp} {(account.Oaths.IsOathActive(Oath.Oaf) ? $" (effective: {(ulong)(account.Xp / 4 / account.XpBoost)})" : "")} - next in {account.XPneeded}{(account.NewGames >= 1 ? $"\n({account.TotalXp} total | {account.NewGames} resets)" : "")}", true)
                        .AddField("Oaths", $"active: {string.Join(", ", account.Oaths.ActiveOaths.Select(o => o.ToString()))}\n" +
                        $"completed this run: {string.Join(", ", account.Oaths.OathsCompletedThisRun.Select(o => o.ToString()))}");
                    break;

                case 1: // Stats
                    embed
                        .AddField("Unlocked Classes", account.BonusClasses.Count == 0 ? "none" : string.Join(", ", account.BonusClasses))
                        .AddField("Colosso wins | Dungeon Wins", $"{account.ServerStats.ColossoWins} | {account.ServerStats.DungeonsCompleted}", true)
                        .AddField("Endless Streaks", $"Solo: {account.ServerStats.EndlessStreak.Solo} | Duo: {account.ServerStats.EndlessStreak.Duo} \nTrio: {account.ServerStats.EndlessStreak.Trio} | Quad: {account.ServerStats.EndlessStreak.Quad}", true)
                        .AddField("Damage Dealt", account.BattleStats.DamageDealt, true)
                        .AddField("Highest Damage", account.BattleStats.HighestDamage, true)
                        .AddField("Damage Tanked", account.BattleStats.DamageTanked, true)
                        .AddField("HP Healed", account.BattleStats.HPhealed, true)
                        .AddField("PP Used", account.BattleStats.PPUsed, true)
                        .AddField("Revives", account.BattleStats.Revives, true)
                        .AddField("Kills by Hand", account.BattleStats.KillsByHand, true)
                        .AddField("Item Activations", account.BattleStats.ItemActivations, true)
                        .AddField("Bad Luck", account.DjinnBadLuck, true);
                    break;

                case 2: // Total Statistics
                    var allTimeBestStreak = account.ServerStats.EndlessStreak + account.ServerStatsTotal.EndlessStreak;
                    var XPIncrement = account.Oaths.OathsCompletedThisRun.Count;

                    embed
                        .AddField("Resets", account.NewGames, true)
                        .AddField("Total XP", account.TotalXp, true)
                        .AddField("XP Boost", account.XpBoost, true)
                        .AddField("XP Boost After Reset", (0.075 + XPIncrement * 0.025) * (1 - Math.Exp(-(double)account.Xp / 120000)), true)
                        .AddField("XP Boost Cap", account.MaxXpBoost, true)
                        .AddField("Colosso wins | Dungeon Wins", $"{account.ServerStatsTotal.ColossoWins} | {account.ServerStatsTotal.DungeonsCompleted}", true)
                        .AddField("Endless Streaks", $"Solo: {allTimeBestStreak.Solo} | Duo: {allTimeBestStreak.Duo} \nTrio: {allTimeBestStreak.Trio} | Quad: {allTimeBestStreak.Quad}", true)
                        .AddField("Oaths", $"sol. completed: {string.Join(", ", account.Oaths.CompletedSolitudeOaths.Select(o => o.ToString()))}\n" +
                        $"completed: {string.Join(", ", account.Oaths.CompletedOaths.Except(account.Oaths.CompletedSolitudeOaths).Select(o => o.ToString()))}");

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
        [ActionParameterOptionString(Name = "Jupitery", Order = 3, Value = "Jupiter")]
        [ActionParameterOptionString(Name = "Mercury", Order = 4, Value = "Mercury")]
        [ActionParameterOptionString(Name = "Exathi", Order = 0, Value = "none")]
        [ActionParameterComponent(Order = 0, Name = "element", Description = "Element", Required = true)]
        public Element SelectedElement { get; set; }

        [ActionParameterSlash(Order = 1, Name = "class", Description = "class", Required = false, AutoComplete = true, Type = ApplicationCommandOptionType.String)]
        [ActionParameterComponent(Order = 1, Name = "class", Description = "class", Required = false)]
        public string SelectedClass { get; set; }

        [ActionParameterComponent(Order = 1, Name = "Passive Initiative", Description = "Passive Initiative", Required = false)]
        public string SelectedPassive { get; set; }

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

                    if (options.Count() > 2)
                        SelectedPassive = (string)options.ElementAt(2).Value;
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
                var isPassive = false;
                if (idOptions != null && idOptions.Any() && (idOptions.FirstOrDefault() is string s && !s.IsNullOrEmpty()))
                {
                    if (s == "Passive")
                    {
                        isPassive = true;
                    }
                    else
                    {
                        SelectedElement = Enum.Parse<Element>((string)idOptions.FirstOrDefault());
                        if (idOptions.Length == 2)
                            SelectedClass = (string)idOptions.ElementAt(1);

                        if (idOptions.Length == 3)
                            SelectedPassive = (string)idOptions.ElementAt(2);
                    }
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
                        if (isPassive)
                            SelectedPassive = selectOptions.FirstOrDefault();
                        else
                            SelectedClass = selectOptions.FirstOrDefault();
                    }
                }

                return Task.CompletedTask;
            }
        };

        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            await ChangeAdeptAsync(Context, SelectedElement, SelectedClass, SelectedPassive);
            if (intoNew)
            {
                msgProps.Content = $"Welcome to the {SelectedElement} Clan, {Context.User.Mention}";
            }
            else
            {
                var user = EntityConverter.ConvertUser(Context.User);
                var a = new ClassAction();
                a.Initialize(ServiceProvider, Context);

                msgProps.Embed = await a.GetClassEmbed(user);
                msgProps.Components = ClassAction.GetClassComponent(user);
            }

            await Task.CompletedTask;
        }

        public static async Task ChangeAdeptAsync(RequestContext context, Element selectedElement, string selectedClass = null, string selectedPassive = null)
        {
            var guser = (SocketGuildUser)context.User;
            await GiveElementRole(guser, selectedElement, context);
            await ChangeAdept(guser, selectedElement, selectedClass, selectedPassive, context);

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

        private static async Task ChangeAdept(IGuildUser guser, Element chosenElement, string classSeriesName, string passive, RequestContext context)
        {
            var user = EntityConverter.ConvertUser(guser);
            await ChangeElement(user, chosenElement, context);

            ChangeClass(user, classSeriesName);
            user.Passives.SelectedPassive = passive ?? user.Passives.SelectedPassive;

            user.Tags.Remove("Warrior");
            user.Tags.Remove("Mage");
            user.Tags.Add(user.ClassSeries.Archtype.ToString());
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
            if (user.Oaths.IsOathOfElementActive())
                return;

            user.Element = chosenElement;
            var tags = new[] { "VenusAdept", "MarsAdept", "JupiterAdept", "MercuryAdept" };

            if (!user.Passives.GetSelectedPassive().elements?.Contains(chosenElement) ?? false)
                user.Passives.SelectedPassive = user.Passives.UnlockedPassives.FirstOrDefault(p => p.elements.Contains(chosenElement)).Name;

            user.Tags.RemoveAll(s => tags.Contains(s));
            if ((int)chosenElement < tags.Length)
            {
                user.Tags.Add(tags[(int)chosenElement]);
            }
            await Task.CompletedTask;
        }

        private static void ChangeClass(UserAccount user, string classSeriesName = "")
        {
            if (!classSeriesName.IsNullOrEmpty())
            {
                user.ClassToggle = 0;
                AdeptClassSeriesManager.SetClass(user, classSeriesName);
            }

            if (user.ClassSeries != null && !user.DjinnPocket.DjinnSetup.All(d => user.ClassSeries.Elements.Contains(d)))
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
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: await GetClassEmbed(user), components: GetClassComponent(user));
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

            msgProps.Embed = await GetClassEmbed(user);
            msgProps.Components = GetClassComponent(user);
            await Task.CompletedTask;
        }

        public async Task<Embed> GetClassEmbed(UserAccount account)
        {
            var allClasses = AdeptClassSeriesManager.AllClasses;
            var allAvailableClasses = allClasses.Where(c => c.IsDefault || account.BonusClasses.Any(bc => bc.Equals(c.Name)));
            var ofElement = allAvailableClasses.Where(c => c.Elements.Contains(account.Element)).Select(c => c.Name).OrderBy(n => n);
            var availableClasses = AdeptClassSeriesManager.GetAvailableClasses(account);

            var embed = new EmbedBuilder();
            embed.WithTitle("Classes");
            embed.WithColor(Colors.Get(account.Element.ToString()));
            embed.AddField("Current Class", AdeptClassSeriesManager.GetClass(account).Name);
            embed.AddField($"Available as {Emotes.GetIcon(account.Element)} {account.Element} Adept:", string.Join(", ", ofElement));
            embed.AddField("Other classes unlocked:", string.Join(", ", allAvailableClasses.Select(c => c.Name).Except(ofElement).OrderBy(n => n)));

            var passiveDesc = account.Passives.SelectedPassive.IsNullOrEmpty() 
                            ? "-" 
                            : account.Passives.SelectedPassive + $" (Level {account.Passives.GetPassiveLevel(account.Oaths) +1})" + "\n" + $"*{account.Passives.GetSelectedPassive().Description}*" ;
            embed.AddField("Selected Passive Initiative", $"{passiveDesc}", true);
            embed.AddField("Passive Initiatives?", "*Passive Initiatives are abilities that activate at the start of every battle. They get unlocked by completing the Elemental Path IV's and upgrade through the elemental oaths.*");

            var cs = ServiceProvider.GetRequiredService<CompassService>();
            embed.WithThumbnailUrl(await cs.GetCompass(account));

            embed.WithFooter($"Total: {allAvailableClasses.Count()}/{allClasses.Count}");

            return embed.Build();
        }

        public static MessageComponent GetClassComponent(UserAccount account)
        {
            var allClasses = AdeptClassSeriesManager.AllClasses;
            var allAvailableClasses = allClasses.Where(c => c.IsDefault || account.BonusClasses.Any(bc => bc.Equals(c.Name)));
            var ofElement = allAvailableClasses.Where(c => c.Elements.Contains(account.Element)).OrderBy(n => n.Name);

            var availableClasses = AdeptClassSeriesManager.GetAvailableClasses(account);
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
            builder.WithSelectMenu($"#{nameof(ChangeAdeptAction)}.", elementOptions, disabled: account.Oaths.IsOathOfElementActive());

            List<SelectMenuOptionBuilder> classOptions = new();
            foreach (var series in availableClasses)
            {
                classOptions.Add(new()
                {
                    Label = series.Name,
                    Value = $"{series.Name}",
                    IsDefault = series.Classes.Any(c => c.Name.Equals(account.GsClass))
                });
            }
            builder.WithSelectMenu($"#{nameof(ChangeAdeptAction)}", classOptions);

            List<SelectMenuOptionBuilder> passiveOptions = new();
            foreach (var passive in account.Passives.UnlockedPassives.Where(p => p.elements.Contains(account.Element)))
            {
                passiveOptions.Add(new()
                {
                    Label = $"{passive.Name} ({Passives.GetPassiveLevel(passive, account.Oaths) + 1})",
                    Value = $"{passive.Name}",
                    IsDefault = account.Passives.SelectedPassive == passive.Name,
                    Description = passive.ShortDescription
                });
            }
            if (passiveOptions.Any())
                builder.WithSelectMenu($"#{nameof(ChangeAdeptAction)}.Passive", passiveOptions);

            builder.WithButton(labels ? "Status" : null, customId: $"#{nameof(StatusAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("StatusAction"), row: 3);
            builder.WithButton(labels ? "Loadouts" : null, $"#{nameof(LoadoutAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("LoadoutAction"));
            builder.WithButton(labels ? "Reveal to others" : null, $"{nameof(RevealEphemeralAction)}", ButtonStyle.Secondary,
                Emotes.GetEmote("RevealEphemeralAction"));
            return builder.Build();
        }
    }
}