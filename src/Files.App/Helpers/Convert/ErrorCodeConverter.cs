using Files.Shared.Enums;

namespace Files.App.Helpers
{
	public static class ErrorCodeConverter
	{
		public static ReturnResult ToStatus(this FileSystemStatusCode errorCode)
		{
            return errorCode switch
            {
                FileSystemStatusCode.Success => ReturnResult.Success,
                FileSystemStatusCode.Generic => ReturnResult.Failed,
                FileSystemStatusCode.Unauthorized => ReturnResult.AccessUnauthorized,
                FileSystemStatusCode.NotFound => ReturnResult.IntegrityCheckFailed,
                FileSystemStatusCode.InUse => ReturnResult.AccessUnauthorized,
                FileSystemStatusCode.NameTooLong => ReturnResult.UnknownException,
                FileSystemStatusCode.AlreadyExists => ReturnResult.Failed,
                FileSystemStatusCode.NotAFolder => ReturnResult.BadArgumentException,
                FileSystemStatusCode.NotAFile => ReturnResult.BadArgumentException,
                FileSystemStatusCode.InProgress => ReturnResult.InProgress,
                _ => default,
            };
        }
	}
}
