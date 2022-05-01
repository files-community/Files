using Files.Shared.Services.DateTimeFormatter;
using Microsoft.Toolkit.Uwp;
using System;

namespace Files.Uwp.ServicesImplementation.DateTimeFormatter
{
    internal class UniversalDateTimeFormatter : AbstractDateTimeFormatter
    {
        public override string Name => "Universal".GetLocalized();

        public override string ToShortLabel(DateTimeOffset offset)
        {
            if (offset.Year <= 1601 || offset.Year >= 9999)
            {
                return " ";
            }
            return offset.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }

        public override ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset)
        {
            var label = base.ToTimeSpanLabel(offset);
            return new TimeSpanLabel(label.Range, label.Range, label.Glyph, label.Index);
        }

        protected override string ToRangeLabel(DateTime range) => range.ToString("yyyy-MM-dd");
    }
}
