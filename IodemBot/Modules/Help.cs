using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Iodembot.Preconditions;

namespace IodemBot.Modules
{
    public class Help : ModuleBase<SocketCommandContext>
    {
        private CommandService _service;
        private int _fieldRange = 10;

        public Help(CommandService service)
        {
            _service = service;
        }

        [Command("help"), Alias("h"),
         Remarks(
             "DMs you a huge message if called without parameter - otherwise shows help to the provided command or module")]
        public async Task HelpCmd()
        {
            await Context.Channel.SendMessageAsync("Check your DMs.");

            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();

            var contextString = Context.Guild?.Name ?? "DMs with me";
            var builder = new EmbedBuilder()
            {
                Title = "Help",
                Description = $"These are the commands you can use in {contextString}. Brackets `( )` denote alternative usages.",
                Color = new Color(114, 137, 218)
            };

            foreach (var module in _service.Modules.OrderBy(m => m.Name))
            {
                await AddModuleEmbedField(module, builder);
            }

            // We have a limit of 6000 characters for a message, so we are taking first ten fields
            // and then sending the message. In the current state it will send 2 messages.
            var fields = builder.Fields.ToList();
            while (builder.ToString().Length > 6000)
            {
                builder.Fields.RemoveRange(0, fields.Count);
                var firstSet = fields.Take(_fieldRange);
                builder.Fields.AddRange(firstSet);
                if (builder.ToString().Length > 6000)
                {
                    _fieldRange--;
                    continue;
                }
                await dmChannel.SendMessageAsync("", false, builder.Build());
                fields.RemoveRange(0, _fieldRange);
                builder.Fields.RemoveRange(0, _fieldRange);
                builder.Fields.AddRange(fields);
            }

            await dmChannel.SendMessageAsync("", false, builder.Build());

            // Embed are limited to 24 Fields at max. So lets clear some stuff
            // out and then send it in multiple embeds if it is too big.
            builder.WithTitle("")
                .WithDescription("")
                .WithAuthor("");
            while (builder.Fields.Count > 24)
            {
                builder.Fields.RemoveRange(0, 25);
                await dmChannel.SendMessageAsync("", false, builder.Build());
            }
        }

        [Command("help"), Alias("h")]
        [Remarks("Shows what a specific command or module does and what parameters it takes.")]
        [Cooldown(5)]
        public async Task HelpQuery([Remainder] string query)
        {
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Title = $"Help for '{query}'"
            };

            var result = _service.Search(Context, query);
            if (query.StartsWith("module "))
            {
                query = query.Remove(0, "module ".Length);
            }

            var emb = result.IsSuccess ? HelpCommand(result, builder) : await HelpModule(query, builder);

            if (emb.Fields.Length == 0)
            {
                await ReplyAsync($"Sorry, I couldn't find anything for \"{query}\".");
                return;
            }

            await Context.Channel.SendMessageAsync("", false, emb);
        }

        private static Embed HelpCommand(SearchResult search, EmbedBuilder builder)
        {
            foreach (var match in search.Commands)
            {
                var cmd = match.Command;
                var parameters = cmd.Parameters.Select(p => string.IsNullOrEmpty(p.Summary) ? p.Name : p.Summary);
                var paramsString = $"Parameters: {string.Join(", ", parameters)}" +
                                   (string.IsNullOrEmpty(cmd.Summary) ? "" : $"\nSummary: {cmd.Summary}") +
                                   (string.IsNullOrEmpty(cmd.Remarks) ? "" : $"\nRemarks: {cmd.Remarks}");

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = paramsString;
                    x.IsInline = false;
                });
            }

            return builder.Build();
        }

        private async Task<Embed> HelpModule(string moduleName, EmbedBuilder builder)
        {
            var module = _service.Modules.ToList().Find(mod =>
                string.Equals(mod.Name, moduleName, StringComparison.CurrentCultureIgnoreCase));
            await AddModuleEmbedField(module, builder);
            return builder.Build();
        }

        private async Task AddModuleEmbedField(ModuleInfo module, EmbedBuilder builder)
        {
            if (module is null)
            {
                return;
            }

            var descriptionBuilder = new List<string>();
            var duplicateChecker = new List<string>();
            foreach (var cmd in module.Commands.OrderBy(c => c.Name))
            {
                var result = await cmd.CheckPreconditionsAsync(Context);
                if (!result.IsSuccess || duplicateChecker.Contains(cmd.Aliases.First()))
                {
                    continue;
                }

                duplicateChecker.Add(cmd.Aliases.First());
                var addDesc = string.Join("`, `", cmd.Aliases.Where(c => c.Length <= 3 && c != cmd.Aliases.First()));
                addDesc = addDesc.Length > 0 ? $" (`{addDesc}`)" : "";
                var cmdDescription = $"`{cmd.Aliases.First()}`{addDesc}";
                //var cmdDescription = $"`{string.Join("`, `", cmd.Aliases)}`";
                if (!string.IsNullOrEmpty(cmd.Summary))
                {
                    cmdDescription += $" | {cmd.Summary}";
                }

                if (!string.IsNullOrEmpty(cmd.Remarks))
                {
                    cmdDescription += $" | {cmd.Remarks}";
                }

                if (cmdDescription != "``")
                {
                    descriptionBuilder.Add(cmdDescription);
                }
            }

            if (descriptionBuilder.Count <= 0)
            {
                return;
            }

            var builtString = string.Join("\n", descriptionBuilder);
            var testLength = builtString.Length;
            if (testLength >= 1024)
            {
                Console.WriteLine(testLength);
                builtString = builtString.Substring(0, 1000);
                //throw new ArgumentException("Value cannot exceed 1024 characters");
            }
            var moduleNotes = "";
            if (!string.IsNullOrEmpty(module.Summary))
            {
                moduleNotes += $" {module.Summary}";
            }

            if (!string.IsNullOrEmpty(module.Remarks))
            {
                moduleNotes += $" {module.Remarks}";
            }

            if (!string.IsNullOrEmpty(moduleNotes))
            {
                moduleNotes += "\n";
            }

            if (!string.IsNullOrEmpty(module.Name))
            {
                builder.AddField($"__**{module.Name}:**__",
                    $"{moduleNotes} {builtString}\n\u200b");
            }
        }
    }
}