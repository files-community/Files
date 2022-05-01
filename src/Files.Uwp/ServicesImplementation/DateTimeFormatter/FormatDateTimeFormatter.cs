using Files.Shared.Services.DateTimeFormatter;
using System;

namespace Files.Uwp.ServicesImplementation.DateTimeFormatter
{
    internal class FormatDateTimeFormatter : AbstractDateTimeFormatter
    {
        private readonly string format;

        public override string Name { get; }

        public FormatDateTimeFormatter(string name, string format) => (Name, this.format) = (name, format);

        public override string ToShortLabel(DateTimeOffset offset)
        {
            if (offset.Year <= 1601 || offset.Year >= 9999)
            {
                return " ";
            }
            return offset.ToLocalTime().ToString(format);
        }

        public override ITimeSpanLabel ToTimeSpanLabel(DateTimeOffset offset)
        {
            var label = base.ToTimeSpanLabel(offset);
            return new TimeSpanLabel(label.Range, label.Range, label.Glyph, label.Index);
        }
    }
}
