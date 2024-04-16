namespace Files.Shared.Services.DateTimeFormatter
{
    public interface ITimeSpanLabel
    {
        string Text { get; }
        string Glyph { get; }
        int Index { get; }
    }
}
