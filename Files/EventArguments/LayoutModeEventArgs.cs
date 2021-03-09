using Files.Enums;

namespace Files.EventArguments
{
    public class LayoutModeEventArgs
    {
        public readonly FolderLayoutModes LayoutMode;

        public readonly int GridViewSize;

        internal LayoutModeEventArgs(FolderLayoutModes layoutMode, int gridViewSize)
        {
            LayoutMode = layoutMode;
            GridViewSize = gridViewSize;
        }
    }
}