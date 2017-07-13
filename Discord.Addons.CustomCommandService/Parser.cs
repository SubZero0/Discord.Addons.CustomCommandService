using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Discord.Addons.CustomCommandService
{
    public abstract class Parser
    {
        public abstract Task<ParseResult> ParseAsync(CommandInfo commandInfo, ICommandContext context, int startIndex, SearchResult searchResult, PreconditionResult preconditionResult = null, IServiceProvider services = null);
    }
}
