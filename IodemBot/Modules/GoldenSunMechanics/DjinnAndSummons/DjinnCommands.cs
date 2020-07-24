using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    [Name("DjinnAndSummons")]
    public class DjinnCommands : ModuleBase<SocketCommandContext>
    {
        [Command("djinninfo"), Alias("di")]
        [Summary("Shows information about the specified djinn")]
        [Cooldown(5)]
        public async Task DjinnInfo([Remainder] string name = "")
        {
            if (name == "")
            {
                return;
            }

            if (!DjinnAndSummonsDatabase.TryGetDjinn(name, out Djinn djinn))
            {
                var emb = new EmbedBuilder();
                emb.WithDescription(":x: There is no such spirit with that description!");
                emb.WithColor(Colors.Get("Error"));
                await Context.Channel.SendMessageAsync("", false, emb.Build());
                return;
            }

            var embed = new EmbedBuilder();
            embed.WithAuthor($"{djinn.Name}");
            embed.AddField("Icon", djinn.Emote, true);
            embed.AddField("Stats", djinn.Stats.NonZerosToString(), true);
            var effectList = djinn.Effects.Count > 0 ? string.Join("\n", djinn.Effects.Select(e => e.ToString())) : "";
            embed.AddField("Description", string.Join("\n", djinn.ToString(), effectList, djinn.HasPriority ? "Always goes first." : ""));

            embed.WithColor(Colors.Get(djinn.Element.ToString()));

            _ = Context.Channel.SendMessageAsync("", false, embed.Build());
            await Task.CompletedTask;
        }

        [Command("summoninfo"), Alias("si")]
        [Summary("Shows information about the specified summon")]
        [Cooldown(5)]
        public async Task SummonInfo([Remainder] string name = "")
        {
            if (name == "")
            {
                return;
            }

            if (!DjinnAndSummonsDatabase.TryGetSummon(name, out Summon summon))
            {
                var emb = new EmbedBuilder();
                emb.WithDescription(":x: There is no such spirit with that description!");
                emb.WithColor(Colors.Get("Error"));
                await Context.Channel.SendMessageAsync("", false, emb.Build());
                return;
            }

            var embed = new EmbedBuilder();
            embed.WithAuthor($"{summon.Name}");
            embed.AddField("Icon", summon.Emote, true);
            embed.AddField("Needed",
                string.Join("", Enumerable.Repeat(GoldenSun.ElementIcons[Element.Venus], summon.VenusNeeded)
                .Concat(Enumerable.Repeat(GoldenSun.ElementIcons[Element.Mars], summon.MarsNeeded))
                .Concat(Enumerable.Repeat(GoldenSun.ElementIcons[Element.Jupiter], summon.JupiterNeeded))
                .Concat(Enumerable.Repeat(GoldenSun.ElementIcons[Element.Mercury], summon.MercuryNeeded))),
                true);
            var effectList = summon.Effects.Count > 0 ? string.Join("\n", summon.Effects.Select(e => e.ToString())) : "";
            embed.AddField("Description", string.Join("\n", summon.ToString(), effectList, summon.HasPriority ? "Always goes first." : ""));

            embed.WithColor(Colors.Get(
                Enumerable.Repeat(Element.Venus.ToString(), summon.VenusNeeded)
                .Concat(Enumerable.Repeat(Element.Mars.ToString(), summon.MarsNeeded))
                .Concat(Enumerable.Repeat(Element.Jupiter.ToString(), summon.JupiterNeeded))
                .Concat(Enumerable.Repeat(Element.Mercury.ToString(), summon.MercuryNeeded))
                .ToArray()
                ));

            _ = Context.Channel.SendMessageAsync("", false, embed.Build());
            await Task.CompletedTask;
        }

        [Command("Djinn")]
        [Summary("Displays your Djinn pocket")]
        public async Task DjinnInv(DjinnDetail detail = DjinnDetail.None)
        {
            var djinnPocket = UserAccounts.GetAccount(Context.User).DjinnPocket;
            var embed = new EmbedBuilder();

            var equippedstring = string.Join("", djinnPocket.GetDjinns().Select(d => GoldenSun.ElementIcons[d.Element]));
            if (equippedstring.IsNullOrEmpty())
            {
                equippedstring = "-";
            }
            embed.AddField("Equipped", equippedstring);

            foreach (Element e in new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury })
            {
                var djinnString = djinnPocket.djinn.OfElement(e).GetDisplay(detail);
                embed.AddField($"{e} Djinn", djinnString, true);
            }
            embed.WithFooter($"{djinnPocket.djinn.Count()}/{djinnPocket.PocketSize} Upgrade: {(djinnPocket.PocketUpgrades + 1) * 3000}");

            var summonString = string.Join(detail == DjinnDetail.Names ? ", " : "", djinnPocket.summons.Select(s => $"{s.Emote}{(detail == DjinnDetail.Names ? $" {s.Name}" : "")}"));
            if (summonString.IsNullOrEmpty())
            {
                summonString = "-";
            }
            embed.AddField("Summons", summonString);
            await ReplyAsync(embed: embed.Build());
        }

        [Command("UpgradeDjinn")]
        [Summary("Increase the size of your djinn pocket by two slots")]
        public async Task DjinnUpgrade()
        {
            var acc = UserAccounts.GetAccount(Context.User);
            var djinnPocket = acc.DjinnPocket;
            var inv = acc.Inv;
            var price = (uint)(djinnPocket.PocketUpgrades + 1) * 3000;
            if (inv.RemoveBalance(price))
            {
                djinnPocket.PocketUpgrades++;
                await DjinnInv();
            }
            else
            {
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithDescription($":x: Not enough funds. Next upgrade {price}<:coin:569836987767324672>.")
                    .Build());
            }
        }

        [Command("Djinn Take")]
        [Summary("Take up to two specified djinn on your journey")]
        [Remarks("`i!djinn Take Flint Echo`")]
        public async Task TakeDjinn(params string[] Names)
        {
            if (Names.Count() == 0)
            {
                return;
            }
            if (Names[0].Contains(',')) Names = Names[0].Split(',').Select(p => p.Trim()).ToArray();
            var user = UserAccounts.GetAccount(Context.User);
            TakeDjinn(user, Names);
            await DjinnInv();
        }

        public static void TakeDjinn(UserAccount user, string[] Names)
        {
            var userDjinn = user.DjinnPocket;
            var userclass = AdeptClassSeriesManager.GetClassSeries(user);
            var chosenDjinn = Names
                .Select(n => userDjinn.GetDjinn(n))
                .OfElement(userclass.Elements)
                .Take(DjinnPocket.MaxDjinn)
                .ToList();
               
            chosenDjinn.ForEach(d =>
            {
                userDjinn.djinn.Remove(d);
                userDjinn.djinn = userDjinn.djinn.Prepend(d).ToList();
            });
            userDjinn.DjinnSetup = chosenDjinn.Select(d => d.Element).ToList();
        }

        [Command("GiveDjinn")]
        [RequireModerator]
        public async Task GiveDjinn(string djinnName, [Remainder] SocketGuildUser user = null)
        {
            if (DjinnAndSummonsDatabase.TryGetDjinn(djinnName, out Djinn djinn))
            {
                UserAccounts.GetAccount(user ?? Context.User).DjinnPocket.AddDjinn(djinn);
                await DjinnInv();
            }
        }

        [Command("GiveSummon")]
        [RequireModerator]
        public async Task GiveSummon(string summonName, [Remainder] SocketGuildUser user = null)
        {
            if (DjinnAndSummonsDatabase.TryGetSummon(summonName, out Summon summon))
            {
                UserAccounts.GetAccount(user ?? Context.User).DjinnPocket.AddSummon(summon);
                await DjinnInv();
            }
        }

        [Command("Djinn Release")]
        [Summary("Say goodbye to a djinn.")]
        public async Task ReleaseDjinn(string DjinnName)
        {
            _ = ReleaseDjinnHidden(DjinnName);
            await Task.CompletedTask;
        }

        private async Task ReleaseDjinnHidden(string DjinnName)
        {
            var userDjinn = UserAccounts.GetAccount(Context.User).DjinnPocket;
            var chosenDjinn = userDjinn.djinn
                .Where(d => DjinnName.Equals(d.Djinnname, StringComparison.CurrentCultureIgnoreCase) || DjinnName.Equals(d.Nickname, StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();
            if (chosenDjinn == null)
            {
                return;
            }
            await ReplyAsync(embed: new EmbedBuilder()
                .WithDescription($"Are you sure that you want to release your djinni {chosenDjinn.Emote} {chosenDjinn.Name}")
                .Build());
            var response = await Context.Channel.AwaitMessage(m => m.Author == Context.User);
            if (response.Content.Equals("Yes", StringComparison.CurrentCultureIgnoreCase))
            {
                userDjinn.djinn.Remove(chosenDjinn);
                _ = ReplyAsync(embed: new EmbedBuilder().WithDescription($"You set {chosenDjinn.Emote} {chosenDjinn.Name} free, who swiftly rushes off to find another master.").Build());
            }
        }

        [Command("Djinn Rename")]
        [Alias("Djinn Nickname")]
        [Summary("Rename one of your djinn")]
        [Remarks("`i!djinn rename Echo, YODEL Yodel yodel`")]
        public async Task NicknameDjinn([Remainder] string djinnandnewname)
        {
            var DjinnName = djinnandnewname;
            var newname = "";
            if (djinnandnewname.Contains(','))
            {
                DjinnName = djinnandnewname.Split(',')[0].Trim();
                newname = djinnandnewname.Split(',')[1].Trim();
            }

            var userDjinn = UserAccounts.GetAccount(Context.User).DjinnPocket;
            var chosenDjinn = userDjinn.djinn
                .Where(d => DjinnName.Equals(d.Djinnname, StringComparison.CurrentCultureIgnoreCase) || DjinnName.Equals(d.Nickname, StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();
            if (chosenDjinn == null)
            {
                return;
            }

            chosenDjinn.Nickname = newname;
            chosenDjinn.UpdateMove();
            await DjinnInv(DjinnDetail.Names);
        }
    }
}