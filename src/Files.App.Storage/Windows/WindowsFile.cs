﻿// Copyright (c) Files Community
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
			throw new NotImplementedException();
		}
	}
}
