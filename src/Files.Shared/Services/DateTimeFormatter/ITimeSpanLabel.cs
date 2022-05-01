namespace Files.Shared.Services.DateTimeFormatter
{
    public interface ITimeSpanLabel
    {
        string Text { get; }
        string Range { get; }
        string Glyph { get; }
        int Index { get; }

        public void Deconstruct(out string text, out string range, out string glyph, out int index);
    }
}
