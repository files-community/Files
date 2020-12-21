using Files.Enums;
using Files.Filesystem;

namespace Files.Helpers
{
    public static class ErrorCodeConverter
    {
        public static ReturnResult ToStatus(this FilesystemErrorCode errorCode)
        {
            switch (errorCode)
            {
                case FilesystemErrorCode.ERROR_SUCCESS:
                    return ReturnResult.Success;

                case FilesystemErrorCode.ERROR_GENERIC:
                    return ReturnResult.InProgress | ReturnResult.Cancelled;

                case FilesystemErrorCode.ERROR_UNAUTHORIZED:
                    return ReturnResult.AccessUnauthorized;

                case FilesystemErrorCode.ERROR_NOTFOUND:
                    return ReturnResult.IntegrityCheckFailed;

                case FilesystemErrorCode.ERROR_INUSE:
                    return ReturnResult.AccessUnauthorized;

                case FilesystemErrorCode.ERROR_NAMETOOLONG:
                    return ReturnResult.UnknownException;

                case FilesystemErrorCode.ERROR_ALREADYEXIST:
                    return ReturnResult.Failed | ReturnResult.UnknownException;

                case FilesystemErrorCode.ERROR_NOTAFOLDER:
                    return ReturnResult.BadArgumentException | ReturnResult.IntegrityCheckFailed;

                case FilesystemErrorCode.ERROR_NOTAFILE:
                    return ReturnResult.BadArgumentException | ReturnResult.IntegrityCheckFailed;

                case FilesystemErrorCode.ERROR_INPROGRESS:
                    return ReturnResult.InProgress;

                default:
                    return default;
            }
        }
    }
}