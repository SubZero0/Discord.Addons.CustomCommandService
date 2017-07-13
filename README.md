# Discord.Addons.CustomCommandService
Now you can have custom parsers for different commands

# How to use custom parsers
1) Change your `CommandService` to `CustomizableCommandService`
2) Add your parser to the CustomizableCommandService with `AddParser<CommandMarkerAttribute>(Parser)` or `AddParser(CommandMarkerAttribute, Parser)`
3) Add your CommandMarkerAttribute in the command that you want your custom Parser
4) Profit!

You can see an example in this repo. But it actually uses `CustomizableCommandService.EnableCliStyleCommands();` to register it.

# How to create a custom parser
1) Create a new attribute that implements the abstract class `CommandMarkerAttribute`
2) Create your parser that implements the abstract class `Parser`
3) You can create an extension method for `CustomizableCommandService` that registers it, just like `CliStyleCommand` does.

You can look into `Discord.Addons.CliStyleCommand` to see how it's done.

# How to use
See the Example in this repo.