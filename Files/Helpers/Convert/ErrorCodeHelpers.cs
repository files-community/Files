using Files.Filesystem;

namespace Files.Helpers
{
    public static class ErrorCodeHelpers
    {
        public static Status ToStatus(this FilesystemErrorCode errorCode)
        {
            switch (errorCode)
            {
                case FilesystemErrorCode.ERROR_SUCCESS:
                    return Status.Success;

                case FilesystemErrorCode.ERROR_GENERIC:
                    return Status.InProgress | Status.Cancelled;

                case FilesystemErrorCode.ERROR_UNAUTHORIZED:
                    return Status.AccessUnauthorized;

                case FilesystemErrorCode.ERROR_NOTFOUND:
                    return Status.IntegrityCheckFailed;

                case FilesystemErrorCode.ERROR_INUSE:
                    return Status.AccessUnauthorized;

                case FilesystemErrorCode.ERROR_NAMETOOLONG:
                    return Status.UnknownException;

                case FilesystemErrorCode.ERROR_ALREADYEXIST:
                    return Status.Failed | Status.UnknownException;

                case FilesystemErrorCode.ERROR_NOTAFOLDER:
                    return Status.BadArgumentException | Status.IntegrityCheckFailed;

                case FilesystemErrorCode.ERROR_NOTAFILE:
                    return Status.BadArgumentException | Status.IntegrityCheckFailed;

                case FilesystemErrorCode.ERROR_INPROGRESS:
                    return Status.InProgress;

                default: return default;
            }
        }

        public static FilesystemErrorCode ToFilesystemErrorCode(this Status status)
        {
            switch (status)
            {
                case Status.InProgress:
                    return FilesystemErrorCode.ERROR_INPROGRESS;

                case Status.Success:
                    return FilesystemErrorCode.ERROR_SUCCESS;

                case Status.Failed:
                    return FilesystemErrorCode.ERROR_UNAUTHORIZED | FilesystemErrorCode.ERROR_NOTFOUND |
                        FilesystemErrorCode.ERROR_NOTAFILE | FilesystemErrorCode.ERROR_NOTAFOLDER;

                case Status.IntegrityCheckFailed:
                    return FilesystemErrorCode.ERROR_NOTAFILE | FilesystemErrorCode.ERROR_NOTAFOLDER | FilesystemErrorCode.ERROR_NOTFOUND;

                case Status.UnknownException:
                    return FilesystemErrorCode.ERROR_ALREADYEXIST | FilesystemErrorCode.ERROR_NAMETOOLONG;

                case Status.NullException:
                    return FilesystemErrorCode.ERROR_NOTFOUND | FilesystemErrorCode.ERROR_NOTAFILE | FilesystemErrorCode.ERROR_NOTAFOLDER;

                case Status.AccessUnauthorized:
                    return FilesystemErrorCode.ERROR_UNAUTHORIZED;

                case Status.Cancelled:
                    return FilesystemErrorCode.ERROR_GENERIC | FilesystemErrorCode.ERROR_INPROGRESS;

                default: return default;
            }
        }
    }
}
