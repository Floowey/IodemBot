using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Modules.GoldenSunMechanics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules
{
    public class GoldenSun : ModuleBase<SocketCommandContext>
    {
        private enum rndElement { Venus, Mars, Jupiter, Mercury }

        //public enum Element { Venus, Mars, Jupiter, Mercury, None }

        [Command("awardClassSeries")]
        [Remarks("Awards a given Class Series to a User")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task awardSeries(SocketGuildUser user, [Remainder] string series)
        {
            var account = UserAccounts.GetAccount(Context.User);
            await AwardClassSeries(series, user, (SocketTextChannel)Context.Channel);
        }

        [Command("classInfo")]
        public async Task classInfo([Remainder] string name = "")
        {
            if (name == "")
            {
                return;
            }

            if (AdeptClassSeriesManager.TryGetClassSeries(name, out AdeptClassSeries series))
            {
                AdeptClass adeptClass = series.classes.Where(c => c.name.ToUpper() == name.ToUpper()).FirstOrDefault();
                var embed = new EmbedBuilder();
                embed.WithAuthor($"{adeptClass.name}");
                embed.WithColor(Colors.get(series.elements.Select(s => s.ToString()).ToArray()));
                var relevantMoves = AdeptClassSeriesManager.getMoveset(adeptClass).Where(m => m is Psynergy).ToList().ConvertAll(m => (Psynergy)m).ConvertAll(p => $"{p.emote} {p.name} `{p.PPCost}`");
                embed.AddField("Description", series.description ?? "-");
                embed.AddField("Stats", adeptClass.statMultipliers, true);
                embed.AddField("Elemental Stats", series.elstats.ToString(), true);
                embed.AddField("Movepool", string.Join(" - ", relevantMoves));
                embed.AddField($"Other Classes in {series.name}", string.Join(", ", series.classes.Select(s => s.name)), true);
                embed.AddField("Elements", string.Join(", ", series.elements.Select(e => e.ToString())), true);
                await Context.Channel.SendMessageAsync("", false, embed.Build());
                ServerGames.UserLookedUpClass((SocketGuildUser)Context.User, (SocketTextChannel)Context.Channel);
            }
            else
            {
                return;
            }
        }

        [Command("class")]
        [Remarks("Assign yourself to a class of your current element, or toggle through your available list.")]
        [Cooldown(2)]
        public async Task classToggle([Remainder] string name = "")
        {
            var account = UserAccounts.GetAccount(Context.User);
            setClass(account, name);

            var embed = new EmbedBuilder();
            embed.WithDescription($"You are {article(account.gsClass)} {account.gsClass} now, {Context.User.Username}.");
            embed.WithColor(Colors.get(account.element.ToString()));
            //embed.WithThumbnailUrl(Sprites.get)
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("element"), Alias("el")]
        [Remarks("Get your current Element or set it to one of the four with e.g. `i!element Venus`")]
        [Cooldown(5)]
        public async Task element([Remainder] string element = "")
        {
            var embed = new EmbedBuilder();
            var account = UserAccounts.GetAccount(Context.User);

            if (element.Length > 0)
            {
                //Assign new Element
                if (!Enum.TryParse(element, true, out Element chosenElement))
                {
                    embed.WithColor(Colors.get("Iodem"));
                    embed.WithDescription(Utilities.GetFormattedAlert("CLAN_NOT_FOUND", element));
                    await Context.Channel.SendMessageAsync("", false, embed.Build());
                    return;
                }
                var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == chosenElement.ToString() + " Adepts");
                var venusRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Venus Adepts");
                var marsRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Mars Adepts");
                var jupiterRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Jupiter Adepts");
                var mercuryRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Mercury Adepts");
                var exathi = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Exathi") ?? venusRole;
                if (role == null)
                {
                    role = exathi;
                }

                await (Context.User as IGuildUser).RemoveRolesAsync(new IRole[] { venusRole, marsRole, jupiterRole, mercuryRole, exathi });
                await (Context.User as IGuildUser).AddRoleAsync(role);
                account.element = chosenElement;
                account.classToggle = 0;
                UserAccounts.SaveAccounts();
                embed.WithColor(Colors.get(chosenElement.ToString()));
                embed.WithDescription($"Welcome to the {chosenElement.ToString()} Clan, {account.gsClass} {Context.User.Username}!");
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
            else
            {
                //Return current Element
                element = account.element.ToString();
                embed.WithColor(Colors.get(element));
                embed.WithDescription($"{account.gsClass} {Context.User.Username} is a {element} Adept");
                await Context.Channel.SendMessageAsync("", false, embed.Build());
            }
        }

        [Command("MoveInfo")]
        [Alias("Psynergy", "PsynergyInfo", "psy")]
        [Remarks("Get information on moves and psynergies")]
        public async Task moveInfo([Remainder] string name = "")
        {
            if (name == "")
            {
                return;
            }

            Psynergy psy = PsynergyDatabase.GetPsynergy(name);
            if (psy.name.Contains("Not Implemented"))
            {
                var failEmbed = new EmbedBuilder();
                failEmbed.WithColor(Colors.get("Iodem"));
                failEmbed.WithDescription("I have never heard of that kind of Psynergy");
                await Context.Channel.SendMessageAsync("", false, failEmbed.Build());
                return;
            }
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.get(psy.element.ToString()));
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
            ServerGames.UserLookedUpPsynergy((SocketGuildUser)Context.User, (SocketTextChannel)Context.Channel);
        }

        [Command("rndElement"), Alias("rndEl", "randElement")]
        [Cooldown(10)]
        [Remarks("Get a random Element (Use `i!element` to assign one, however)")]
        public async Task RandomElement()
        {
            var embed = new EmbedBuilder();
            Random r = new Random();

            rndElement el = (rndElement)r.Next(0, 4);

            embed.WithAuthor(el.ToString());

            switch (el)
            {
                case rndElement.Venus:
                    embed.WithColor(Colors.get("Venus"));
                    embed.WithDescription(Utilities.GetAlert("ELEMENT_VENUS"));
                    embed.WithThumbnailUrl("https://archive-media-0.nyafuu.org/vp/image/1499/44/1499447315322.png");
                    break;

                case rndElement.Mars:
                    embed.WithColor(Colors.get("Mars"));
                    embed.WithDescription(Utilities.GetAlert("ELEMENT_MARS"));
                    embed.WithThumbnailUrl("https://kmsmith0613.files.wordpress.com/2013/11/mars-djinni.png");
                    break;

                case rndElement.Jupiter:
                    embed.WithColor(Colors.get("Jupiter"));
                    embed.WithDescription(Utilities.GetAlert("ELEMENT_JUPITER"));
                    embed.WithThumbnailUrl("https://pre00.deviantart.net/a1e1/th/pre/i/2014/186/f/7/golden_sun___jupiter_djinn_by_vercidium-d7pb4l3.png");
                    break;

                case rndElement.Mercury:
                    embed.WithColor(Colors.get("Mercury"));
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
        public async Task removeSeries(SocketGuildUser user, [Remainder] string series)
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
            embed.WithColor(Colors.get("Iodem"));

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

        internal static async Task AwardClassSeries(string series, UserAccount avatar, SocketTextChannel channel)
        {
            if (avatar.BonusClasses.Contains(series))
            {
                return;
            }

            string curClass = AdeptClassSeriesManager.getClassSeries(avatar).name;
            var list = new List<string>(avatar.BonusClasses) { series };
            list.Sort();
            avatar.BonusClasses = list.ToArray();
            setClass(avatar, curClass);
            UserAccounts.SaveAccounts();
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.get("Iodem"));
            embed.WithDescription($"Congratulations, {channel.Users.Where(u => u.Id == avatar.ID).FirstOrDefault().Mention}! You have unlocked the {series}!");
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
            embed.WithColor(Colors.get("Iodem"));
            embed.WithDescription($"{series} was removed from {user.Mention}.");
            await channel.SendMessageAsync("", false, embed.Build());
        }

        private static void setClass(UserAccount account, string name = "")
        {
            if (name == "")
            {
                account.classToggle++;
            }
            else
            {
                string curClass = AdeptClassSeriesManager.getClassSeries(account).name;
                account.classToggle++;
                while (AdeptClassSeriesManager.getClassSeries(account).name != curClass)
                {
                    if (AdeptClassSeriesManager.getClassSeries(account).name.ToLower().Contains(name.ToLower()))
                    {
                        break;
                    }

                    account.classToggle++;
                }
            }
            UserAccounts.SaveAccounts();
        }
        private string article(string s)
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