using System;

namespace Files.Uwp.Filesystem.Native
{
    [Flags]
    public enum TokenAccess : uint
    {
        TOKEN_ASSIGN_PRIMARY = 0x0001,
        TOKEN_DUPLICATE = 0x0002,
        TOKEN_IMPERSONATE = 0x0004,
        TOKEN_QUERY = 0x0008,
        TOKEN_QUERY_SOURCE = 0x0010,
        TOKEN_ADJUST_PRIVILEGES = 0x0020,
        TOKEN_ADJUST_GROUPS = 0x0040,
        TOKEN_ADJUST_DEFAULT = 0x0080,
        TOKEN_ADJUST_SESSIONID = 0x0100,
        TOKEN_ALL_ACCESS_P = 0x000F00FF,
        TOKEN_ALL_ACCESS = 0x000F01FF,
        TOKEN_READ = 0x00020008,
        TOKEN_WRITE = 0x000200E0,
        TOKEN_EXECUTE = 0x00020000,
    }
}
