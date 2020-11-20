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

                default: return default;
            }
        }

        public static FilesystemErrorCode ToFilesystemErrorCode(this ReturnResult status)
        {
            switch (status)
            {
                case ReturnResult.InProgress:
                    return FilesystemErrorCode.ERROR_INPROGRESS;

                case ReturnResult.Success:
                    return FilesystemErrorCode.ERROR_SUCCESS;

                case ReturnResult.Failed:
                    return FilesystemErrorCode.ERROR_UNAUTHORIZED | FilesystemErrorCode.ERROR_NOTFOUND |
                        FilesystemErrorCode.ERROR_NOTAFILE | FilesystemErrorCode.ERROR_NOTAFOLDER;

                case ReturnResult.IntegrityCheckFailed:
                    return FilesystemErrorCode.ERROR_NOTAFILE | FilesystemErrorCode.ERROR_NOTAFOLDER | FilesystemErrorCode.ERROR_NOTFOUND;

                case ReturnResult.UnknownException:
                    return FilesystemErrorCode.ERROR_ALREADYEXIST | FilesystemErrorCode.ERROR_NAMETOOLONG;

                case ReturnResult.NullException:
                    return FilesystemErrorCode.ERROR_NOTFOUND | FilesystemErrorCode.ERROR_NOTAFILE | FilesystemErrorCode.ERROR_NOTAFOLDER;

                case ReturnResult.AccessUnauthorized:
                    return FilesystemErrorCode.ERROR_UNAUTHORIZED;

                case ReturnResult.Cancelled:
                    return FilesystemErrorCode.ERROR_GENERIC | FilesystemErrorCode.ERROR_INPROGRESS;

                default: return default;
            }
        }
    }
}
