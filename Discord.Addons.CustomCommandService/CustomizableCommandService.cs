using Discord.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Addons.CustomCommandService
{
    public class CustomizableCommandService : CommandService
    {
        private readonly ConcurrentDictionary<Type, Parser> _parsers = new ConcurrentDictionary<Type, Parser>();

        public ImmutableDictionary<Type, Parser> Parsers => _parsers.ToImmutableDictionary();

        public CustomizableCommandService() : base()
        {
        }

        public CustomizableCommandService(CommandServiceConfig config) : base(config)
        {
        }

        //Parsers
        public void AddParser<T>(Parser parser) where T : CommandMarkerAttribute
        {
            _parsers[typeof(T)] = parser;
        }
        public void AddParser(CommandMarkerAttribute type, Parser parser)
        {
            _parsers[type.GetType()] = parser;
        }

        public new Task<IResult> ExecuteAsync(ICommandContext context, int argPos, IServiceProvider services = null, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
            => ExecuteAsync(context, context.Message.Content.Substring(argPos), services, multiMatchHandling);
        public async new Task<IResult> ExecuteAsync(ICommandContext context, string input, IServiceProvider services = null, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
        {
            services = services ?? EmptyServiceProvider.Instance;

            var searchResult = Search(context, input);
            if (!searchResult.IsSuccess)
                return searchResult;

            var commands = searchResult.Commands;
            var preconditionResults = new Dictionary<CommandMatch, PreconditionResult>();

            foreach (var match in commands)
            {
                preconditionResults[match] = await match.Command.CheckPreconditionsAsync(context, services).ConfigureAwait(false);
            }

            var successfulPreconditions = preconditionResults
                .Where(x => x.Value.IsSuccess)
                .ToArray();

            if (successfulPreconditions.Length == 0)
            {
                //All preconditions failed, return the one from the highest priority command
                var bestCandidate = preconditionResults
                    .OrderByDescending(x => x.Key.Command.Priority)
                    .FirstOrDefault(x => !x.Value.IsSuccess);
                return bestCandidate.Value;
            }

            //If we get this far, at least one precondition was successful.

            var parseResultsDict = new Dictionary<CommandMatch, ParseResult>();
            foreach (var pair in successfulPreconditions)
            {
                ParseResult parseResult;
                var customParser = pair.Key.Command.Attributes.FirstOrDefault(x => _parsers.ContainsKey(x.GetType()));
                if (customParser != null)
                    parseResult = await _parsers[customParser.GetType()].ParseAsync(pair.Key.Command, context, pair.Key.Alias.Length, searchResult, pair.Value, services ?? EmptyServiceProvider.Instance).ConfigureAwait(false);
                else
                    parseResult = await pair.Key.ParseAsync(context, searchResult, pair.Value, services).ConfigureAwait(false);

                if (parseResult.Error == CommandError.MultipleMatches)
                {
                    IReadOnlyList<TypeReaderValue> argList, paramList;
                    switch (multiMatchHandling)
                    {
                        case MultiMatchHandling.Best:
                            argList = parseResult.ArgValues.Select(x => x.Values.OrderByDescending(y => y.Score).First()).ToImmutableArray();
                            paramList = parseResult.ParamValues.Select(x => x.Values.OrderByDescending(y => y.Score).First()).ToImmutableArray();
                            parseResult = ParseResult.FromSuccess(argList, paramList);
                            break;
                    }
                }

                parseResultsDict[pair.Key] = parseResult;
            }

            // Calculates the 'score' of a command given a parse result
            float CalculateScore(CommandMatch match, ParseResult parseResult)
            {
                float argValuesScore = 0, paramValuesScore = 0;

                if (match.Command.Parameters.Count > 0)
                {
                    var argValuesSum = parseResult.ArgValues?.Sum(x => x.Values.OrderByDescending(y => y.Score).FirstOrDefault().Score) ?? 0;
                    var paramValuesSum = parseResult.ParamValues?.Sum(x => x.Values.OrderByDescending(y => y.Score).FirstOrDefault().Score) ?? 0;

                    argValuesScore = argValuesSum / match.Command.Parameters.Count;
                    paramValuesScore = paramValuesSum / match.Command.Parameters.Count;
                }

                var totalArgsScore = (argValuesScore + paramValuesScore) / 2;
                return match.Command.Priority + totalArgsScore * 0.99f;
            }

            //Order the parse results by their score so that we choose the most likely result to execute
            var parseResults = parseResultsDict
                .OrderByDescending(x => CalculateScore(x.Key, x.Value));

            var successfulParses = parseResults
                .Where(x => x.Value.IsSuccess)
                .ToArray();

            if (successfulParses.Length == 0)
            {
                //All parses failed, return the one from the highest priority command, using score as a tie breaker
                var bestMatch = parseResults
                    .FirstOrDefault(x => !x.Value.IsSuccess);
                return bestMatch.Value;
            }

            //If we get this far, at least one parse was successful. Execute the most likely overload.
            var chosenOverload = successfulParses[0];
            return await chosenOverload.Key.ExecuteAsync(context, chosenOverload.Value, services).ConfigureAwait(false);
        }
    }
}
