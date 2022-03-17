using ReportPortal.Shared.Configuration;
using System.Collections.Generic;

namespace ReportPortal.NUnitExtension.Extensions
{
    internal static class ConfigurationExtensions
    {
        public static IEnumerable<string> GetRootNamespaces(this IConfiguration configuration, IEnumerable<string> defaultValue = null)
        {
            return configuration.GetValues<string>("rootNamespaces", defaultValue);
        }
    }
}
