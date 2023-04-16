using Files.Shared.Enums;

namespace Files.App.Helpers
{
	public static class ErrorCodeConverter
	{
		public static ReturnResult ToStatus(this FileSystemStatusCode errorCode)
		{
			switch (errorCode)
			{
				case FileSystemStatusCode.Success:
					return ReturnResult.Success;

				case FileSystemStatusCode.Unauthorized:
				case FileSystemStatusCode.InUse:
					return ReturnResult.AccessUnauthorized;

				case FileSystemStatusCode.NotFound:
					return ReturnResult.IntegrityCheckFailed;

				case FileSystemStatusCode.NotAFolder:
				case FileSystemStatusCode.NotAFile:
					return ReturnResult.BadArgumentException;

				case FileSystemStatusCode.InProgress:
					return ReturnResult.InProgress;

				default:
					return ReturnResult.Failed;
			}
		}
	}
}