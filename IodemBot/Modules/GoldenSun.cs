using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules
{
    public class GoldenSun : ModuleBase<SocketCommandContext>
    {
        private enum RndElement { Venus, Mars, Jupiter, Mercury }

        internal static Dictionary<Element, string> ElementIcons = new Dictionary<Element, string>(){
            {Psynergy.Element.Venus, "<:Venus_Element:573938340219584524>"},
            { Psynergy.Element.Mars, "<:Mars_Element:573938340307402786>"},
            { Psynergy.Element.Jupiter, "<:Jupiter_Element:573938340584488987>" },
            { Psynergy.Element.Mercury, "<:Mercury_Element:573938340743872513>" }, {Psynergy.Element.none , ""}
        };

        //public enum Element { Venus, Mars, Jupiter, Mercury, None }

        [Command("awardClassSeries")]
        [Remarks("Awards a given Class Series to a User")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task AwardSeries(SocketGuildUser user, [Remainder] string series)
        {
            var account = UserAccounts.GetAccount(Context.User);
            await AwardClassSeries(series, user, (SocketTextChannel)Context.Channel);
        }

        [Command("classInfo")]
        public async Task ClassInfo([Remainder] string name = "")
        {
            if (name == "")
            {
                return;
            }

            if (AdeptClassSeriesManager.TryGetClassSeries(name, out AdeptClassSeries series))
            {
                AdeptClass adeptClass = series.Classes.Where(c => c.Name.ToUpper() == name.ToUpper()).FirstOrDefault();
                var embed = new EmbedBuilder();
                embed.WithAuthor($"{adeptClass.Name} - {series.Archtype}");
                embed.WithColor(Colors.Get(series.Elements.Select(s => s.ToString()).ToArray()));
                var relevantMoves = AdeptClassSeriesManager.GetMoveset(adeptClass).Where(m => m is Psynergy).ToList().ConvertAll(m => (Psynergy)m).ConvertAll(p => $"{p.emote} {p.name} `{p.PPCost}`");
                embed.AddField("Description", series.Description ?? "-");
                embed.AddField("Stats", adeptClass.StatMultipliers, true);
                embed.AddField("Elemental Stats", series.Elstats.ToString(), true);
                embed.AddField("Movepool", string.Join(" - ", relevantMoves));
                embed.AddField($"Other Classes in {series.Name}", string.Join(", ", series.Classes.Select(s => s.Name)), true);
                embed.AddField("Elements", string.Join(", ", series.Elements.Select(e => e.ToString())), true);
                await Context.Channel.SendMessageAsync("", false, embed.Build());
                _ = ServerGames.UserLookedUpClass((SocketGuildUser)Context.User, (SocketTextChannel)Context.Channel);
            }
            else
            {
                return;
            }
        }

        [Command("class")]
        [Remarks("Assign yourself to a class of your current element, or toggle through your available list.")]
        [Cooldown(2)]
        public async Task ClassToggle([Remainder] string name = "")
        {
            var account = UserAccounts.GetAccount(Context.User);
            AdeptClassSeriesManager.TryGetClassSeries(account.GsClass, out AdeptClassSeries curSeries);
            var gotClass = AdeptClassSeriesManager.TryGetClassSeries(name, out AdeptClassSeries series);
            var embed = new EmbedBuilder().WithColor(Colors.Get(account.Element.ToString()));

            if (gotClass)
            {
                var success = SetClass(account, series.Name);
                if (curSeries.Name.Equals(series.Name) || success)
                {
                    await Context.Channel.SendMessageAsync(embed: embed
                    .WithDescription($":x: You are {Article(account.GsClass)} {account.GsClass} now, {((SocketGuildUser)Context.User).DisplayName()}.")
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

        [Command("xp"), Alias("level")]
        [Cooldown(5)]
        [Remarks("Get information about your level etc")]
        public async Task Xp()
        {
            var user = (SocketGuildUser)Context.User;
            var account = UserAccounts.GetAccount(user);
            var embed = new EmbedBuilder();
            var factory = new PlayerFighterFactory();
            var p = factory.CreatePlayerFighter(user);

            embed.WithColor(Colors.Get(account.Element.ToString()));
            var author = new EmbedAuthorBuilder();
            author.WithName(user.DisplayName());
            author.WithIconUrl(user.GetAvatarUrl());
            embed.WithAuthor(author);
            embed.AddField("Level", account.LevelNumber, true);
            embed.AddField("XP", account.XP, true);
            embed.AddField("XP to level up", Leveling.XPforNextLevel(account.XP), true);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("status")]
        [Cooldown(5)]
        [Remarks("Get information about your level etc")]
        public async Task Status([Remainder] SocketGuildUser user = null)
        {
            user = user ?? (SocketGuildUser)Context.User;
            var account = UserAccounts.GetAccount(user);
            var factory = new PlayerFighterFactory();
            var p = factory.CreatePlayerFighter(user);

            var author = new EmbedAuthorBuilder();
            author.WithName($"{user.DisplayName()}");
            author.WithIconUrl(user.GetAvatarUrl());
            //embed.WithThumbnailUrl(user.GetAvatarUrl());
            //embed.WithDescription($"Status.");

            //embed.AddField("Element", account.element, true);

            var embed = new EmbedBuilder()
            .WithColor(Colors.Get(account.Element.ToString()))
            .WithAuthor(author)
            .AddField("Level", account.LevelNumber, true)
            .AddField("XP", $"{account.XP} - next in {Leveling.XPforNextLevel(account.XP)}", true)
            .AddField("Rank", UserAccounts.GetRank(user) + 1, true)

            .AddField("Class", account.GsClass, true)
            .AddField("Colosso wins | streak", $"{account.ServerStats.ColossoWins} | {account.ServerStats.ColossoHighestStreak} ", true)
            .AddField("Colosso | Showdown Streaks", $"Solo: {account.ServerStats.ColossoHighestRoundEndlessSolo} | Duo: {account.ServerStats.ColossoHighestRoundEndlessDuo} \nTrio: {account.ServerStats.ColossoHighestRoundEndlessTrio} | Quad: {account.ServerStats.ColossoHighestRoundEndlessQuad}", true)

            .AddField("Current Equip", account.Inv.GearToString(AdeptClassSeriesManager.GetClassSeries(account).Archtype), true)
            .AddField("Psynergy", p.GetMoves(false), false)

            .AddField("Stats", p.Stats.ToString(), true)
            .AddField("Elemental Stats", p.ElStats.ToString(), true)
            .AddField("Unlocked Classes", account.BonusClasses.Length == 0 ? "none" : string.Join(", ", account.BonusClasses));

            var Footer = new EmbedFooterBuilder();
            Footer.WithText("Joined this Server on " + user.JoinedAt.Value.Date.ToString("dd-MM-yyyy"));
            Footer.WithIconUrl(Sprites.GetImageFromName("Iodem"));
            embed.WithFooter(Footer);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("patdown")]
        [RequireStaff]
        public async Task PatDown([Remainder] SocketGuildUser user = null)
        {
            var account = UserAccounts.GetAccount(user);

            await Context.Channel.SendMessageAsync(embed:
                new EmbedBuilder()
                .WithAuthor(user)
                .AddField("Account Created", user.CreatedAt)
                .AddField("User Joined", user.JoinedAt)
                .AddField("Status", user.Status, true)
                .AddField("Last Activity", account.ServerStats.LastDayActive)
                .Build());
        }

        [Command("hiddenstats"), Alias("tri")]
        [Cooldown(5)]
        [RequireStaff]
        [Remarks("Hidden Information on Tri-Elemental Classes")]
        public async Task Tri([Remainder] SocketGuildUser user = null)
        {
            user = user ?? (SocketGuildUser)Context.User;
            var account = UserAccounts.GetAccount(user);
            var embed = new EmbedBuilder();
            var factory = new PlayerFighterFactory();
            var p = factory.CreatePlayerFighter(user);

            embed.WithColor(Colors.Get(account.Element.ToString()));
            var author = new EmbedAuthorBuilder();
            author.WithName(user.DisplayName());
            author.WithIconUrl(user.GetAvatarUrl());
            embed.WithAuthor(author);
            embed.WithThumbnailUrl(user.GetAvatarUrl());
            embed.AddField("Server Stats", JsonConvert.SerializeObject(account.ServerStats, Formatting.Indented).Replace("{", "").Replace("}", "").Replace("\"", ""));
            embed.AddField("Battle Stats", JsonConvert.SerializeObject(account.BattleStats, Formatting.Indented).Replace("{", "").Replace("}", "").Replace("\"", ""));
            embed.AddField("Account Created:", user.CreatedAt);
            embed.AddField("Unlocked Classes", account.BonusClasses.Length == 0 ? "none" : string.Join(", ", account.BonusClasses));

            var Footer = new EmbedFooterBuilder();
            Footer.WithText("Joined this Server on " + user.JoinedAt.Value.Date.ToString("dd-MM-yyyy"));
            Footer.WithIconUrl(Sprites.GetImageFromName("Iodem"));
            embed.WithFooter(Footer);

            //await Context.User.SendMessageAsync("", false, embed.Build());
            await Context.Channel.SendMessageAsync("", false, embed.Build());

            Console.WriteLine(JsonConvert.SerializeObject(account, Formatting.Indented));
        }

        [Command("Dungeons")]
        [Cooldown(5)]
        public async Task ListDungeons()
        {
            var account = UserAccounts.GetAccount(Context.User);
            var defaultDungeons = EnemiesDatabase.DefaultDungeons;
            var availableDefaultDungeons = defaultDungeons.Where(d => d.Requirement.Applies(account)).Select(s => s.Name).ToArray();
            var unavailableDefaultDungeons = defaultDungeons.Where(d => !d.Requirement.Applies(account)).Select(s => s.Name).ToArray();

            var unlockedDungeons = account.Dungeons.Select(s => EnemiesDatabase.GetDungeon(s));
            var availablePermUnlocks = unlockedDungeons.Where(d => !d.IsOneTimeOnly && d.Requirement.Applies(account)).Select(s => s.Name).ToArray();
            var unavailablePermUnlocks = unlockedDungeons.Where(d => !d.IsOneTimeOnly && !d.Requirement.Applies(account)).Select(s => s.Name).ToArray();

            var availableOneTimeUnlocks = unlockedDungeons.Where(d => d.IsOneTimeOnly && d.Requirement.Applies(account)).Select(s => s.Name).ToArray();
            var unavailableOneTimeUnlocks = unlockedDungeons.Where(d => d.IsOneTimeOnly && !d.Requirement.Applies(account)).Select(s => s.Name).ToArray();

            var embed = new EmbedBuilder();
            embed.WithTitle("Dungeons");
            if (defaultDungeons.Count() > 0)
            {
                embed.AddField("<:town:606236181243625493> Default Unlocks", $"Available: {string.Join(", ", availableDefaultDungeons)} \n Unavailable: {string.Join(", ", unavailableDefaultDungeons)}");
            }
            if (availablePermUnlocks.Count() > 0)
            {
                embed.AddField("<:mapopen:606236181503410176> Places Discovered", $"Available: {string.Join(", ", availablePermUnlocks)} \n Unavailable: {string.Join(", ", unavailablePermUnlocks)}");
            }
            if (availableOneTimeUnlocks.Count() > 0)
            {
                embed.AddField("<:dungeonkey:606237382047694919> Dungeon Keys", $"Available: {string.Join(", ", availableOneTimeUnlocks)} \n Unavailable: {string.Join(", ", unavailableOneTimeUnlocks)}");
            }
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("element"), Alias("el")]
        [Remarks("Get your current Element or set it to one of the four with e.g. `i!element Venus`")]
        [Cooldown(5)]
        public async Task Element(Element chosenElement, [Remainder] string classSeriesName = null)
        {
            var embed = new EmbedBuilder();
            var account = UserAccounts.GetAccount(Context.User);

            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == chosenElement.ToString() + " Adepts");
            var venusRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Venus Adepts");
            var marsRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Mars Adepts");
            var jupiterRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Jupiter Adepts");
            var mercuryRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Mercury Adepts");
            var exathi = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Exathi") ?? venusRole;
            if (role == null)
            {
                return;
            }

            if (chosenElement == account.Element)
            {
                return;
            }

            await (Context.User as IGuildUser).RemoveRolesAsync(new IRole[] { venusRole, marsRole, jupiterRole, mercuryRole, exathi });
            await (Context.User as IGuildUser).AddRoleAsync(role);

            foreach (string removed in account.Inv.UnequipExclusiveTo(account.Element))
            {
                var removedEmbed = new EmbedBuilder();
                removedEmbed.WithDescription($"<:Exclamatory:571309036473942026> Your {removed} was unequipped.");
                await Context.Channel.SendMessageAsync("", false, removedEmbed.Build());
            }

            account.Element = chosenElement;
            account.ClassToggle = 0;
            if (classSeriesName != null && classSeriesName != "")
            {
                SetClass(account, classSeriesName);
            }

            UserAccounts.SaveAccounts();
            embed.WithColor(Colors.Get(chosenElement.ToString()));
            embed.WithDescription($"Welcome to the {chosenElement.ToString()} Clan, {account.GsClass} {((SocketGuildUser)Context.User).DisplayName()}!");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("MoveInfo")]
        [Alias("Psynergy", "PsynergyInfo", "psy")]
        [Remarks("Get information on moves and psynergies")]
        public async Task MoveInfo([Remainder] string name = "")
        {
            if (name == "")
            {
                return;
            }

            Psynergy psy = PsynergyDatabase.GetPsynergy(name);
            if (psy.name.Contains("Not Implemented"))
            {
                var failEmbed = new EmbedBuilder();
                failEmbed.WithColor(Colors.Get("Iodem"));
                failEmbed.WithDescription("I have never heard of that kind of Psynergy");
                await Context.Channel.SendMessageAsync("", false, failEmbed.Build());
                return;
            }
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get(psy.element.ToString()));
            embed.WithAuthor(psy.name);
            embed.AddField("Emote", psy.emote, true);
            embed.AddField("PP", psy.PPCost, true);
            //embed.AddField("Element", psy.element, true);
            embed.AddField("Description", $"{psy.ToString()} {(psy.hasPriority ? "Always goes first." : "")}");
            var s = "none";

            if (psy.effects.Count > 0)
            {
                s = string.Join("\n", psy.effects.Select(e => $"{e.ToString()}"));
            }

            embed.AddField("Effects", s);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
            _ = ServerGames.UserLookedUpPsynergy((SocketGuildUser)Context.User, (SocketTextChannel)Context.Channel);
        }

        [Command("rndElement"), Alias("rndEl", "randElement")]
        [Cooldown(10)]
        [Remarks("Get a random Element (Use `i!element` to assign one, however)")]
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
        [Remarks("Remove a given Class Series from a User")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task RemoveSeries(SocketGuildUser user, [Remainder] string series)
        {
            var account = UserAccounts.GetAccount(Context.User);
            await RemoveClassSeries(series, user, (SocketTextChannel)Context.Channel);
        }

        [Command("sprite"), Alias("portrait")]
        [Remarks("Get a random sprite or one of a given Character")]
        [Cooldown(5)]
        public async Task Sprite([Remainder] string name = "")
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));

            if (Sprites.GetSpriteCount() == 0)
            {
                embed.WithDescription(Utilities.GetAlert("no_sprites"));
            }
            else if (name == "")
            {
                embed.WithImageUrl(Sprites.GetRandomSprite());
            }
            else
            {
                embed.WithImageUrl(Sprites.GetImageFromName(name));
            }

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        internal static async Task AwardClassSeries(string series, SocketGuildUser user, SocketTextChannel channel)
        {
            var avatar = UserAccounts.GetAccount(user);
            await AwardClassSeries(series, avatar, channel);
        }

        internal static async Task AwardClassSeries(string series, UserAccount avatar, ITextChannel channel)
        {
            if (avatar.BonusClasses.Contains(series))
            {
                return;
            }

            string curClass = AdeptClassSeriesManager.GetClassSeries(avatar).Name;
            var list = new List<string>(avatar.BonusClasses) { series };
            list.Sort();
            avatar.BonusClasses = list.ToArray();
            SetClass(avatar, curClass);
            UserAccounts.SaveAccounts();
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
            var avatar = UserAccounts.GetAccount(user);
            if (!avatar.BonusClasses.Contains(series))
            {
                return;
            }

            var list = new List<string>(avatar.BonusClasses);
            list.Remove(series);
            avatar.BonusClasses = list.ToArray();
            UserAccounts.SaveAccounts();
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription($"{series} was removed from {user.Mention}.");
            await channel.SendMessageAsync("", false, embed.Build());
        }

        private static bool SetClass(UserAccount account, string name = "")
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
            UserAccounts.SaveAccounts();
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