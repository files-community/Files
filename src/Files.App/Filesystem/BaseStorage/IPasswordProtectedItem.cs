using FluentFTP;
using SevenZip;
using System;
using Windows.Storage;

namespace Files.App.Filesystem.StorageItems
{
	public interface IPasswordProtectedItem
	{
		StorageCredentialsHolder StorageCredentialsHolder { get; init; }
	}

	public class StorageCredentialsHolder
	{
		public StorageCredentialsHolder(IStorageItem item)
			=> Item = item;

		public StorageCredentialsHolder(IStorageItem item, StorageCredential credential)
			=> (Item, Credentials) = (item, credential);

		public void CopyFrom(StorageCredentialsHolder parent)
		{
			if (parent.PasswordRequested is not null)
			{
				foreach (var handler in parent.PasswordRequested.GetInvocationList().Cast<EventHandler<TaskCompletionSource<StorageCredential>>>())
					this.PasswordRequested += handler;
			}
			this.Credentials = parent.Credentials;
		}

		public StorageCredential Credentials { get; set; }

		public event EventHandler<TaskCompletionSource<StorageCredential>> PasswordRequested;

		public IStorageItem Item { get; }

		public async Task<TOut> RetryWithCredentials<TOut>(Func<Task<TOut>> func, Exception exception)
		{
			var handled = exception is SevenZipOpenFailedException szofex && szofex.Result is OperationResult.WrongPassword ||
				exception is ExtractionFailedException efex && efex.Result is OperationResult.WrongPassword ||
				exception is FtpAuthenticationException;
			if (!handled || PasswordRequested is null)
				throw exception;

			var tcs = new TaskCompletionSource<StorageCredential>();
			PasswordRequested?.Invoke(Item, tcs);
			Credentials = await tcs.Task;
			return await func();
		}

		public async Task RetryWithCredentials(Func<Task> func, Exception exception)
		{
			var handled = exception is SevenZipOpenFailedException szofex && szofex.Result is OperationResult.WrongPassword ||
				exception is ExtractionFailedException efex && efex.Result is OperationResult.WrongPassword ||
				exception is FtpAuthenticationException;
			if (!handled || PasswordRequested is null)
				throw exception;

			var tcs = new TaskCompletionSource<StorageCredential>();
			PasswordRequested?.Invoke(Item, tcs);
			Credentials = await tcs.Task;
			await func();
		}
	}
}
