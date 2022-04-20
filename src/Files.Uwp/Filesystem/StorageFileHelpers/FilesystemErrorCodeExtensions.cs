using Files.Shared.Enums;

namespace Files.Uwp.Filesystem
{
    public static class FilesystemErrorCodeExtensions
    {
        public static bool HasFlag(this FileSystemStatusCode errorCode, FileSystemStatusCode flag)
            => (errorCode & flag) is not FileSystemStatusCode.Success;
    }
}
