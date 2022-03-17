using System;

namespace ReportPortal.NUnitExtension.Extensions
{
    internal static class TypeExtensions
    {
        public static bool HasDefaultConstructor(this Type type)
        {
            return type.GetConstructor(Type.EmptyTypes) != null;
        }
    }
}
