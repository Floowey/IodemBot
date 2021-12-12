using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Preconditions;

namespace IodemBot.Modules
{
    [Name("Friendcodes")]
    [Group("fc")]
    [Cooldown(10)]
    public class FriendCodes : ModuleBase<SocketCommandContext>
    {
        public enum Code { PoGo, Switch, Ds, N3ds }

        [Command(""), Alias("")]
        [Summary("Get your Friendcode or the the FC of someone else")]
        public async Task Codes(SocketUser target = null)
        {
            target ??= Context.User;

            var user = EntityConverter.ConvertUser(target);
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithAuthor(target);

            if (user.ArePublicCodes || target.Equals(Context.User))
            {
                embed.WithDescription($"PoGo: {user.PoGoCode} \n" +
                $"Switch: {user.SwitchCode} \n" +
                $"3DS: {user.N3DsCode}");
            }
            else
            {
                embed.WithDescription("You better ask them yourselves...");
            }

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("set")]
        [Summary("Set your Friendcode for any of the following Systems: 3ds, switch, pogo")]
        [Remarks("`i!fc 3ds 0123-4567-...`")]
        public async Task SetCode(string type, [Remainder] string code)
        {
            var embed = new EmbedBuilder();
            type = type.ToLower();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithThumbnailUrl(Sprites.GetImageFromName("Iodem"));
            var account = EntityConverter.ConvertUser(Context.User);
            code = code.RemoveBadChars();
            switch (type)
            {
                case "3ds":
                case "n3ds":
                    account.N3DsCode = code;
                    embed.WithDescription("I have added your Nintendo 3DS Friend code.");
                    break;

                case "switch":
                case "sw":
                    account.SwitchCode = code;
                    embed.WithDescription("I have added your Switch Friend code.");
                    break;

                case "pogo":
                    account.PoGoCode = code;
                    embed.WithDescription("I have added your Pokemon Go Friend code.");
                    break;

                default:
                    embed.WithDescription("Not a valid code.");
                    break;
            }
            UserAccountProvider.StoreUser(account);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("public")]
        [Summary("Everyone will be able to request your Friendcodes")]
        public async Task SetPublic()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            account.ArePublicCodes = true;
            UserAccountProvider.StoreUser(account);

            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithThumbnailUrl(Sprites.GetImageFromName("Iodem"));
            embed.WithDescription("Anyone can see your codes now.");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("private")]
        [Summary("Only you can access your Friendcodes")]
        public async Task SetPrivate()
        {
            var account = EntityConverter.ConvertUser(Context.User);
            account.ArePublicCodes = false;
            UserAccountProvider.StoreUser(account);

            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithThumbnailUrl(Sprites.GetImageFromName("Iodem"));
            embed.WithDescription("Your codes are private now.");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }
    }
}