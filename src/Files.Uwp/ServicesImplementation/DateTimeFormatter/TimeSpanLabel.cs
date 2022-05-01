using Files.Shared.Services.DateTimeFormatter;
using System;

namespace Files.Uwp.ServicesImplementation.DateTimeFormatter
{
    internal class TimeSpanLabel : ITimeSpanLabel
    {
        public string Text { get; init; } = string.Empty;
        public string Range { get; init; } = string.Empty;
        public string Glyph { get; init; } = string.Empty;
        public int Index { get; init; }

        public TimeSpanLabel() {}
        public TimeSpanLabel(string text, string range, string glyph, int index)
            => (Text, Range, Glyph, Index) = (text, range, glyph, index);
        public TimeSpanLabel(string text, DateTime range, string glyph, int index)
            => (Text, Range, Glyph, Index) = (text, range.ToShortDateString(), glyph, index);

        public void Deconstruct(out string text, out string range, out string glyph, out int index)
        {
            text = Text;
            range = Range;
            glyph = Glyph;
            index = Index;
        }
    }
}
