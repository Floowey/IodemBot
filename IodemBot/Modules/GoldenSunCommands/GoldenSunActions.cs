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
using IodemBot.Discords.Contexts;

namespace IodemBot.Modules
{
    public class StatusAction : IodemBotCommandAction
    {
        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        [ActionParameterComponent(Name = "Page", Description ="page", Order =0, Required =false)]
        public int statusPage { get; set; } = 0;
        private static int nPages = 3;
        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
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
                    statusPage = int.Parse((string)idOptions.FirstOrDefault());
                return Task.CompletedTask;
            }
        };

        private async Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            var account = EntityConverter.ConvertUser(Context.User);
            msgProps.Embed = GetStatusEmbed(account, statusPage);
            msgProps.Components = GetStatusComponent(account, statusPage);
            await Task.CompletedTask;
        }
        public override async Task RunAsync()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var embed = GetStatusEmbed(account, statusPage);
            var component = GetStatusComponent(account, statusPage);
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: embed, components: component);
        }
        private static readonly Dictionary<Detail, char> split = new()
        {
            { Detail.none, '>' },
            { Detail.Names, ',' },
            { Detail.NameAndPrice, '\n' }
        };

        internal static Embed GetStatusEmbed(UserAccount account, int statusPage=0)
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

            switch (statusPage){
                
                case 0: // Overview
                    embed
                        .AddField("Current Equip", account.Inv.GearToString(AdeptClassSeriesManager.GetClassSeries(account).Archtype), true)
                        .AddField("Psynergy", p.GetMoves(false), true)
                        .AddField("Djinn", account.DjinnPocket.GetDjinns().GetDisplay(DjinnDetail.None), true)

                        .AddField("Stats", p.Stats.ToString(), true)
                        .AddField("Elemental Stats", p.ElStats.ToString(), true)
                        .AddField("Unleash Rate", $"{p.unleashRate}%", true)
                        .AddField("XP", $"{account.XP} - next in {account.XPneeded}{(account.NewGames >= 1 ? $"\n({account.TotalXP} total | {account.NewGames} resets)" : "")}", true);
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
                    embed
                        .AddField("Resets", account.NewGames, true)
                        .AddField("Total XP", account.TotalXP, true)
                        .AddField("Colosso wins | Dungeon Wins", $"{account.ServerStatsTotal.ColossoWins} | {account.ServerStatsTotal.DungeonsCompleted}", true);
                    break;
                default:
                    embed
                        .WithDescription("Something went wrong.");
                    break;
            }


            return embed.Build();
        }

        internal static MessageComponent GetStatusComponent(UserAccount account, int statusPage=0)
        {
            var inv = account.Inv;
            var builder = new ComponentBuilder();
            //add status menu button
            builder.WithButton("Classes", $"#{nameof(ClassAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("ClassAction"));
            builder.WithButton("Loadouts", $"#{nameof(LoadoutAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("LoadoutAction"));

            builder.WithButton("Inventory", $"#{nameof(InventoryAction)}", style: ButtonStyle.Success, emote: Emotes.GetEmote("InventoryAction"));
            builder.WithButton("Djinn", $"#{nameof(InventoryAction)}.", style: ButtonStyle.Success, emote: Emotes.GetEmote("DjinnAction"));

            var prevPage = statusPage - 1;
            var nextPage = statusPage + 1;


            builder.WithButton("◀️", $"#{nameof(StatusAction)}.{prevPage}", style: ButtonStyle.Secondary, disabled: prevPage<0, row:1);
            builder.WithButton("▶️", $"#{nameof(StatusAction)}.{nextPage}", style: ButtonStyle.Secondary, disabled: nextPage>=nPages, row:1);


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
            await ChangeAdeptAsync(Context, SelectedElement, SelectedClass);
            if (intoNew)
            {
                msgProps.Content = $"Welcome to the {SelectedElement} Clan, {Context.User.Mention}";
            } else
            {
                var user = EntityConverter.ConvertUser(Context.User);
                msgProps.Embed = ClassAction.GetClassEmbed(user);
                msgProps.Components = ClassAction.GetClassComponent(user);
            }

            await Task.CompletedTask;
        }

        public static async Task ChangeAdeptAsync(RequestContext Context, Element SelectedElement, string SelectedClass = null)
        {
            var guser = (SocketGuildUser)Context.User;
            await GiveElementRole(guser, SelectedElement, Context);
            await ChangeAdept(guser, SelectedElement, SelectedClass, Context);

            //loadedLoadout.ApplyLoadout(user);
        }
        private static async Task GiveElementRole(SocketGuildUser user, Element chosenElement, RequestContext Context)
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

        private static async Task ChangeAdept(IGuildUser guser, Element chosenElement, string classSeriesName, RequestContext Context)
        {
            var user = EntityConverter.ConvertUser(guser);
            await ChangeElement(user, chosenElement, Context);
            ChangeClass(user, classSeriesName);
            UserAccountProvider.StoreUser(user);
        }
        private static async Task ChangeElement(UserAccount user, Element chosenElement, RequestContext Context)
        {
            if (user.Element == chosenElement)
            {
                return;
            }
            foreach (string removed in user.Inv.UnequipExclusiveTo(user.Element))
            {
                var removedEmbed = new EmbedBuilder();
                removedEmbed.WithDescription($"<:Exclamatory:571309036473942026> Your {removed} was unequipped.");
                _ = Context.ReplyWithMessageAsync(EphemeralRule.EphemeralOrFail,embed: removedEmbed.Build());
            }

            user.Element = chosenElement;
            var tags = new[] { "VenusAdept", "MarsAdept", "JupiterAdept", "MercuryAdept" };
            user.Tags.RemoveAll(s => tags.Contains(s));
            if ((int)chosenElement < tags.Length)
            {
                user.Tags.Add(tags[(int)chosenElement]);
            }
        }
        private static void ChangeClass(UserAccount user, string classSeriesName = "")
        {
            user.ClassToggle = 0;
            if(!classSeriesName.IsNullOrEmpty())
                AdeptClassSeriesManager.SetClass(user, classSeriesName);
            
            var series = AdeptClassSeriesManager.GetClassSeries(user);
            if (series != null && !user.DjinnPocket.DjinnSetup.All(d => series.Elements.Contains(d)))
            {
                user.DjinnPocket.DjinnSetup.Clear();
                user.DjinnPocket.DjinnSetup.Add(user.Element);
                user.DjinnPocket.DjinnSetup.Add(user.Element);
            }
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
            embed.AddField($"Available as {Emotes.GetIcon(account.Element)} {account.Element} Adept:", string.Join(", ", OfElement));
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
                    Emote = Emotes.GetEmote(element)
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
            builder.WithButton("Status", customId: $"#{nameof(StatusAction)}",style: ButtonStyle.Primary, emote: Emotes.GetEmote("StatusAction"),row:3);
            builder.WithButton("Loadouts", $"#{nameof(LoadoutAction)}", style: ButtonStyle.Primary, emote: Emotes.GetEmote("LoadoutAction"));
            return builder.Build();
        }
    }

    public class LoadoutAction : IodemBotCommandAction
    {
        public override Task RunAsync()
        {
            throw new NotImplementedException();
        }

        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            CanRefreshAsync = _ => Task.FromResult((true, (string) null)),
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
                options.Add(new() { Label= $"{item.LoadoutName}", Value = $"{item.LoadoutName}", Emote= Emotes.GetEmote(item.Element)});
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
            { Element.Mercury, new[] {"Flowing", "Freezing", "Snowy", "Oceanic", "Aqua", "Mercury", "Blue", "Raining" } }
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
