using Microsoft.Toolkit.Uwp;
using System;

namespace Files.Uwp.ServicesImplementation.DateTimeFormatter
{
    internal class UniversalDateTimeFormatter : AbstractDateTimeFormatter
    {
        public override string Name => "Universal".GetLocalized();

        public override string ToShortLabel(DateTimeOffset offset)
        {
            if (offset.Year is <= 1601 or >= 9999)
            {
                return " ";
            }
            return offset.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
