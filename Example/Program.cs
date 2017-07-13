using Discord;
using Discord.Addons.CliStyleCommand;
using Discord.Addons.CustomCommandService;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Example
{
    class Program
    {
        private readonly DiscordSocketClient _client;

        private readonly IServiceCollection _map = new ServiceCollection();
        /*
         * It NEED to be a CustomizableCommandService or won't be possible to use different parsers
         */
        private readonly CustomizableCommandService _commands = new CustomizableCommandService();

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private Program()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
            });
        }

        private async Task MainAsync()
        {
            _client.Log += (log) =>
            {
                Console.WriteLine(log);
                return Task.CompletedTask;
            };

            await InitCommands();

            var vars = Environment.GetEnvironmentVariables();
            await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("BotDemonToken"));
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private IServiceProvider _services;

        private async Task InitCommands()
        {
            /*
             * This method is the one that registers CliStyleAttribute and it's parser
             */
            _commands.EnableCliStyleCommands();
            _services = _map.BuildServiceProvider();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            int pos = 0;
            if (msg.HasCharPrefix('!', ref pos))
            {
                var context = new SocketCommandContext(_client, msg);
                var result = await _commands.ExecuteAsync(context, pos, _services);
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                    await msg.Channel.SendMessageAsync(result.ErrorReason);
            }
        }
    }
}