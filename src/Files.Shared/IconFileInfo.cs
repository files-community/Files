namespace Files.Common
{
    public class IconFileInfo
    {
        public byte[] IconDataBytes { get; set; }
        public string IconData { get; }
        public int Index { get; }

        public IconFileInfo(string iconData, int index)
        {
            IconData = iconData;
            Index = index;
        }
    }
}
