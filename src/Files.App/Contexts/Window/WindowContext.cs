using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Windowing;

namespace Files.App.Contexts
{
	internal class WindowContext : ObservableObject, IWindowContext
	{
		private bool isCompactOverlay;
		public bool IsCompactOverlay => isCompactOverlay;

		public WindowContext()
		{
			App.Window.PresenterChanged += Window_PresenterChanged;
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
