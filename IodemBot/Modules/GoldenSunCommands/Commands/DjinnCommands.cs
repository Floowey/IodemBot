using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Preconditions;
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
                string.Join("", Enumerable.Repeat(Emotes.GetIcon(Element.Venus), summon.VenusNeeded)
                .Concat(Enumerable.Repeat(Emotes.GetIcon(Element.Mars), summon.MarsNeeded))
                .Concat(Enumerable.Repeat(Emotes.GetIcon(Element.Jupiter), summon.JupiterNeeded))
                .Concat(Enumerable.Repeat(Emotes.GetIcon(Element.Mercury), summon.MercuryNeeded))),
                true);
            var effectList = summon.Effects.Count > 0 ? string.Join("\n", summon.Effects.Select(e => e.ToString())) : "";
            var UserList = summon.EffectsOnUser?.Count > 0 ? "On User:\n" + string.Join("\n", summon.EffectsOnUser.Select(e => e.ToString())) : "";
            var PartyList = summon.EffectsOnParty?.Count > 0 ? "On Party:\n" + string.Join("\n", summon.EffectsOnParty.Select(e => e.ToString())) : "";
            embed.AddField("Description", string.Join("\n", summon.ToString(), effectList, UserList, PartyList, summon.HasPriority ? "Always goes first." : ""));

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
            var djinnPocket = EntityConverter.ConvertUser(Context.User).DjinnPocket;
            var embed = new EmbedBuilder();

            var equippedstring = string.Join("", djinnPocket.GetDjinns().Select(d => Emotes.GetIcon(d.Element)));
            if (equippedstring.IsNullOrEmpty())
            {
                equippedstring = "-";
            }
            embed.AddField("Equipped", equippedstring);

            foreach (Element e in new[] { Element.Venus, Element.Mars, Element.none, Element.Jupiter, Element.Mercury, Element.none })
            {
                if(e == Element.none)
                {
                    embed.AddField("\u200b", "\u200b", true);
                }
                else
                {
                    var djinnString = djinnPocket.Djinn.OfElement(e).GetDisplay(detail);
                    embed.AddField($"{e} Djinn", djinnString, true);
                }
            }
            var eventDjinn = djinnPocket.Djinn.Count(d => d.IsEvent);
            embed.WithFooter($"{djinnPocket.Djinn.Count()}/{djinnPocket.PocketSize}{(eventDjinn > 0 ? $"(+{eventDjinn})" : "")} Upgrade: {(djinnPocket.PocketUpgrades + 1) * 3000}");

            var summonString = string.Join(detail == DjinnDetail.Names ? ", " : "", djinnPocket.Summons.Select(s => $"{s.Emote}{(detail == DjinnDetail.Names ? $" {s.Name}" : "")}"));
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
            var acc = EntityConverter.ConvertUser(Context.User);
            var djinnPocket = acc.DjinnPocket;
            var inv = acc.Inv;
            var price = (uint)(djinnPocket.PocketUpgrades + 1) * 3000;
            if(djinnPocket.PocketSize >= 70)
            {
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithDescription($"Max Djinn pocket size reached")
                    .Build());
                return;
            }
        
            if (inv.RemoveBalance(price))
            {
                djinnPocket.PocketUpgrades++;
                UserAccountProvider.StoreUser(acc);
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
        [Summary("Take a specified djinn on your journey")]
        [Remarks("`i!djinn Take Flint`")]
        public async Task TakeDjinn([Remainder] string Names)
        {
            if (Names.Count() == 0)
            {
                return;
            }
            var user = EntityConverter.ConvertUser(Context.User);
            if (Names == "none")
            {
                user.DjinnPocket.DjinnSetup.Clear();
                UserAccountProvider.StoreUser(user);
            }
            else if (Names.Contains(','))
            {
                var parts = Names.Split(',').Select(s => s.Trim()).ToArray();
                TakeDjinn(user, parts);
            }
            else
            {
                TakeDjinn(user, Names);
            }

            _ = DjinnInv();
            await Task.CompletedTask;
        }

        public static void TakeDjinn(UserAccount user, params string[] Names)
        {
            var userDjinn = user.DjinnPocket;
            var userclass = AdeptClassSeriesManager.GetClassSeries(user);
            var chosenDjinn = Names
                .Select(n => userDjinn.GetDjinn(n))
                .Where(d => d != null)
                .OfElement(userclass.Elements)
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

        [Command("GiveDjinn")]
        [RequireStaff]
        public async Task GiveDjinn(string djinnName, [Remainder] SocketGuildUser user = null)
        {
            if (DjinnAndSummonsDatabase.TryGetDjinn(djinnName, out Djinn djinn))
            {
                var acc = EntityConverter.ConvertUser(user ?? Context.User);
                acc.DjinnPocket.AddDjinn(djinn);
                UserAccountProvider.StoreUser(acc);
            }
            await Task.CompletedTask;
        }

        [Command("GiveSummon")]
        [RequireStaff]
        public async Task GiveSummon(string summonName, [Remainder] SocketGuildUser user = null)
        {
            if (DjinnAndSummonsDatabase.TryGetSummon(summonName, out Summon summon))
            {
                var acc = EntityConverter.ConvertUser(user ?? Context.User);
                acc.DjinnPocket.AddSummon(summon);
                UserAccountProvider.StoreUser(acc);
            }
            await Task.CompletedTask;
        }

        [Command("Djinn Release")]
        [Summary("Say goodbye to a djinn.")]
        public async Task ReleaseDjinn([Remainder] string DjinnName)
        {
            _ = ReleaseDjinnHidden(DjinnName);
            await Task.CompletedTask;
        }

        private async Task ReleaseDjinnHidden(string DjinnName)
        {
            var acc = EntityConverter.ConvertUser(Context.User);
            var userDjinn = acc.DjinnPocket;
            var chosenDjinn = userDjinn.GetDjinn(DjinnName);
            if (chosenDjinn == null)
            {
                return;
            }
            await ReplyAsync(embed: new EmbedBuilder()
                .WithDescription($"Are you sure that you want to release your djinni {chosenDjinn.Emote} {chosenDjinn.Name}?")
                .Build());
            var response = await Context.Channel.AwaitMessage(m => m.Author == Context.User);
            if (response.Content.Equals("Yes", StringComparison.CurrentCultureIgnoreCase))
            {
                userDjinn.Djinn.Remove(chosenDjinn);
                UserAccountProvider.StoreUser(acc);
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
                newname = djinnandnewname.Split(',')[1].Trim().RemoveBadChars();
            }

            var account = EntityConverter.ConvertUser(Context.User);
            var userDjinn = account.DjinnPocket;
            var chosenDjinn = userDjinn.GetDjinn(DjinnName);
            if (chosenDjinn == null)
            {
                return;
            }

            chosenDjinn.Nickname = newname;
            chosenDjinn.UpdateMove();
            UserAccountProvider.StoreUser(account);
            await DjinnInv(DjinnDetail.Names);
        }
    }

    
}