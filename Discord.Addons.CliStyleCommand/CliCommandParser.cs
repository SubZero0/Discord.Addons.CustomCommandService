using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Discord.Commands;
using Discord.Addons.CustomCommandService;

namespace Discord.Addons.CliStyleCommand
{
    public class CliCommandParser : Parser
    {
        public override async Task<ParseResult> ParseAsync(CommandInfo commandInfo, ICommandContext context, int startIndex, SearchResult searchResult, PreconditionResult preconditionResult = null, IServiceProvider services = null)
        {
            if (!searchResult.IsSuccess)
                return ParseResult.FromError(searchResult);
            if (preconditionResult != null && !preconditionResult.IsSuccess)
                return ParseResult.FromError(preconditionResult);

            string input = searchResult.Text.Substring(startIndex);
            return await ParseArgs(commandInfo, context, services, input, 0).ConfigureAwait(false);
        }

        //Original  (?<All>-(?<Parameter>[0-9a-z_@]+)(?<Separator>!?=|>=?|<=?| |:)(?<Value>"[^"\\]*(?:\\.[^"\\]*)*"|[^"]?\S+)|\S+))
        //C#        (?<All>-(?<Parameter>[0-9a-z_@]+)(?<Separator>!?=|>=?|<=?| |:)(?<Value>\"[^\"\\\\]*(?:\\\\.[^\"\\\\]*)*\"|[^\"]?\\S+)|\\S+)
        private static readonly Regex _regexParser = new Regex("(?<All>-(?<Parameter>[0-9a-z_@]+)(?<Separator>!?=|>=?|<=?| |:)(?<Value>\"[^\"\\\\]*(?:\\\\.[^\"\\\\]*)*\"|[^\"]?\\S+)|\\S+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private ParameterInfo GetParameter(IReadOnlyList<ParameterInfo> parameters, string name)
        {
            name = name.ToLower();
            var resultParam = parameters.FirstOrDefault(x => x.Name.ToLower() == name);
            if (resultParam != null)
                return resultParam;
            foreach (var param in parameters)
            {
                var aliases = param.Attributes.FirstOrDefault(x => x is ParamAliasAttribute) as ParamAliasAttribute;
                if (aliases == null)
                    continue;
                if (aliases.Aliases.Any(x => x.ToLower() == name))
                    return param;
            }
            return null;
        }

        private async Task<ParseResult> ParseArgs(CommandInfo command, ICommandContext context, IServiceProvider services, string input, int startPos)
        {
            var argList = ImmutableArray.CreateBuilder<TypeReaderResult>();
            var paramList = ImmutableArray.CreateBuilder<TypeReaderResult>();

            Dictionary<ParameterInfo, TypeReaderResult> parameters = new Dictionary<ParameterInfo, TypeReaderResult>();
            var remainder = new List<string>();
            foreach (Match match in _regexParser.Matches(input.Substring(startPos)))
            {
                if (!match.Groups["Parameter"].Success || !match.Groups["Separator"].Success || !match.Groups["Value"].Success)
                {
                    remainder.Add(match.Value);
                    continue;
                }

                string rawParameter = match.Groups["Parameter"].Value;
                string rawSeparator = match.Groups["Separator"].Value; //TODO: Something with Separator
                string rawValue = match.Groups["Value"].Value.Trim();

                ParameterInfo curParam = GetParameter(command.Parameters, rawParameter);

                if (curParam == null)
                    return ParseResult.FromError(CommandError.ParseFailed, $"Unknown parameter '{rawParameter}'");

                if (rawValue.StartsWith("\""))
                {
                    if (!rawValue.EndsWith("\""))
                        return ParseResult.FromError(CommandError.ParseFailed, $"Missing last quote mark for '{rawParameter}'");
                    else
                        rawValue = rawValue.Substring(1, rawValue.Length - 2);
                }

                var typeReaderResult = await curParam.Parse(context, rawValue, services).ConfigureAwait(false);
                if (!typeReaderResult.IsSuccess && typeReaderResult.Error != CommandError.MultipleMatches)
                    return ParseResult.FromError(typeReaderResult);

                if (curParam.IsMultiple) //Should I handle it differently? Since we know which parameter we are talking about and how many?
                    throw new InvalidOperationException("You can't use multiple"); //Not sure how to handle it right now

                parameters[curParam] = typeReaderResult;
            }

            foreach (ParameterInfo param in command.Parameters)
            {
                if (param.IsMultiple) //Dont know how to deal with it yet
                    throw new InvalidOperationException("You can't use multiple");
                TypeReaderResult toAdd;
                if (!parameters.ContainsKey(param))
                {
                    if (!param.IsOptional)
                    {
                        if (!param.IsRemainder)
                            return ParseResult.FromError(CommandError.ParseFailed, $"Missing required parameter '{param.Name}'");
                        else
                            return ParseResult.FromError(CommandError.ParseFailed, $"Missing required remainder parameter '{param.Name}'");
                    }
                    if (param.IsRemainder)
                    {
                        var typeReaderResult = await param.Parse(context, String.Join(" ", remainder), services).ConfigureAwait(false);
                        if (!typeReaderResult.IsSuccess && typeReaderResult.Error != CommandError.MultipleMatches)
                            return ParseResult.FromError(typeReaderResult);
                        toAdd = typeReaderResult;
                    }
                    else
                        toAdd = TypeReaderResult.FromSuccess(param.DefaultValue);
                }
                else
                    toAdd = parameters[param];
                argList.Add(toAdd);
            }

            return ParseResult.FromSuccess(argList.ToImmutable(), paramList.ToImmutable());
        }
    }
}
