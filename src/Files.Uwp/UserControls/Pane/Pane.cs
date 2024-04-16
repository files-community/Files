namespace Files.Uwp.UserControls
{
    public interface IPane
    {
        PanePositions Position { get; }

        void UpdatePosition(double panelWidth, double panelHeight);
    }

    public enum PanePositions : ushort
    {
        None,
        Right,
        Bottom,
    }
}
