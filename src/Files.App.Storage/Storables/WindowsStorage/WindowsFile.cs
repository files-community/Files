// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage.Storables
{
	public sealed class WindowsFile : WindowsStorable, IChildFile, IDisposable
	{
		public string Id => throw new NotImplementedException();

		public string Name => throw new NotImplementedException();

		public WindowsFile(ComPtr<IShellItem> nativeObject)
		{
			ThisPtr = nativeObject;
		}

		public Task<Stream> OpenStreamAsync(FileAccess accessMode, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			ThisPtr.Dispose();
		}
	}
}
