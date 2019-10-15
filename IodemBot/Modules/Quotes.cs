using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IodemBot.Modules
{
    public class Quotes : ModuleBase<SocketCommandContext>
    {
        private static List<QuoteStruct> quoteList = new List<QuoteStruct>();

        [Command("addQuote")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Remarks("Add a Quote to the quoteList.")]
        //add permissions
        public async Task AddQuoteCommand(string name, [Remainder] string quote)
        {
            AddQuote(name, quote);
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription(Utilities.GetAlert("quote_added"));
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("quote"), Alias("q")]
        [Cooldown(10)]
        [Remarks("Get a random quote. Add a name to get a quote from that character")]
        public async Task RandomQuote()
        {
            if (GetQuotesCount() == 0)
            {
                await NoQuotes();
                return;
            }

            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            QuoteStruct q = quoteList[(new Random()).Next(0, quoteList.Count)];
            q.name = Utilities.ToCaps(q.name);
            embed.WithAuthor(q.name);
            embed.WithThumbnailUrl(Sprites.GetImageFromName(q.name));
            embed.WithDescription(q.quote);
            if (q.quote.Contains(@"#^@%!"))
            {
                var userAccount = UserAccounts.GetAccount(Context.User);
                userAccount.ServerStats.HasQuotedMatthew = true;
                UserAccounts.SaveAccounts();
                await ServerGames.UserHasCursed((SocketGuildUser)Context.User, (SocketTextChannel)Context.Channel);
            }

            await Context.Channel.SendMessageAsync("", false, embed.Build());
            //await embed.WithDescription(Utilities.GetAlert("quote"));
        }

        [Command("quote"), Alias("q")]
        [Cooldown(10)]
        public async Task RandomQuote([Remainder] string name)
        {
            name = name.ToLower();
            if (GetQuotesCount() == 0)
            {
                await NoQuotes();
                return;
            }
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));

            //TODO: Optimize this. This is ugly.
            List<QuoteStruct> QuotesFromName = new List<QuoteStruct>();
            foreach (QuoteStruct q in quoteList)
            {
                if (q.name.Equals(name))
                {
                    QuotesFromName.Add(q);
                }
            }
            if (QuotesFromName.Count == 0)
            {
                embed.WithDescription(Utilities.GetAlert("No_Quote_From_Name"));
            }
            else
            {
                var quote = QuotesFromName[(new Random()).Next(0, QuotesFromName.Count)];
                embed.WithThumbnailUrl(Sprites.GetImageFromName(quote.name));
                embed.WithAuthor(Utilities.ToCaps(quote.name));

                embed.WithDescription(quote.quote);
                if (quote.quote.Contains(@"#^@%!"))
                {
                    var userAccount = UserAccounts.GetAccount(Context.User);
                    userAccount.ServerStats.HasQuotedMatthew = true;
                    UserAccounts.SaveAccounts();
                    await ServerGames.UserHasCursed((SocketGuildUser)Context.User, (SocketTextChannel)Context.Channel);
                }
            }
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        private async Task NoQuotes()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription(Utilities.GetAlert("no_quotes"));
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        public static void AddQuote(string name, string quote)
        {
            quoteList.Add(new QuoteStruct(name.ToLower(), quote));
            SaveData();
        }

        public static int GetQuotesCount()
        {
            return quoteList.Count;
        }

        static Quotes()
        {
            // Load data
            if (!ValidateStorageFile("SystemLang/quotes.json"))
            {
                return;
            }

            string json = File.ReadAllText("SystemLang/quotes.json");
            quoteList = JsonConvert.DeserializeObject<List<QuoteStruct>>(json);
        }

        public static void SaveData()
        {
            // Save data
            string json = JsonConvert.SerializeObject(quoteList, Formatting.Indented);
            File.WriteAllText("SystemLang/quotes.json", json);
        }

        private static bool ValidateStorageFile(string file)
        {
            if (!File.Exists(file))
            {
                File.WriteAllText(file, "");
                SaveData();
                return false;
            }
            return true;
        }

        private struct QuoteStruct
        {
            public string name;
            public string quote;

            public QuoteStruct(string name, string quote)
            {
                this.name = name;
                this.quote = quote;
            }
        }
    }
}