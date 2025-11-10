using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stok.Helpers
{
    public static class ServiceHelper
    {
        private static IServiceProvider? _services;

        public static void Initialize(IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services ??= services;
        }

        public static T GetRequiredService<T>() where T : notnull
        {
            if (_services == null)
            {
                throw new InvalidOperationException("Service provider has not been initialized.");
            }

            return _services.GetRequiredService<T>();
        }
    }
}
