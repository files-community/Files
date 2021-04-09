using Files.Enums;

namespace Files.EventArguments
{
    public class LayoutModeEventArgs
    {
        public readonly int GridViewSize;
        public readonly FolderLayoutModes LayoutMode;

        internal LayoutModeEventArgs(FolderLayoutModes layoutMode, int gridViewSize)
        {
            LayoutMode = layoutMode;
            GridViewSize = gridViewSize;
        }
    }
}