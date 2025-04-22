// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	[DebuggerDisplay("{" + nameof(ToString) + "()}")]
	public sealed class WindowsFile : WindowsStorable, IChildFile, IDisposable
	{
		public WindowsFile(ComPtr<IShellItem> nativeObject)
		{
			ThisPtr = nativeObject;
		}

		public Task<Stream> OpenStreamAsync(FileAccess accessMode, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		// Disposer

		/// <inheritdoc/>
		public void Dispose()
		{
			ThisPtr.Dispose();
		}
	}
}
