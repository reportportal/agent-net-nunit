namespace ReportPortal.NUnitExtension.Extensions
{
    internal static class StringExtensions
    {
        public static bool HasValue(this string str) => !string.IsNullOrEmpty(str);
    }
}
