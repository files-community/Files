// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Windowing;

namespace Files.App.Data.Contexts
{
	/// <inheritdoc cref="IWindowContext"/>
	internal sealed class WindowContext : ObservableObject, IWindowContext
	{
		private IWindowsSecurityService WindowsSecurityService = Ioc.Default.GetRequiredService<IWindowsSecurityService>();

		private bool isCompactOverlay;
		/// <inheritdoc/>
		public bool IsCompactOverlay => isCompactOverlay;

		/// <inheritdoc/>
		public bool IsRunningAsAdmin { get; private set; }

		/// <inheritdoc/>
		public bool CanDragAndDrop { get; private set; }

		public WindowContext()
		{
			IsRunningAsAdmin = WindowsSecurityService.IsAppElevated();
			CanDragAndDrop = WindowsSecurityService.CanDragAndDrop();

			MainWindow.Instance.PresenterChanged += Window_PresenterChanged;
		}

		private void Window_PresenterChanged(object? sender, AppWindowPresenter e)
		{
			SetProperty(
				ref isCompactOverlay,
				e.Kind is AppWindowPresenterKind.CompactOverlay,
				nameof(IsCompactOverlay));
		}
	}
}
