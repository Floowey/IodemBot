using Discord;
using Discord.Commands;
using Iodembot.Preconditions;
using IodemBot.Core.UserManagement;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class DjinnCommands : ModuleBase<SocketCommandContext>
    {
        [Command("djinninfo")]
        [Cooldown(5)]
        [Remarks("Gets information on specified equipment. Example: `i!iteminfo Wheat Sword`")]
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

        public enum DjinnDetail { None, Name }

        [Command("Djinn")]
        public async Task DjinnInv()
        {
            var djinnPocket = UserAccounts.GetAccount(Context.User).DjinnPocket;
        }
    }
}