using System;

namespace Files.App.Filesystem.StorageItems
{
	public interface IPasswordProtectedItem
	{
		StorageCredential Credentials { get; set; }

		event EventHandler<TaskCompletionSource<StorageCredential>> PasswordRequested;
	}
}
