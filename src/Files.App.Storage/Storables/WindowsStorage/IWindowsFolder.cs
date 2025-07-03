// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public unsafe interface IWindowsFolder : IWindowsStorable, IChildFolder
	{
		/// <summary>
		/// Gets or sets the cached <see cref="IContextMenu"/> for the ShellNew context menu.
		/// </summary>
		public IContextMenu* ShellNewMenu { get; set; }
	}
}
