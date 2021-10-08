using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Preconditions;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;

namespace IodemBot.Modules
{
    public class GoldenSunCommands : ModuleBase<SocketCommandContext>
    {
        internal static Dictionary<Element, string> ElementIcons = new Dictionary<Element, string>(){
            {Element.Venus, "<:Venus_Element:573938340219584524>"},
            {Element.Mars, "<:Mars_Element:573938340307402786>"},
            {Element.Jupiter, "<:Jupiter_Element:573938340584488987>" },
            {Element.Mercury, "<:Mercury_Element:573938340743872513>" }, {Element.none , ""}
        };

        //public enum Element { Venus, Mars, Jupiter, Mercury, None }

        [Command("awardClassSeries")]
        [Summary("Awards a given Class Series to a User")]
        [RequireModerator]
        public async Task AwardSeries(SocketGuildUser user, [Remainder] string series)
        {
            _ = AwardClassSeries(series, user, Context.Channel);
            await Task.CompletedTask;
        }

        [Command("classInfo"), Alias("ci")]
        [Summary("Show information about a class")]
        public async Task ClassInfo([Remainder] string name = "")
        {
            if (name == "")
            {
                return;
            }

            if (AdeptClassSeriesManager.TryGetClassSeries(name, out AdeptClassSeries series))
            {
                AdeptClass adeptClass = series.Classes.FirstOrDefault(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase)) ?? series.Classes.First();
                var embed = new EmbedBuilder();
                embed.WithAuthor($"{adeptClass.Name} - {series.Archtype}");
                embed.WithColor(Colors.Get(series.Elements.Select(s => s.ToString()).ToArray()));
                var relevantMoves = AdeptClassSeriesManager.GetMoveset(adeptClass).Where(m => m is Psynergy).ToList().ConvertAll(m => (Psynergy)m).ConvertAll(p => $"{p.Emote} {p.Name} `{p.PPCost}`");
                embed.AddField("Description", series.Description ?? "-");
                embed.AddField("Stats", adeptClass.StatMultipliers, true);
                embed.AddField("Elemental Stats", series.Elstats.ToString(), true);
                embed.AddField("Movepool", string.Join(" - ", relevantMoves));
                embed.AddField($"Other Classes in {series.Name}", string.Join(", ", series.Classes.Select(s => s.Name)), true);
                embed.AddField("Elements", string.Join(", ", series.Elements.Select(e => e.ToString())), true);
                await Context.Channel.SendMessageAsync("", false, embed.Build());
                if (Context.User is SocketGuildUser sgu)
                {
                    _ = ServerGames.UserLookedUpClass(sgu, (SocketTextChannel)Context.Channel);
                }
            }
            else
            {
                return;
            }
        }

        [Command("Classes")]
        [Cooldown(2)]
        public async Task ListClasses()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var allClasses = AdeptClassSeriesManager.allClasses;
            var allAvailableClasses = allClasses.Where(c => c.IsDefault || account.BonusClasses.Any(bc => bc.Equals(c.Name)));
            var OfElement = allAvailableClasses.Where(c => c.Elements.Contains(account.Element)).Select(c => c.Name).OrderBy(n => n);
            
