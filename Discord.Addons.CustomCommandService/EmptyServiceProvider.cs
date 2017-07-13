using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Addons.CustomCommandService
{
    internal class EmptyServiceProvider : IServiceProvider
    {
        public static readonly EmptyServiceProvider Instance = new EmptyServiceProvider();

        public object GetService(Type serviceType) => null;
    }
}
