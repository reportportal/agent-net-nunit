using System;

namespace ReportPortal.NUnitExtension.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class ReportKeyAttribute : Attribute
    {
        public ReportKeyAttribute(string key)
        {
            Key = key;
        }

        public string Key { get; }
    }
}
