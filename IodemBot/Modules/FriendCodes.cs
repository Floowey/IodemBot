using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core.UserManagement;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot.Modules
{
    [Group("fc")]
    [Cooldown(15)]
    public class FriendCodes : ModuleBase<SocketCommandContext>
    {
        public enum Code { PoGo, Switch, DS, n3ds }

        [Command("get"), Alias("")]
        [Remarks("<Optional: name> Get your Friendcode or the the FC of someone else")]
        public async Task Codes([Remainder] string arg = "")
        {
            var mentionedUser = Context.Message.MentionedUsers.FirstOrDefault();
            SocketUser target = mentionedUser ?? Context.User;

            var user = UserAccounts.GetAccount(target);
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));

            if (user.arePublicCodes || target.Equals(Context.User))
            {
                embed.WithDescription($"PoGo: {user.PoGoCode} \n" +
                $"Switch: {user.SwitchCode} \n" +
                $"3DS: {user.N3DSCode}");
            }
            else
            {
                embed.WithDescription(Utilities.GetAlert("CODE_IS_PRIVATE"));
            }

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("set")]
        [Remarks("<Optional: Type (3ds | switch | pogo)> Set your Friendcode for a given System")]
        public async Task SetCode(string type, [Remainder] string code)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            var account = UserAccounts.GetAccount(Context.User);

            switch (type)
            {
                case "3ds":
                case "n3ds":
                    account.N3DSCode = code;
                    embed.WithDescription(Utilities.GetFormattedAlert("FC_ADDED_SUCCESS", "3DS"));
                    break;

                case "switch":
                    account.SwitchCode = code;
                    embed.WithDescription(Utilities.GetFormattedAlert("FC_ADDED_SUCCESS", "Switch"));
                    break;

                case "pogo":
                    account.PoGoCode = code;
                    embed.WithDescription(Utilities.GetFormattedAlert("FC_ADDED_SUCCESS", "Pokemon Go"));
                    break;

                default:
                    embed.WithDescription(Utilities.GetAlert("FC_CODE_UNKNOWN"));
                    break;
            }
            UserAccounts.SaveAccounts();
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("public")]
        [Remarks("Everyone will be able to request your Friendcodes")]
        public async Task SetPublic()
        {
            var account = UserAccounts.GetAccount(Context.User);
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            account.arePublicCodes = true;
            embed.WithDescription(Utilities.GetAlert("FC_PUBLIC"));
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("private")]
        [Remarks("Only you can access your Friendcodes")]
        public async Task SetPrivate()
        {
            var account = UserAccounts.GetAccount(Context.User);
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            account.arePublicCodes = false;
            embed.WithDescription(Utilities.GetAlert("FC_PRIVATE"));
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }
    }
}