            var embed = new EmbedBuilder();
            embed.WithTitle("Classes");
            embed.WithColor(Colors.Get(account.Element.ToString()));
            embed.AddField($"Available as {ElementIcons[account.Element]} {account.Element} Adept:", string.Join(", ", OfElement));
            embed.AddField($"Others Unlocked:", string.Join(", ", allAvailableClasses.Select(c => c.Name).Except(OfElement).OrderBy(n => n)));
            embed.WithFooter($"Total: {allAvailableClasses.Count()}/{allClasses.Count()}");
            _ = ReplyAsync(embed: embed.Build());
            await Task.CompletedTask;
        }

        [Command("class")]
        [Summary("Assign yourself to a class of your current element, or toggle through your available list.")]
        [Cooldown(2)]
        public async Task ClassToggle([Remainder] string name = "")
        {
            var account = EntityConverter.ConvertUser(Context.User);
            AdeptClassSeriesManager.TryGetClassSeries(account.GsClass, out AdeptClassSeries curSeries);
            var gotClass = AdeptClassSeriesManager.TryGetClassSeries(name, out AdeptClassSeries series);
            var embed = new EmbedBuilder().WithColor(Colors.Get(account.Element.ToString()));

            if (gotClass || name == "")
            {
                var success = SetClass(account, series?.Name ?? "");
                series = AdeptClassSeriesManager.GetClassSeries(account);
                if (curSeries.Name.Equals(series?.Name) || success)
                {
                    if (series != null && !account.DjinnPocket.DjinnSetup.All(d => series.Elements.Contains(d)))
                    {
                        account.DjinnPocket.DjinnSetup.Clear();
                        account.DjinnPocket.DjinnSetup.Add(account.Element);
                        account.DjinnPocket.DjinnSetup.Add(account.Element);
                    }
                    UserAccountProvider.StoreUser(account);
                    await Context.Channel.SendMessageAsync(embed: embed
                    .WithDescription($"You are {Article(account.GsClass)} {account.GsClass} now, {account.Name}.")
                    .Build());
                    return;
                }
                else
                {
                    await Context.Channel.SendMessageAsync(embed: embed
                        .WithDescription($":x: A {account.Element} Adept cannot get into the {series.Name}")
                        .Build());
                    return;
                }
            }

            await Context.Channel.SendMessageAsync(embed: embed
                .WithDescription($":x: I could not find the {name} class in your available classes.")
                .Build());
        }

        public enum LoadoutAction { Show, Save, Load, Remove };
        [Command("loadout"), Alias("loadouts")]
        [RequireUserServer]
        public async Task LoadoutTask(LoadoutAction action = LoadoutAction.Show, [Remainder] string loadoutName = "")
        {
            if (!(Context.User is SocketGuildUser sgu))
            {
                return;
            }

            var user = EntityConverter.ConvertUser(sgu);
            switch (action)
            {
                case LoadoutAction.Show:
                    var embed = new EmbedBuilder();
                    if (user.Loadouts.loadouts.Count > 0)
                    {
                        foreach (var item in user.Loadouts.loadouts)
                        {
                            var items = item.Gear.Count > 0 ? string.Join("", item.Gear.Select(i => user.Inv.GetItem(i)?.Icon ?? "-")) : "no gear";
                            var djinn = item.Djinn.Count > 0 ? string.Join("", item.Djinn.Select(d => user.DjinnPocket.GetDjinn(d)?.Emote ?? "-")) : "no Djinn";
                            embed.AddField(item.LoadoutName,
                                $"{ElementIcons[item.Element]} {item.ClassSeries}\n" +
                                $"{items}\n" +
                                $"{djinn}"
                                , inline: true);
                        }
                    }
                    else
                    {
                        embed.WithDescription("No loadouts saved.");
                    }
                    _ = ReplyAsync(embed: embed.Build());
                    break;
                case LoadoutAction.Save:
                    if (loadoutName.IsNullOrEmpty())
                    {
                        return;
                    }

                    user.Loadouts.RemoveLoadout(loadoutName);
                    if (user.Loadouts.loadouts.Count >= 9)
                    {
                        _ = ReplyAsync("Loadout limit of 9 reached.");
                        return;
                    }
                    var newLoadout = Loadout.GetLoadout(user);
                    newLoadout.LoadoutName = loadoutName;
                    user.Loadouts.SaveLoadout(newLoadout);
                    UserAccountProvider.StoreUser(user);
                    _ = LoadoutTask(LoadoutAction.Show);
                    break;
                case LoadoutAction.Load:
                    var loadedLoadout = user.Loadouts.GetLoadout(loadoutName);
                    if (loadedLoadout != null)
                    {
                        await GiveElementRole(sgu, loadedLoadout.Element);
                        await ChangeElement(user, loadedLoadout.Element);
                        loadedLoadout.ApplyLoadout(user);
                        UserAccountProvider.StoreUser(user);
                        _ = Status();
                    }
                    break;
                case LoadoutAction.Remove:
                    if (loadoutName.IsNullOrEmpty())
                    {
                        return;
                    }

                    user.Loadouts.RemoveLoadout(loadoutName);
                    UserAccountProvider.StoreUser(user);
                    _ = LoadoutTask(LoadoutAction.Show);
                    break;
            }
            await Task.CompletedTask;
        }

        [Command("xp"), Alias("level")]
        [Cooldown(5)]
        [Summary("See xp for your next level")]
        public async Task Xp()
        {
            var user = (SocketGuildUser)Context.User;
            var account = EntityConverter.ConvertUser(user);
            var embed = new EmbedBuilder();

            embed.WithColor(Colors.Get(account.Element.ToString()));
            var author = new EmbedAuthorBuilder();
            author.WithName(user.DisplayName());
            author.WithIconUrl(user.GetAvatarUrl());
            embed.WithAuthor(author);
            embed.AddField("Level", account.LevelNumber, true);
            embed.AddField("XP", account.XP, true);
            embed.AddField("XP to level up", account.XPneeded, true);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("status")]
        [Cooldown(5)]
        [Summary("Get information about your level and more")]
        public async Task Status([Remainder] SocketUser user = null)
        {
            user ??= Context.User;
            var account = EntityConverter.ConvertUser(user);
            var factory = new PlayerFighterFactory();
            var p = factory.CreatePlayerFighter(account);

            var author = new EmbedAuthorBuilder();
            author.WithName($"{(user is SocketGuildUser sguser ? sguser.DisplayName() : user.Username)}");
            author.WithIconUrl(user.GetAvatarUrl());


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

            if (user is SocketGuildUser socketGuildUser)
            {
                var Footer = new EmbedFooterBuilder();
                Footer.WithText("Joined this Server on " + socketGuildUser.JoinedAt.Value.Date.ToString("dd-MM-yyyy"));
                Footer.WithIconUrl(Sprites.GetImageFromName("Iodem"));
                embed.WithFooter(Footer);
            }
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("Trophy"), Alias("Trophies")]
        public async Task Trophies([Remainder] SocketUser user = null)
        {
            user ??= Context.User;
            var acc = EntityConverter.ConvertUser(user);
            if (acc.TrophyCase.Trophies.Count == 0)
            {
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"Trophies of {acc.Name}");
            acc.TrophyCase.Trophies.ForEach(t => embed.AddField("Trophy", $"{t.Icon}\n{t.Text}\nObtained on: {t.ObtainedOn.Date:d}", true));
            _ = ReplyAsync(embed: embed.Build());
            await Task.CompletedTask;
        }

        [Command("patdown")]
        [RequireStaff]
        public async Task PatDown([Remainder] SocketGuildUser user = null)
        {
            var account = EntityConverter.ConvertUser(user);

            await Context.Channel.SendMessageAsync(embed:
                new EmbedBuilder()
                .WithAuthor(user)
                .AddField("Account Created", user.CreatedAt)
                .AddField("User Joined", user.JoinedAt)
                .AddField("Status", user.Status, true)
                .AddField("Last Activity", account.LastXP)
                .Build());
        }

        [Command("hiddenstats"), Alias("tri")]
        [Cooldown(5)]
        [RequireStaff]
        public async Task Tri(SocketGuildUser user = null, bool withFile = false)
        {
            user ??= (SocketGuildUser)Context.User;
            var account = EntityConverter.ConvertUser(user);
            var embed = new EmbedBuilder();

            embed.WithColor(Colors.Get(account.Element.ToString()));
            var author = new EmbedAuthorBuilder();
            author.WithName(user.DisplayName());
            author.WithIconUrl(user.GetAvatarUrl());
            embed.WithAuthor(author);
            embed.WithThumbnailUrl(user.GetAvatarUrl());
            embed.AddField("Server Stats", JsonConvert.SerializeObject(account.ServerStats, Formatting.Indented).Replace("{", "").Replace("}", "").Replace("\"", ""));
            embed.AddField("Battle Stats", JsonConvert.SerializeObject(account.BattleStats, Formatting.Indented).Replace("{", "").Replace("}", "").Replace("\"", ""));
            embed.AddField("Account Created:", user.CreatedAt);
            embed.AddField("Unlocked Classes", account.BonusClasses.Count == 0 ? "none" : string.Join(", ", account.BonusClasses));

            var Footer = new EmbedFooterBuilder();
            Footer.WithText("Joined this Server on " + user.JoinedAt.Value.Date.ToString("dd-MM-yyyy"));
            Footer.WithIconUrl(Sprites.GetImageFromName("Iodem"));
            embed.WithFooter(Footer);

            await Context.Channel.SendMessageAsync("", false, embed.Build());
            
        }

        [Command("Dungeons"), Alias("dgs")]
        [Summary("Shows the dungeons you have discovered so far")]
        [Cooldown(5)]
        public async Task ListDungeons()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var defaultDungeons = EnemiesDatabase.DefaultDungeons.Where(d => !d.Requirement.IsLocked(account));
            var availableDefaultDungeons = defaultDungeons.Where(d => d.Requirement.Applies(account)).Select(s => s.Name).ToArray();
            var unavailableDefaultDungeons = defaultDungeons.Where(d => !d.Requirement.Applies(account)).Select(s => s.Name).ToArray();

            var unlockedDungeons = account.Dungeons.Where(s => EnemiesDatabase.HasDungeon(s)).Select(s => EnemiesDatabase.GetDungeon(s)).Where(d => !d.Requirement.IsLocked(account));
            var availablePermUnlocks = availableDefaultDungeons
                .Concat(unlockedDungeons.Where(d =>
                    !d.IsOneTimeOnly &&
                    d.Requirement.FulfilledRequirements(account))
                    .Select(s => s.Name)
                    .ToArray());
            var unavailablePermUnlocks = unavailableDefaultDungeons
                .Concat(unlockedDungeons.Where(d =>
                    !d.IsOneTimeOnly &&
                    !d.Requirement.FulfilledRequirements(account))
                    .Select(s => s.Name)
                    .ToArray());

            var availableOneTimeUnlocks = unlockedDungeons.Where(d => d.IsOneTimeOnly && d.Requirement.FulfilledRequirements(account)).Select(s => s.Name).ToArray();
            var unavailableOneTimeUnlocks = unlockedDungeons.Where(d => d.IsOneTimeOnly && !d.Requirement.FulfilledRequirements(account)).Select(s => s.Name).ToArray();

            var embed = new EmbedBuilder();
            embed.WithTitle("Dungeons");

            if (availablePermUnlocks.Count() > 0)
            {
                embed.AddField("<:mapopen:606236181503410176> Places Discovered", $"Available: {string.Join(", ", availablePermUnlocks)} \nUnavailable: {string.Join(", ", unavailablePermUnlocks)}");
            }
            if (availableOneTimeUnlocks.Count() + unavailableOneTimeUnlocks.Count() > 0)
            {
                embed.AddField("<:cave:607402486562684944> Dungeon Keys", $"Available: {string.Join(", ", availableOneTimeUnlocks)} \nUnavailable: {string.Join(", ", unavailableOneTimeUnlocks)}");
            }
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("element"), Alias("el")]
        [RequireUserServer]
        [Summary("Set your Element to one of the four. Add a class to immediately change into it, if possible")]
        [Remarks("`i!element Venus`, `i!element Mars Brute`")]
        [Cooldown(5)]
        public async Task ChooseElement(Element chosenElement, [Remainder] string classSeriesName = null)
        {
            if (!(Context.User is SocketGuildUser user))
            {
                return;
            }

            var embed = new EmbedBuilder();
            var account = EntityConverter.ConvertUser(Context.User);
            _ = GiveElementRole(user, chosenElement);
            await ChangeElement(account, chosenElement, classSeriesName);
            UserAccountProvider.StoreUser(account);
            embed.WithColor(Colors.Get(chosenElement.ToString()));
            embed.WithDescription($"Welcome to the {chosenElement} Clan, {account.GsClass} {((SocketGuildUser)Context.User).DisplayName()}!");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        public async Task ChangeElement(UserAccount user, Element chosenElement, string classSeriesName = "")
        {
            if(user.Element == chosenElement) {
                return;
            }
            foreach (string removed in user.Inv.UnequipExclusiveTo(user.Element))
            {
                var removedEmbed = new EmbedBuilder();
                removedEmbed.WithDescription($"<:Exclamatory:571309036473942026> Your {removed} was unequipped.");
                await Context.Channel.SendMessageAsync("", false, removedEmbed.Build());
            }

            user.Element = chosenElement;
            user.ClassToggle = 0;
            if (!classSeriesName.IsNullOrEmpty())
            {
                SetClass(user, classSeriesName);
            }

            var series = AdeptClassSeriesManager.GetClassSeries(user);
            if (series != null && !user.DjinnPocket.DjinnSetup.All(d => series.Elements.Contains(d)))
            {
                user.DjinnPocket.DjinnSetup.Clear();
                user.DjinnPocket.DjinnSetup.Add(user.Element);
                user.DjinnPocket.DjinnSetup.Add(user.Element);
            }

            var tags = new[] { "VenusAdept", "MarsAdept", "JupiterAdept", "MercuryAdept" };
            user.Tags.RemoveAll(s => tags.Contains(s));
            if((int)chosenElement < tags.Length)
            {
                user.Tags.Add(tags[(int)chosenElement]);
            }
        }

        public async Task GiveElementRole(SocketGuildUser user, Element chosenElement)
        {
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == chosenElement.ToString() + " Adepts");
            if (chosenElement == Element.none)
            {
                role = Context.Guild.Roles.FirstOrDefault(r => r.Name == "Exathi");
            }
            var venusRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Venus Adepts");
            var marsRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Mars Adepts");
            var jupiterRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Jupiter Adepts");
            var mercuryRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Mercury Adepts");
            var exathi = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Exathi") ?? venusRole;

            await user.RemoveRolesAsync(new IRole[] { venusRole, marsRole, jupiterRole, mercuryRole, exathi });
            _ = user.AddRoleAsync(role);
        }


        [Command("MoveInfo")]
        [Alias("Psynergy", "PsynergyInfo", "psy")]
        [Summary("Get information on moves and psynergies")]
        public async Task MoveInfo([Remainder] string name = "")
        {
            if (name == "")
            {
                return;
            }

            Move m = PsynergyDatabase.GetMove(name);
            if(!(m is Psynergy psy))
            {
                return;
            }
            if (psy.Name.ToLower().Contains("not implemented"))
            {
                var failEmbed = new EmbedBuilder();
                failEmbed.WithColor(Colors.Get("Iodem"));
                failEmbed.WithDescription("I have never heard of that kind of Psynergy");
                await Context.Channel.SendMessageAsync("", false, failEmbed.Build());
                return;
            }
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get(psy.Element.ToString()));
            embed.WithAuthor(psy.Name);
            embed.AddField("Emote", psy.Emote, true);
            embed.AddField("PP", psy.PPCost, true);
            embed.AddField("Description", $"{psy} {(psy.HasPriority ? "Always goes first." : "")}");

            if (psy.Effects.Count > 0)
            {
                var s = string.Join("\n", psy.Effects.Select(e => $"{e}"));
                embed.AddField("Effects", s);
            }

            var classWithMove = AdeptClassSeriesManager.allClasses.Where(d => d.Classes.Any(c => c.Movepool.Contains(psy.Name)));
            if (classWithMove.Count() > 0)
            {
                embed.AddField("Learned by", string.Join(", ", classWithMove.Select(c => c.Name)));
            }

            await Context.Channel.SendMessageAsync("", false, embed.Build());
            if (Context.User is SocketGuildUser sgu)
            {
                _ = ServerGames.UserLookedUpPsynergy(sgu, (SocketTextChannel)Context.Channel);
            }
        }

        [Command("rndElement"), Alias("rndEl", "randElement")]
        [Cooldown(10)]
        [Summary("Get a random Element (Use `i!element` to assign one, however)")]
        public async Task RandomElement()
        {
            var embed = new EmbedBuilder();
            Random r = new Random();

            RndElement el = (RndElement)r.Next(0, 4);

            embed.WithAuthor(el.ToString());

            switch (el)
            {
                case RndElement.Venus:
                    embed.WithColor(Colors.Get("Venus"));
                    embed.WithDescription(Utilities.GetAlert("ELEMENT_VENUS"));
                    embed.WithThumbnailUrl("https://archive-media-0.nyafuu.org/vp/image/1499/44/1499447315322.png");
                    break;

                case RndElement.Mars:
                    embed.WithColor(Colors.Get("Mars"));
                    embed.WithDescription(Utilities.GetAlert("ELEMENT_MARS"));
                    embed.WithThumbnailUrl("https://kmsmith0613.files.wordpress.com/2013/11/mars-djinni.png");
                    break;

                case RndElement.Jupiter:
                    embed.WithColor(Colors.Get("Jupiter"));
                    embed.WithDescription(Utilities.GetAlert("ELEMENT_JUPITER"));
                    embed.WithThumbnailUrl("https://pre00.deviantart.net/a1e1/th/pre/i/2014/186/f/7/golden_sun___jupiter_djinn_by_vercidium-d7pb4l3.png");
                    break;

                case RndElement.Mercury:
                    embed.WithColor(Colors.Get("Mercury"));
                    embed.WithDescription(Utilities.GetAlert("ELEMENT_MERCURY"));
                    embed.WithThumbnailUrl("http://thelostwaters.com/gallery/galleries/goldensun/official/MercuryDjinn.png");
                    break;

                default: break;
            }

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("removeClassSeries")]
        [Summary("Remove a given Class Series from a User")]
        [RequireModerator]
        public async Task RemoveSeries(SocketGuildUser user, [Remainder] string series)
        {
            await RemoveClassSeries(series, user, (SocketTextChannel)Context.Channel);
        }

        [Command("newgame")]
        [Summary("Reset and start a new game. Careful, your progress will be lost!")]
        public async Task NewGamePlus()
        {
            _ = NewGamePlusTask();
            await Task.CompletedTask;
        }

        public async Task NewGamePlusTask()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            await ReplyAsync("So you want to start over? Are you sure?");
            var response = await Context.Channel.AwaitMessage(m => m.Author == Context.User);
            if (!response.Content.Equals("Yes", StringComparison.CurrentCultureIgnoreCase))
            {
                return;
            }

            await ReplyAsync($"You will lose all your progress so far, are you really sure? However, you will get an experience boost from x{account.XpBoost:F} to x{Math.Min(2,account.XpBoost * (1 + 0.1 * (1 - Math.Exp(-(double)account.XP / 120000)))):F}");

            response = await Context.Channel.AwaitMessage(m => m.Author == Context.User);
            if (!response.Content.Equals("Yes", StringComparison.CurrentCultureIgnoreCase))
            {
                return;
            }

            if (ColossoPvE.UserInBattle(EntityConverter.ConvertUser(Context.User)))
            {
                await ReplyAsync($"I find it highly unwise to do such things in the midst of a fight.");
                return;
            }

            await ReplyAsync("Let us reverse the cycle, to a stage where you were just beginning");
            account.NewGame();
            UserAccountProvider.StoreUser(account);
            await Status();
        }

        internal static async Task AwardClassSeries(string series, SocketUser user, IMessageChannel channel)
        {
            var avatar = EntityConverter.ConvertUser(user);
            await AwardClassSeries(series, avatar, channel);
        }

        internal static async Task AwardClassSeries(string series, UserAccount avatar, IMessageChannel channel)
        {
            if (avatar.BonusClasses.Contains(series))
            {
                return;
            }

            string curClass = AdeptClassSeriesManager.GetClassSeries(avatar).Name;
            avatar.BonusClasses.Add(series);
            avatar.BonusClasses.Sort();
            SetClass(avatar, curClass);
            UserAccountProvider.StoreUser(avatar);

            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription($"Congratulations, <@{avatar.ID}>! You have unlocked the {series}!");
            if (channel == null)
            {
                return;
            }
            await channel.SendMessageAsync("", false, embed.Build());
        }

        internal static async Task RemoveClassSeries(string series, SocketGuildUser user, SocketTextChannel channel)
        {
            var avatar = EntityConverter.ConvertUser(user);
            if (!avatar.BonusClasses.Contains(series))
            {
                return;
            }

            avatar.BonusClasses.Remove(series);
            UserAccountProvider.StoreUser(avatar);
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription($"{series} was removed from {user.Mention}.");
            await channel.SendMessageAsync("", false, embed.Build());
        }

        internal static bool SetClass(UserAccount account, string name = "")
        {
            string curClass = AdeptClassSeriesManager.GetClassSeries(account).Name;
            if (name == "")
            {
                account.ClassToggle++;
            }
            else
            {
                account.ClassToggle++;
                while (AdeptClassSeriesManager.GetClassSeries(account).Name != curClass)
                {
                    if (AdeptClassSeriesManager.GetClassSeries(account).Name.ToLower().Contains(name.ToLower()))
                    {
                        break;
                    }

                    account.ClassToggle++;
                }
            }
            return !curClass.Equals(AdeptClassSeriesManager.GetClassSeries(account).Name);
        }

        private string Article(string s)
        {
            s = s.ToLower();
            char c = s.ElementAt(0);
            switch (c)
            {
                case 'a':
                case 'e':
                case 'i':
                case 'o':
                case 'u':
                    return "an";

                default:
                    return "a";
            }
        }

    }
}