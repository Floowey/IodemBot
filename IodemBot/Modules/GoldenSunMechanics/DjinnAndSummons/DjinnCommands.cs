using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public enum DjinnDetail { None, Name }

    public class DjinnCommands : ModuleBase<SocketCommandContext>
    {
        [Command("djinninfo"), Alias("di")]
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
            var effectList = djinn.Effects.Count > 0 ? string.Join("\n", djinn.Effects.Select(e => $"{e.ToString()}")) : "";
            embed.AddField("Description", string.Join("\n", djinn.ToString(), effectList, djinn.HasPriority ? "Always goes first." : ""));

            embed.WithColor(Colors.Get(djinn.Element.ToString()));

            _ = Context.Channel.SendMessageAsync("", false, embed.Build());
            await Task.CompletedTask;
        }

        [Command("summoninfo"), Alias("si")]
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
            var effectList = summon.Effects.Count > 0 ? string.Join("\n", summon.Effects.Select(e => $"{e.ToString()}")) : "";
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
                embed.AddField($"{e.ToString()} Djinn", djinnString);
            }

            var summonString = string.Join(detail == DjinnDetail.Name ? ", " : "", djinnPocket.summons.Select(s => $"{s.Emote}{(detail == DjinnDetail.Name ? $" {s.Name}" : "")}"));
            if (summonString.IsNullOrEmpty())
            {
                summonString = "-";
            }
            embed.AddField("Summons", summonString);
            await ReplyAsync(embed: embed.Build());
        }

        [Command("Djinn Take")]
        public async Task TakeDjinn(params string[] Names)
        {
            if (Names.Count() == 0)
            {
                return;
            }
            var user = UserAccounts.GetAccount(Context.User);
            var userDjinn = user.DjinnPocket;
            var chosenDjinn = userDjinn.djinn
                .OfElement(AdeptClassSeriesManager.GetClassSeries(user).Elements)
                .Where(d => Names.Any(n => n.Equals(d.Djinnname, StringComparison.CurrentCultureIgnoreCase)) || Names.Any(n => n.Equals(d.Nickname, StringComparison.CurrentCultureIgnoreCase)))
                .Take(DjinnPocket.MaxDjinn)
                .ToList();
            userDjinn.DjinnSetup = chosenDjinn.Select(d => d.Element).ToList();
            chosenDjinn.ForEach(d =>
            {
                userDjinn.djinn.Remove(d);
                userDjinn.djinn = userDjinn.djinn.Prepend(d).ToList();
            });
            await DjinnInv();
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

        [Command("Djinn Nickname")]
        public async Task NicknameDjinn(string DjinnName, string Nickname = "")
        {
            var userDjinn = UserAccounts.GetAccount(Context.User).DjinnPocket;
            var chosenDjinn = userDjinn.djinn
                .Where(d => DjinnName.Equals(d.Djinnname, StringComparison.CurrentCultureIgnoreCase) || DjinnName.Equals(d.Nickname, StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();
            if (chosenDjinn == null)
            {
                return;
            }

            chosenDjinn.Nickname = Nickname;
            chosenDjinn.UpdateMove();
            await DjinnInv(DjinnDetail.Name);
        }
    }
}