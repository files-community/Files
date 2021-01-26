using Files.Enums;

namespace Files.Helpers
{
    public static class ErrorCodeConverter
    {
        public static ReturnResult ToStatus(this FileSystemStatusCode errorCode)
        {
            switch (errorCode)
            {
                case FileSystemStatusCode.Success:
                    return ReturnResult.Success;

                case FileSystemStatusCode.Generic:
                    return ReturnResult.InProgress | ReturnResult.Cancelled;

                case FileSystemStatusCode.Unauthorized:
                    return ReturnResult.AccessUnauthorized;

                case FileSystemStatusCode.NotFound:
                    return ReturnResult.IntegrityCheckFailed;

                case FileSystemStatusCode.InUse:
                    return ReturnResult.AccessUnauthorized;

                case FileSystemStatusCode.NameTooLong:
                    return ReturnResult.UnknownException;

                case FileSystemStatusCode.AlreadyExists:
                    return ReturnResult.Failed | ReturnResult.UnknownException;

                case FileSystemStatusCode.NotAFolder:
                    return ReturnResult.BadArgumentException | ReturnResult.IntegrityCheckFailed;

                case FileSystemStatusCode.NotAFile:
                    return ReturnResult.BadArgumentException | ReturnResult.IntegrityCheckFailed;

                case FileSystemStatusCode.InProgress:
                    return ReturnResult.InProgress;

                default:
                    return default;
            }
        }
    }
}