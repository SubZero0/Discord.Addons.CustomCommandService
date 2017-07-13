using Discord.Addons.CustomCommandService;

namespace Discord.Addons.CliStyleCommand
{
    public static class CliStyleCommand
    {
        public static void EnableCliStyleCommands(this CustomizableCommandService service)
        {
            service.AddParser<CliStyleAttribute>(new CliCommandParser());
        }
    }
}
