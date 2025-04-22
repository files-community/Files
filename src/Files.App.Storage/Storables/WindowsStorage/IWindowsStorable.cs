// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public interface IWindowsStorable
	{
		ComPtr<IShellItem> ThisPtr { get; }
	}
}
