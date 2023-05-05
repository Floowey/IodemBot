using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.ColossoBattles;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using IodemBot.Preconditions;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using IodemBot.Images;

namespace IodemBot.Modules
{
    public class GoldenSunCommands : ModuleBase<SocketCommandContext>
    {
        public ColossoBattleService BattleService { get; set; }
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
                var relevantMoves = AdeptClassSeriesManager.GetMoveset(adeptClass).OfType<Psynergy>().Select(p => $"{p.Emote} {p.Name} `{p.PpCost}`");
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
        }

        [Command("Classes")]
        [Cooldown(2)]
        public async Task ListClasses()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var allClasses = AdeptClassSeriesManager.AllClasses;
            var allAvailableClasses = allClasses.Where(c => c.IsDefault || account.BonusClasses.Any(bc => bc.Equals(c.Name)));
            var ofElement = allAvailableClasses.Where(c => c.Elements.Contains(account.Element)).Select(c => c.Name).OrderBy(n => n);

            var embed = new EmbedBuilder();
            embed.WithTitle("Classes");
            embed.WithColor(Colors.Get(account.Element.ToString()));
            embed.AddField($"Available as {Emotes.GetIcon(account.Element)} {account.Element} Adept:", string.Join(", ", ofElement));
            embed.AddField("Others Unlocked:", string.Join(", ", allAvailableClasses.Select(c => c.Name).Except(ofElement).OrderBy(n => n)));
            embed.WithFooter($"Total: {allAvailableClasses.Count()}/{allClasses.Count}");
            _ = ReplyAsync(embed: embed.Build());
            await Task.CompletedTask;
        }

        [Command("class")]
        [Summary("Assign yourself to a class of your current element, or toggle through your available list.")]
        [Cooldown(2)]
        public async Task ClassToggle([Remainder] string name = "")
        {
            var account = EntityConverter.ConvertUser(Context.User);
            var curSeries = account.ClassSeries;

            var foundClass = AdeptClassSeriesManager.TryGetClassSeries(name, out AdeptClassSeries series);
            var embed = new EmbedBuilder().WithColor(Colors.Get(account.Element.ToString()));

            if (foundClass || name == "")
            {
                var success = SetClass(account, series?.Name ?? "");
                series = account.ClassSeries;
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
                    .WithDescription($"You are {Utilities.Article(account.GsClass)} {account.GsClass} now, {account.Name}.")
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

        public enum LoadoutAction
        { Show, Save, Load, Remove };

        [Command("loadout"), Alias("Loadouts")]
        [RequireUserServer]
        public async Task LoadoutTask(LoadoutAction action = LoadoutAction.Show, [Remainder] string loadoutName = "")
        {
            if (Context.User is not SocketGuildUser sgu)
            {
                return;
            }

            var user = EntityConverter.ConvertUser(sgu);
            switch (action)
            {
                case LoadoutAction.Show:
                    var embed = new EmbedBuilder();
                    if (user.Loadouts.LoadoutsList.Count > 0)
                    {
                        foreach (var item in user.Loadouts.LoadoutsList)
                        {
                            var items = item.Gear.Count > 0 ? string.Join("", item.Gear.Select(i => user.Inv.GetItem(i)?.Icon ?? "-")) : "no gear";
                            var djinn = item.Djinn.Count > 0 ? string.Join("", item.Djinn.Select(d => user.DjinnPocket.GetDjinn(d)?.Emote ?? "-")) : "no Djinn";
                            embed.AddField(item.LoadoutName,
                                $"{Emotes.GetIcon(item.Element)} {item.ClassSeries}\n" +
                                $"{items}\n" +
                                $"{djinn}"
                                , inline: true);
                        }
                    }
                    else
                    {
                        embed.WithDescription("No LoadoutsList saved.");
                    }
                    _ = ReplyAsync(embed: embed.Build());
                    break;

                case LoadoutAction.Save:
                    if (loadoutName.IsNullOrEmpty())
                    {
                        return;
                    }

                    user.Loadouts.RemoveLoadout(loadoutName);
                    if (user.Loadouts.LoadoutsList.Count >= 9)
                    {
                        _ = ReplyAsync("Loadout limit of 9 reached.");
                        return;
                    }
                    var newLoadout = Loadout.GetLoadout(user);
                    newLoadout.LoadoutName = loadoutName;
                    user.Loadouts.SaveLoadout(newLoadout);
                    UserAccountProvider.StoreUser(user);
                    _ = LoadoutTask();
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
                    _ = LoadoutTask();
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
            embed.AddField("XP", account.Xp, true);

            if (account.Oaths.IsOathActive(Oath.Oaf))
                embed.AddField("Effective", account.Xp / 4 / account.XpBoost);

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
            .WithTitle(string.Concat($"Level {account.LevelNumber} {account.GsClass} {string.Join("", account.TrophyCase.Trophies.Select(t => t.Icon))} (Rank {UserAccounts.GetRank(account) + 1})".Take(EmbedBuilder.MaxTitleLength)))
            .AddField("Current Equip", account.Inv.GearToString(account.ClassSeries.Archtype), true)
            .AddField("Psynergy", p.GetMoves(false), true)
            .AddField("Djinn", account.DjinnPocket.GetDjinns().GetDisplay(DjinnDetail.None), true)

            .AddField("Stats", p.Stats.ToString(), true)
            .AddField("Elemental Stats", p.ElStats.ToString(), true)

            .AddField("Unlocked Classes", account.BonusClasses.Count == 0 ? "none" : string.Join(", ", account.BonusClasses))

            .AddField("XP", $"{account.Xp} {(account.Oaths.IsOathActive(Oath.Oaf) ? $" (effective: {(ulong)(account.Xp / 4 / account.XpBoost)})" : "")} - next in {account.XPneeded}{(account.NewGames >= 1 ? $"\n({account.TotalXp} total | {account.NewGames} resets)" : "")}", true)
            .AddField("Colosso wins | Dungeon Wins", $"{account.ServerStats.ColossoWins} | {account.ServerStats.DungeonsCompleted}", true)
            .AddField("Endless Streaks", $"Solo: {account.ServerStats.EndlessStreak.Solo} | Duo: {account.ServerStats.EndlessStreak.Duo} \nTrio: {account.ServerStats.EndlessStreak.Trio} | Quad: {account.ServerStats.EndlessStreak.Quad}", true);

            if (user is SocketGuildUser socketGuildUser)
            {
                var footer = new EmbedFooterBuilder();
                footer.WithText("Joined this Server on " + socketGuildUser.JoinedAt.Value.Date.ToString("dd-MM-yyyy"));
                footer.WithIconUrl(Sprites.GetImageFromName("Iodem"));
                embed.WithFooter(footer);
            }
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("Trophy"), Alias("Trophies")]
        public async Task Trophies([Remainder] SocketUser user = null)
        {
            user ??= Context.User;
            var acc = EntityConverter.ConvertUser(user);
            var embed = new EmbedBuilder()
                .WithTitle($"Trophies of {acc.Name}");

            if (acc.TrophyCase.Trophies.Any())
            {
                acc.TrophyCase.Trophies.ForEach(t => embed.AddField("Trophy", $"{t.Icon}\n{t.Text}\nObtained on: {t.ObtainedOn.Date:d}", true));
            }
            else
            {
                embed.WithDescription("No trophies obtained yet.");
            }

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
                .AddField("Last Activity", account.LastXp)
                .Build());
        }

        [Command("hiddenstats"), Alias("tri")]
        [Cooldown(5)]
        [RequireStaff]
        public async Task Tri(SocketGuildUser user = null)
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

            var footer = new EmbedFooterBuilder();
            footer.WithText("Joined this Server on " + user.JoinedAt.Value.Date.ToString("dd-MM-yyyy"));
            footer.WithIconUrl(Sprites.GetImageFromName("Iodem"));
            embed.WithFooter(footer);

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

            var unlockedDungeons = account.Dungeons.Where(EnemiesDatabase.HasDungeon).Select(EnemiesDatabase.GetDungeon).Where(d => !d.Requirement.IsLocked(account));
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

            if (availablePermUnlocks.Any())
            {
                embed.AddField("<:mapopen:606236181503410176> Places Discovered", $"Available: {string.Join(", ", availablePermUnlocks)} \nUnavailable: {string.Join(", ", unavailablePermUnlocks)}");
            }
            if (availableOneTimeUnlocks.Length + unavailableOneTimeUnlocks.Length > 0)
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
            if (Context.User is not SocketGuildUser user)
            {
                return;
            }

            var embed = new EmbedBuilder();
            var account = EntityConverter.ConvertUser(Context.User);
            await ChangeElement(account, chosenElement, classSeriesName);
            UserAccountProvider.StoreUser(account);
            _ = GiveElementRole(user, account.Element);
            embed.WithColor(Colors.Get(account.Element.ToString()));
            embed.WithDescription($"Welcome to the {account.Element} Clan, {account.GsClass} {((SocketGuildUser)Context.User).DisplayName()}!");
            if (account.Oaths.IsOathOfElementActive())
                embed.WithFooter("An Oath prevents you from changing your element.");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        public async Task ChangeElement(UserAccount user, Element chosenElement, string classSeriesName = "")
        {
            if (user.Element == chosenElement)
                return;

            if (user.Oaths.IsOathOfElementActive())
                return;

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

            var series = user.ClassSeries;
            if (series != null && !user.DjinnPocket.DjinnSetup.All(d => series.Elements.Contains(d)))
            {
                user.DjinnPocket.DjinnSetup.Clear();
                user.DjinnPocket.DjinnSetup.Add(user.Element);
                user.DjinnPocket.DjinnSetup.Add(user.Element);
            }

            if (!user.Passives.GetSelectedPassive().elements?.Contains(chosenElement) ?? false)
                user.Passives.SelectedPassive = user.Passives.UnlockedPassives.FirstOrDefault(p => p.elements.Contains(chosenElement)).Name;

            var tags = new[] { "VenusAdept", "MarsAdept", "JupiterAdept", "MercuryAdept" };
            user.Tags.RemoveAll(s => tags.Contains(s));
            if ((int)chosenElement < tags.Length)
            {
                user.Tags.Add(tags[(int)chosenElement]);
            }
        }

        public async Task GiveElementRole(SocketGuildUser user, Element chosenElement)
        {
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == chosenElement.ToString() + " Adepts");
            if (chosenElement == Element.None)
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
            await user.RemoveRolesAsync(userRoles);
            _ = user.AddRoleAsync(role);
        }

        [Command("PutElementalRoles")]
        [RequireModerator]
        public async Task PutRoles([Remainder] ITextChannel channel)
        {
            var builder = new ComponentBuilder();
            builder.WithButton("Venus", $"^{nameof(ChangeAdeptAction)}.Venus", ButtonStyle.Primary, emote: Emotes.GetEmote(Element.Venus));
            builder.WithButton("Mars", $"^{nameof(ChangeAdeptAction)}.Mars", ButtonStyle.Primary, emote: Emotes.GetEmote(Element.Mars));
            builder.WithButton("Jupiter", $"^{nameof(ChangeAdeptAction)}.Jupiter", ButtonStyle.Primary, emote: Emotes.GetEmote(Element.Jupiter));
            builder.WithButton("Mercury", $"^{nameof(ChangeAdeptAction)}.Mercury", ButtonStyle.Primary, emote: Emotes.GetEmote(Element.Mercury));
            await channel.SendMessageAsync($"Choose from {Emotes.GetIcon(Element.Venus)}Venus, {Emotes.GetIcon(Element.Mars)}Mars, " +
                $"{Emotes.GetIcon(Element.Jupiter)}Jupiter, or {Emotes.GetIcon(Element.Mercury)}Mercury. You can change your role/element at any time.", components: builder.Build());
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
            if (m is not Psynergy psy)
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
            embed.AddField("PP", psy.PpCost, true);
            embed.AddField("Description", $"{psy} {(psy.HasPriority ? "Always goes first." : "")}");

            if (psy.Effects.Count > 0)
            {
                var s = string.Join("\n", psy.Effects.Select(e => $"{e}"));
                embed.AddField("Effects", s);
            }

            var classWithMove = AdeptClassSeriesManager.AllClasses.Where(d => d.Classes.Any(c => c.Movepool.Contains(psy.Name)));
            if (classWithMove.Any())
            {
                embed.AddField("Learned by", string.Join(", ", classWithMove.Select(c => c.Name)));
            }

            if (!psy.Description.IsNullOrEmpty())
                embed.WithFooter(psy.Description);

            await Context.Channel.SendMessageAsync("", false, embed.Build());
            if (Context.User is SocketGuildUser sgu)
            {
                _ = ServerGames.UserLookedUpPsynergy(sgu, (SocketTextChannel)Context.Channel);
            }
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

        [Command("compass")]
        [Summary("Reset and start a new game. Careful, your progress will be lost!")]
        public async Task SendCompass()
        {
            ImageStitcher.GenerateCompass(EntityConverter.ConvertUser(Context.User));
            await Context.Channel.SendFileAsync("compass.png");
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

            await ReplyAsync($"You will lose all your progress so far, are you really sure? However, you will get an experience boost from x{account.XpBoost:F} to x{Math.Min(2, account.XpBoost * (1 + 0.1 * (1 - Math.Exp(-(double)account.Xp / 120000)))):F}");

            response = await Context.Channel.AwaitMessage(m => m.Author == Context.User);

            if (!response.Content.Equals("Yes", StringComparison.CurrentCultureIgnoreCase))
            {
                return;
            }

            if (BattleService.UserInBattle(Context.User.Id))
            {
                await ReplyAsync("I find it highly unwise to do such things in the midst of a fight.");
                return;
            }

            if ((DateTime.Now - account.LastReset).TotalHours < 0)
            {
                await ReplyAsync("Again so fast? The procedure is quite straining on your body. You should let your new self settle in a bit.");
                return;
            }

            await ReplyAsync("Let us reverse the cycle, to a stage where you were just beginning");
            account.NewGame();
            UserAccountProvider.StoreUser(account);
            await Status();
        }

        internal static async Task AwardClassSeries(string series, IUser user, IMessageChannel channel)
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

            string curClass = avatar.ClassSeries.Name;
            avatar.BonusClasses.Add(series);
            avatar.BonusClasses.Sort();
            SetClass(avatar, curClass);
            UserAccountProvider.StoreUser(avatar);

            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription($"Congratulations, <@{avatar.Id}>! You have unlocked the {series}!");
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
            string curClass = account.ClassSeries.Name;
            if (name == "")
            {
                account.ClassToggle++;
            }
            else
            {
                account.ClassToggle++;
                while (account.ClassSeries.Name != curClass)
                {
                    if (account.ClassSeries.Name.ToLower().Contains(name.ToLower()))
                    {
                        break;
                    }

                    account.ClassToggle++;
                }
            }

            account.Tags.Remove("Warrior");
            account.Tags.Remove("Mage");
            account.Tags.Add(account.ClassSeries.Archtype.ToString());
            return !curClass.Equals(account.ClassSeries.Name);
        }
    }
}