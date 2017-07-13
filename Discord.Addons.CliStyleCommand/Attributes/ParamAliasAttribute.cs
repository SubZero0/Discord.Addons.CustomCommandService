using System;

namespace Discord.Addons.CliStyleCommand
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ParamAliasAttribute : Attribute
    {
        public string[] Aliases { get; }

        public ParamAliasAttribute()
        {
            Aliases = Array.Empty<string>();
        }

        public ParamAliasAttribute(params string[] aliases)
        {
            Aliases = aliases;
        }
    }
}
