using System;
using System.IO;
using Windows.Storage;

namespace Files.App.Storage.Extensions
{
	internal static class FileAccessExtensions
	{
		public static FileAccessMode ToFileAccessMode(this FileAccess access) => access switch
		{
			FileAccess.Read => FileAccessMode.Read,
			FileAccess.Write => FileAccessMode.ReadWrite,
			FileAccess.ReadWrite => FileAccessMode.ReadWrite,
			_ => throw new ArgumentOutOfRangeException(nameof(access))
		};

		public static StorageOpenOptions ToStorageOpenOptions(this FileShare share) => share switch
		{
			FileShare.Read => StorageOpenOptions.AllowOnlyReaders,
			FileShare.Write => StorageOpenOptions.AllowReadersAndWriters,
			FileShare.ReadWrite => StorageOpenOptions.AllowReadersAndWriters,
			FileShare.Inheritable => StorageOpenOptions.None,
			FileShare.Delete => StorageOpenOptions.None,
			FileShare.None => StorageOpenOptions.None,
			_ => throw new ArgumentOutOfRangeException(nameof(share))
		};
	}
}
