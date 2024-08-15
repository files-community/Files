// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services
{
	/// <inheritdoc cref="IWindowsShellService"/>
	internal sealed class WindowsShellService : IWindowsShellService
	{
		private List<ShellNewEntry> _cached;

		public async Task InitializeAsync()
		{
			_cached = await ShellNewEntryExtensions.GetNewContextMenuEntries();
		}

		public List<ShellNewEntry> GetEntries()
		{
			return _cached;
		}
	}
}
