namespace Files.Uwp.Filesystem.Native
{
    public enum FILE_INFO_BY_HANDLE_CLASS
    {
        FileBasicInfo = 0,
        FileStandardInfo = 1,
        FileNameInfo = 2,
        FileRenameInfo = 3,
        FileDispositionInfo = 4,
        FileAllocationInfo = 5,
        FileEndOfFileInfo = 6,
        FileStreamInfo = 7,
        FileCompressionInfo = 8,
        FileAttributeTagInfo = 9,
        FileIdBothDirectoryInfo = 10,// 0x0A
        FileIdBothDirectoryRestartInfo = 11, // 0xB
        FileIoPriorityHintInfo = 12, // 0xC
        FileRemoteProtocolInfo = 13, // 0xD
        FileFullDirectoryInfo = 14, // 0xE
        FileFullDirectoryRestartInfo = 15, // 0xF
        FileStorageInfo = 16, // 0x10
        FileAlignmentInfo = 17, // 0x11
        FileIdInfo = 18, // 0x12
        FileIdExtdDirectoryInfo = 19, // 0x13
        FileIdExtdDirectoryRestartInfo = 20, // 0x14
        MaximumFileInfoByHandlesClass,
    }
}
