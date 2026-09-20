// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public interface IWindowsStorable : IStorableChild, IEquatable<IWindowsStorable>, IDisposable
	{
		IShellItem ThisPtr { get; set; }

		IContextMenu? ContextMenu { get; set; }
	}
}
