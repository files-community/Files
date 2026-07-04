// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

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
			throw new NotImplementedException();
		}
	}
}
