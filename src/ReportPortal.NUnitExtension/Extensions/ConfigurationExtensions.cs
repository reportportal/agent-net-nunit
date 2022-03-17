using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Shared.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace ReportPortal.NUnitExtension.Extensions
{
    internal static class ConfigurationExtensions
    {
        public static LaunchMode GetMode(this IConfiguration configuration)
        {
            return configuration.GetValue(ConfigurationPath.LaunchDebugMode, false)
                ? LaunchMode.Debug
                : LaunchMode.Default;
        }

        public static List<ItemAttribute> GetAttributes(this IConfiguration configuration)
        {
            var property = string.Concat("Launch", ConfigurationPath.KeyDelimeter, "Attributes");
            var defaultValue = new List<KeyValuePair<string, string>>();

            return configuration.GetKeyValues(property, defaultValue)
                .Select(a => new ItemAttribute { Key = a.Key, Value = a.Value })
                .ToList();
        }

        public static string GetDescription(this IConfiguration configuration, string defaultValue = "")
        {
            return configuration.GetValue(ConfigurationPath.LaunchDescription, defaultValue);
        }

        public static string GetName(this IConfiguration configuration, string defaultValue = "")
        {
            return configuration.GetValue(ConfigurationPath.LaunchName, defaultValue);
        }

        public static bool IsEnabled(this IConfiguration configuration)
        {
            return configuration.GetValue("Enabled", true);
        }

        public static bool IsDisabled(this IConfiguration configuration) => !IsEnabled(configuration);

        public static IEnumerable<string> GetRootNamespaces(this IConfiguration configuration, IEnumerable<string> defaultValue = null)
        {
            return configuration.GetValues<string>("rootNamespaces", defaultValue);
        }
    }
}
