using System;

namespace Files.Uwp.Filesystem.Native
{
    [Flags]
    public enum CryptProtectFlags
    {
        CRYPTPROTECT_UI_FORBIDDEN = 0x1,
        CRYPTPROTECT_LOCAL_MACHINE = 0x4,
        CRYPTPROTECT_CRED_SYNC = 0x8,
        CRYPTPROTECT_AUDIT = 0x10,
        CRYPTPROTECT_NO_RECOVERY = 0x20,
        CRYPTPROTECT_VERIFY_PROTECTION = 0x40,
        CRYPTPROTECT_CRED_REGENERATE = 0x80,
    }
}
