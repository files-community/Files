using System;

namespace Files.Shared.Enums
{
    /// <summary>
    /// Contains all kinds of return statuses
    /// </summary>
    [Flags]
    public enum OperationErrorCode : uint
    {
        Success = 0,
        UnknownFailed,
        Canceled,
        AccessUnauthorized,
        NotFound,
        AlreadyExists,
        NotAFolder,
        NotAFile,
        InProgress,
        InvalidArgument,
        InvalidOperation,
        InUse,
        NameTooLong,
        ReadOnly
    }
}
