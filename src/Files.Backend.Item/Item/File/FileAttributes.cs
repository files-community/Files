using System;

namespace Files.Backend.Item
{
    [Flags]
    public enum FileAttributes : ushort
    {
        None = 0x0000,
        Archive = 0x0001,
        Compressed = 0x0002,
        Device = 0x0004,
        Directory = 0x0010,
        Encrypted = 0x0020,
        Hidden = 0x0040,
        Offline = 0x0100,
        ReadOnly = 0x0200,
        System = 0x0400,
        Temporary = 0x1000,
    }
}
