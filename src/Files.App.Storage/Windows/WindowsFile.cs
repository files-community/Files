// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	[DebuggerDisplay("{" + nameof(ToString) + "()}")]
	public unsafe class WindowsFile : WindowsStorable, IWindowsFile
	{
		public WindowsFile(IShellItem* ptr)
		{
			ThisPtr = ptr;
		}

		public Task<Stream> OpenStreamAsync(FileAccess accessMode, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// Get the file path from the shell item
			var filePath = Id;
			
			// Convert FileAccess to FileMode and FileAccess
			var fileMode = accessMode switch
			{
				FileAccess.Read => FileMode.Open,
				FileAccess.Write => FileMode.OpenOrCreate,
				FileAccess.ReadWrite => FileMode.OpenOrCreate,
				_ => throw new ArgumentException($"Invalid {nameof(accessMode)} flag.", nameof(accessMode))
			};

			// Open the file stream
			var stream = new FileStream(filePath, fileMode, accessMode, FileShare.Read);
			return Task.FromResult<Stream>(stream);
		}
	}
}
