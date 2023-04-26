// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Enums;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Filesystem
{
	public static class FilesystemTasks
	{
		public static FileSystemStatusCode GetErrorCode(Exception ex, Type T = null) => (ex, (uint)ex.HResult) switch
		{
			(UnauthorizedAccessException, _) => FileSystemStatusCode.Unauthorized,
			(FileNotFoundException, _) => FileSystemStatusCode.NotFound, // Item was deleted
			(COMException, _) => FileSystemStatusCode.NotFound, // Item's drive was ejected
			(_, 0x8007000F) => FileSystemStatusCode.NotFound, // The system cannot find the drive specified
			(PathTooLongException, _) => FileSystemStatusCode.NameTooLong,
			(IOException, _) => FileSystemStatusCode.InUse,
			(ArgumentException, _) => ToStatusCode(T), // Item was invalid
			(_, 0x800700B7) => FileSystemStatusCode.AlreadyExists,
			(_, 0x80071779) => FileSystemStatusCode.ReadOnly,
			_ => FileSystemStatusCode.Generic,
		};

		public async static Task<FilesystemResult> Wrap(Func<Task> wrapped)
		{
			try
			{
				await wrapped();
				return new FilesystemResult(FileSystemStatusCode.Success);
			}
			catch (Exception ex)
			{
				return new FilesystemResult(GetErrorCode(ex));
			}
		}
		public async static Task<FilesystemResult<T>> Wrap<T>(Func<Task<T>> wrapped)
		{
			try
			{
				return new FilesystemResult<T>(await wrapped(), FileSystemStatusCode.Success);
			}
			catch (Exception ex)
			{
				return new FilesystemResult<T>(default, GetErrorCode(ex, typeof(T)));
			}
		}

		public async static Task<FilesystemResult> OnSuccess<T>(this Task<FilesystemResult<T>> wrapped, Action<T> func)
		{
			var res = await wrapped;
			if (res)
			{
				func(res.Result);
			}
			return res;
		}
		public async static Task<FilesystemResult> OnSuccess<T>(this Task<FilesystemResult<T>> wrapped, Func<T, Task> func)
		{
			var res = await wrapped;
			if (res)
			{
				return await Wrap(() => func(res.Result));
			}
			return res;
		}
		public async static Task<FilesystemResult<V>> OnSuccess<V, T>(this Task<FilesystemResult<T>> wrapped, Func<T, Task<V>> func)
		{
			var res = await wrapped;
			if (res)
			{
				return await Wrap(() => func(res.Result));
			}
			return new FilesystemResult<V>(default, res.ErrorCode);
		}

		private static FileSystemStatusCode ToStatusCode(Type T)
			=> T == typeof(StorageFolderWithPath) || typeof(IStorageFolder).IsAssignableFrom(T)
				? FileSystemStatusCode.NotAFolder
				: FileSystemStatusCode.NotAFile;
	}
}
