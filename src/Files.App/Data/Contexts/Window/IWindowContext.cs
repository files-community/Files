// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contexts
{
	public interface IWindowContext : INotifyPropertyChanged
	{
		bool IsCompactOverlay { get; }

		/// <inheritdoc cref="IWindowsSecurityService.IsAppElevated"/>
		bool IsRunningAsAdmin { get; }

		/// <inheritdoc cref="IWindowsSecurityService.CanDragAndDrop"/>
		bool CanDragAndDrop { get; }
	}
}
