using System;

namespace Discord.Addons.CustomCommandService
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public abstract class CommandMarkerAttribute : Attribute
    {
    }
}
