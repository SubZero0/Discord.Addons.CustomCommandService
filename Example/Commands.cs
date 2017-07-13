using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Discord.Addons.CliStyleCommand;

namespace Example
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        //CliStyle command with your CommandMarkerAttribute: CliStyle
        [Command("test"), CliStyle]
        //ParamAlias is an special attribute for CliStyle commands, that enables aliases for your parameters
        public async Task Test([ParamAlias("t")] string test, int number, IGuildUser user, [Remainder] string rest = "")
        {
            await ReplyAsync($"{test}, {number}, {user}\nRemainder: {rest}");
        }

        //Normal command
        [Command("test2")]
        public async Task Test2(string test, int number, IGuildUser user, [Remainder] string rest = "")
        {
            await ReplyAsync($"{test}, {number}, {user}\nRemainder: {rest}");
        }
    }
}
