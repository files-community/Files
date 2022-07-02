namespace Files.Uwp.Filesystem.Native
{
    public static class NativeConstants
    {
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;

        public const uint FILE_APPEND_DATA = 0x0004;
        public const uint FILE_WRITE_ATTRIBUTES = 0x100;
        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint FILE_SHARE_DELETE = 0x00000004;

        public const int FILE_NOTIFY_CHANGE_FILE_NAME = 1;
        public const int FILE_NOTIFY_CHANGE_DIR_NAME = 2;
        public const int FILE_NOTIFY_CHANGE_ATTRIBUTES = 4;
        public const int FILE_NOTIFY_CHANGE_SIZE = 8;
        public const int FILE_NOTIFY_CHANGE_LAST_WRITE = 16;
        public const int FILE_NOTIFY_CHANGE_LAST_ACCESS = 32;
        public const int FILE_NOTIFY_CHANGE_CREATION = 64;
        public const int FILE_NOTIFY_CHANGE_SECURITY = 256;

        public const uint FILE_BEGIN = 0;
        public const uint FILE_END = 2;

        public const uint CREATE_ALWAYS = 2;
        public const uint CREATE_NEW = 1;
        public const uint OPEN_ALWAYS = 4;
        public const uint OPEN_EXISTING = 3;
        public const uint TRUNCATE_EXISTING = 5;

        public const int INVALID_HANDLE_VALUE = -1;

        public const int FSCTL_LOCK_VOLUME = 0x00090018;
        public const int FSCTL_DISMOUNT_VOLUME = 0x00090020;
        public const int FSCTL_GET_REPARSE_POINT = 0x000900A8;

        public const int IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;
        public const int IOCTL_STORAGE_MEDIA_REMOVAL = 0x002D4804;

        public const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
        public const uint IO_REPARSE_TAG_SYMLINK = 0xA000000C;

        public const int FIND_FIRST_EX_CASE_SENSITIVE = 1;
        public const int FIND_FIRST_EX_LARGE_FETCH = 2;

        internal const int MAXIMUM_REPARSE_DATA_BUFFER_SIZE = 16 * 1024;
    }
}
