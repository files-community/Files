// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Windowing;

namespace Files.App.Data.Contexts
{
	/// <inheritdoc cref="IWindowContext"/>
	internal sealed partial class WindowContext : ObservableObject, IWindowContext
	{
		private IWindowsSecurityService WindowsSecurityService = Ioc.Default.GetRequiredService<IWindowsSecurityService>();

		private bool isCompactOverlay;
		/// <inheritdoc/>
		public bool IsCompactOverlay => isCompactOverlay;

		private bool isFullScreen;
		/// <inheritdoc/>
		public bool IsFullScreen => isFullScreen;

		/// <inheritdoc/>
		public bool IsRunningAsAdmin { get; private set; }

		/// <inheritdoc/>
		public bool CanDragAndDrop { get; private set; }

		public WindowContext()
		{
			IsRunningAsAdmin = WindowsSecurityService.IsAppElevated();
			CanDragAndDrop = WindowsSecurityService.CanDragAndDrop();

			// MainWindow.Instance lazily creates the window; defer if constructed during the off-thread prewarm
			if (App.UiDispatcher?.HasThreadAccess ?? true)
				MainWindow.Instance.AppWindow.Changed += AppWindow_Changed;
			else
				App.UiDispatcher.TryEnqueue(() => MainWindow.Instance.AppWindow.Changed += AppWindow_Changed);
		}

		private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
		{
			if (args.DidPresenterChange)
			{
				SetProperty(
					ref isCompactOverlay,
					sender.Presenter.Kind is AppWindowPresenterKind.CompactOverlay,
					nameof(IsCompactOverlay));

				SetProperty(
					ref isFullScreen,
					sender.Presenter.Kind is AppWindowPresenterKind.FullScreen,
					nameof(IsFullScreen));
			}
		}
	}
}
