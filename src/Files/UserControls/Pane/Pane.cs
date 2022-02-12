namespace Files.UserControls
{
    public interface IPane
    {
        PanePosition Position { get; }

        void UpdatePosition(double panelWidth, double panelHeight);
    }

    public enum PanePosition : ushort
    {
        None,
        Right,
        Bottom,
    }
}
