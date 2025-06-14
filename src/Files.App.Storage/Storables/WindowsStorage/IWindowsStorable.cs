// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public unsafe interface IWindowsStorable : IStorableChild, IEquatable<IWindowsStorable>, IDisposable
	{
		IShellItem* ThisPtr { get; }
	}
}
