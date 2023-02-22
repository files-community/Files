using Files.Core.Enums;

namespace Files.App.Helpers
{
	public static class ErrorCodeConvertHelpers
	{
		public static ReturnResult ToStatus(this FileSystemStatusCode errorCode)
		{
			switch (errorCode)
			{
				case FileSystemStatusCode.Success:
					return ReturnResult.Success;

				case FileSystemStatusCode.Generic:
					return ReturnResult.Failed;

				case FileSystemStatusCode.Unauthorized:
					return ReturnResult.AccessUnauthorized;

				case FileSystemStatusCode.NotFound:
					return ReturnResult.IntegrityCheckFailed;

				case FileSystemStatusCode.InUse:
					return ReturnResult.AccessUnauthorized;

				case FileSystemStatusCode.NameTooLong:
					return ReturnResult.UnknownException;

				case FileSystemStatusCode.AlreadyExists:
					return ReturnResult.Failed;

				case FileSystemStatusCode.NotAFolder:
					return ReturnResult.BadArgumentException;

				case FileSystemStatusCode.NotAFile:
					return ReturnResult.BadArgumentException;

				case FileSystemStatusCode.InProgress:
					return ReturnResult.InProgress;

				default:
					return default;
			}
		}
	}
}