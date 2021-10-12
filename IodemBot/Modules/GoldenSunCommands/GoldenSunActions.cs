using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;
using Discord;
using IodemBot.Core.UserManagement;
using IodemBot.Modules.ColossoBattles;
using IodemBot.Modules.GoldenSunMechanics;
using IodemBot.Extensions;
using IodemBot.Discords;
using Discord.WebSocket;

namespace IodemBot.Modules
{
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
        [ActionParameterSlash(Order = 1, Name = "class", Description = "class", Required = false, Type = ApplicationCommandOptionType.String)]
        [ActionParameterComponent(Order = 1, Name = "class", Description = "class", Required = false)]

        public string SelectedClass { get; set; }

        public override bool GuildsOnly => true;
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

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
            await ChangeElementAsync();
            var user = EntityConverter.ConvertUser(Context.User);
            await Context.ReplyWithMessageAsync(EphemeralRule, $"{user.Name} is a {user.Element} {user.GsClass} now.");
        }

        private async Task ChangeElementAsync()
        {
            var guser = (SocketGuildUser)Context.User;
            await GiveElementRole(guser, SelectedElement);
            await ChangeAdept(guser, SelectedElement, SelectedClass);

            //loadedLoadout.ApplyLoadout(user);
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
                    if(idOptions.Count() > 1)
                        SelectedClass = (string)idOptions.ElementAt(1);
                };

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
                };

                return Task.CompletedTask;
            }
        };
        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            await ChangeElementAsync();
            if (intoNew)
            {
                msgProps.Content = $"Welcome to the {SelectedElement} Clan, {Context.User.Mention}";
            } else
            {
                var user = EntityConverter.ConvertUser(Context.User);
                msgProps.Embed = ClassAction.GetClassEmbed(user);
                msgProps.Components = ClassAction.GetClassComponent(user);
            }
        }
        private async Task GiveElementRole(SocketGuildUser user, Element chosenElement)
        {
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == chosenElement.ToString() + " Adepts");
            if (chosenElement == Element.none)
            {
                role = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Exathi");
            }
            if (role == null)
                return;

            var venusRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Venus Adepts");
            var marsRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Mars Adepts");
            var jupiterRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Jupiter Adepts");
            var mercuryRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Mercury Adepts");
            var exathi = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Exathi") ?? venusRole;
            var roles = new[] { venusRole, marsRole, jupiterRole, mercuryRole, exathi };
            var userRoles = user.Roles.Where(r => roles.Contains(r));
            if (userRoles.Count() == 1 && userRoles.First() == role)
                return;

            await user.RemoveRolesAsync(userRoles);
            _ = user.AddRoleAsync(role);
        }

        public async Task ChangeAdept(IGuildUser guser, Element chosenElement, string classSeriesName)
        {
            var user = EntityConverter.ConvertUser(guser);
            await ChangeElement(user, chosenElement);
            ChangeClass(user, classSeriesName);
            UserAccountProvider.StoreUser(user);
        }
        public async Task ChangeElement(UserAccount user, Element chosenElement)
        {
            if (user.Element == chosenElement)
            {
                return;
            }
            foreach (string removed in user.Inv.UnequipExclusiveTo(user.Element))
            {
                var removedEmbed = new EmbedBuilder();
                removedEmbed.WithDescription($"<:Exclamatory:571309036473942026> Your {removed} was unequipped.");
                await Context.ReplyWithMessageAsync(EphemeralRule,embed: removedEmbed.Build());
            }

            user.Element = chosenElement;
            var tags = new[] { "VenusAdept", "MarsAdept", "JupiterAdept", "MercuryAdept" };
            user.Tags.RemoveAll(s => tags.Contains(s));
            if ((int)chosenElement < tags.Length)
            {
                user.Tags.Add(tags[(int)chosenElement]);
            }
        }
        public void ChangeClass(UserAccount user, string classSeriesName = "")
        {
            user.ClassToggle = 0;
            if (!classSeriesName.IsNullOrEmpty() && AdeptClassSeriesManager.SetClass(user, classSeriesName))
            {
                var series = AdeptClassSeriesManager.GetClassSeries(user);
                if (series != null && !user.DjinnPocket.DjinnSetup.All(d => series.Elements.Contains(d)))
                {
                    user.DjinnPocket.DjinnSetup.Clear();
                    user.DjinnPocket.DjinnSetup.Add(user.Element);
                    user.DjinnPocket.DjinnSetup.Add(user.Element);
                }
            }
        }
    }
    public class StatusAction : IodemBotCommandAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

     
        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "status",
            Description = "Show your Status",
            FillParametersAsync = null
        };

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string)null)),
            RefreshAsync = RefreshAsync
        };

        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            msgProps.Embed = GetStatusEmbed(account);
            msgProps.Components = GetStatusComponent(account);
            await Task.CompletedTask;
        }
        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var embed = GetStatusEmbed(account);
            var component = GetStatusComponent(account);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: embed, components: component);
        }
        private static readonly Dictionary<Detail, char> split = new()
        {
            { Detail.none, '>' },
            { Detail.Names, ',' },
            { Detail.NameAndPrice, '\n' }
        };

        internal static Embed GetStatusEmbed(UserAccount account)
        {
            var factory = new PlayerFighterFactory();
            var p = factory.CreatePlayerFighter(account);

            var author = new EmbedAuthorBuilder();
            author.WithName($"{account.Name}");
            author.WithIconUrl(account.ImgUrl);


            var embed = new EmbedBuilder()
            .WithColor(Colors.Get(account.Element.ToString()))
            .WithAuthor(author)
            .WithTitle($"Level {account.LevelNumber} {account.GsClass} {string.Join("", account.TrophyCase.Trophies.Select(t => t.Icon))} (Rank {UserAccounts.GetRank(account) + 1})")
            .AddField("Current Equip", account.Inv.GearToString(AdeptClassSeriesManager.GetClassSeries(account).Archtype), true)
            .AddField("Psynergy", p.GetMoves(false), true)
            .AddField("Djinn", account.DjinnPocket.GetDjinns().GetDisplay(DjinnDetail.None), true)

            .AddField("Stats", p.Stats.ToString(), true)
            .AddField("Elemental Stats", p.ElStats.ToString(), true)

            .AddField("Unlocked Classes", account.BonusClasses.Count == 0 ? "none" : string.Join(", ", account.BonusClasses))

            .AddField("XP", $"{account.XP} - next in {account.XPneeded}{(account.NewGames >= 1 ? $"\n({account.TotalXP} total | {account.NewGames} resets)" : "")}", true)
            .AddField("Colosso wins | Dungeon Wins", $"{account.ServerStats.ColossoWins} | {account.ServerStats.DungeonsCompleted}", true)
            .AddField("Endless Streaks", $"Solo: {account.ServerStats.EndlessStreak.Solo} | Duo: {account.ServerStats.EndlessStreak.Duo} \nTrio: {account.ServerStats.EndlessStreak.Trio} | Quad: {account.ServerStats.EndlessStreak.Quad}", true);

            return embed.Build();
        }

        internal static MessageComponent GetStatusComponent(UserAccount account)
        {
            var inv = account.Inv;
            var builder = new ComponentBuilder();
            //add status menu button
            builder.WithButton(null, $"#{nameof(InventoryAction)}", style: ButtonStyle.Success, emote: Emote.Parse("<:Item:895957416557027369>"));
            builder.WithButton(null, $"#{nameof(ClassAction)}", style: ButtonStyle.Success, emote: Emote.Parse("<:Switch:896735785603194880>"));

            return builder.Build();
        }
    }

    public class ClassAction : IodemBotCommandAction
    {
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
            var allClasses = AdeptClassSeriesManager.allClasses;
            var allAvailableClasses = allClasses.Where(c => c.IsDefault || account.BonusClasses.Any(bc => bc.Equals(c.Name)));
            var OfElement = allAvailableClasses.Where(c => c.Elements.Contains(account.Element)).Select(c => c.Name).OrderBy(n => n);

            var embed = new EmbedBuilder();
            embed.WithTitle("Classes");
            embed.WithColor(Colors.Get(account.Element.ToString()));
            embed.AddField($"Current Class", AdeptClassSeriesManager.GetClass(account).Name);
            embed.AddField($"Available as {GoldenSunCommands.ElementIcons[account.Element]} {account.Element} Adept:", string.Join(", ", OfElement));
            embed.AddField($"Others Unlocked:", string.Join(", ", allAvailableClasses.Select(c => c.Name).Except(OfElement).OrderBy(n => n)));
            embed.WithFooter($"Total: {allAvailableClasses.Count()}/{allClasses.Count()}");
            return embed.Build();
        }

        public static MessageComponent GetClassComponent(UserAccount account)
        {
            var allClasses = AdeptClassSeriesManager.allClasses;
            var allAvailableClasses = allClasses.Where(c => c.IsDefault || account.BonusClasses.Any(bc => bc.Equals(c.Name)));
            var OfElement = allAvailableClasses.Where(c => c.Elements.Contains(account.Element)).OrderBy(n => n.Name);

            var builder = new ComponentBuilder();

            List<SelectMenuOptionBuilder> ElementOptions = new();
            foreach (var element in new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury })
            {
                ElementOptions.Add(new() 
                { 
                    Label = element.ToString(), 
                    Value = $"{element}", 
                    Default = account.Element == element, 
                    Emote = Emote.Parse(GoldenSunCommands.ElementIcons[element]) 
                });
            }
            builder.WithSelectMenu("element", $"#{nameof(ChangeAdeptAction)}.", ElementOptions);

            List<SelectMenuOptionBuilder> ClassOptions = new();
            foreach (var series in OfElement)
            {
                ClassOptions.Add(new()
                {
                    Label = series.Name,
                    Value = $"{series.Name}",
                    Default = series.Classes.Any(c => c.Name.Equals(account.GsClass))
                });
            }
            builder.WithSelectMenu("classSelect", $"#{nameof(ChangeAdeptAction)}", ClassOptions);


            builder.WithButton(label: null, customId: $"#{nameof(StatusAction)}.{account.Element}", emote: Emote.Parse("<:Status:896069873124409375>"), row:3);
            return builder.Build();
        }
    }

    // Loadout Action
}
