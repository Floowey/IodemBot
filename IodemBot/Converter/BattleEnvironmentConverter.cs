using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using IodemBot.ColossoBattles;
using Microsoft.Extensions.DependencyInjection;

namespace IodemBot.Converter
{
    internal class BattleEnvironmentConverter : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var bs = services.GetRequiredService<ColossoBattleService>();
            var env = bs.GetBattleEnvironment(env => env.Name.Equals(input, StringComparison.CurrentCultureIgnoreCase));
            if (env != null)
                return Task.FromResult(TypeReaderResult.FromSuccess(env));
            else if (context.Message.MentionedChannelIds.Any()
                && bs.GetBattleEnvironment(env => env.ChannelIds.Contains(context.Message.MentionedChannelIds.First())) is BattleEnvironment fromMention)
                return Task.FromResult(TypeReaderResult.FromSuccess(fromMention));
            else if (ulong.TryParse(input, out ulong id)
                && bs.GetBattleEnvironment(env => env.ChannelIds.Contains(id)) is BattleEnvironment fromId)
                return Task.FromResult(TypeReaderResult.FromSuccess(fromId));
            else
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "No matching environment found"));
        }
    }
}