namespace Files.Backend.Item
{
    public enum DriveTypes : ushort
    {
        Unknown,
        Fixed,
        Removable,
        Network,
        Ram,
        CDRom,
        FloppyDisk,
        NoRootDirectory,
        Virtual,
        Cloud,
    }
}
