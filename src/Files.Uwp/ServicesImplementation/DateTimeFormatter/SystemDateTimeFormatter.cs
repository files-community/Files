using Files.Shared.Services.DateTimeFormatter;
using Microsoft.Toolkit.Uwp;
using System;

namespace Files.Uwp.ServicesImplementation.DateTimeFormatter
{
    internal class SystemDateTimeFormatter : AbstractDateTimeFormatter
    {
        public override string Name => "SystemTimeStyle".GetLocalized();

        public override string ToShortLabel(DateTimeOffset offset)
        {
            if (offset.Year is <= 1601 or >= 9999)
            {
                return " ";
            }
            return offset.ToLocalTime().ToString("g");
        }

        public override ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset)
        {
            var label = base.ToTimeSpanLabel(offset);
            return new TimeSpanLabel(label.Range, label.Range, label.Glyph, label.Index);
        }

        protected override string ToRangeLabel(DateTime range) => range.ToShortDateString();
    }
}
