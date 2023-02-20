using Files.App.Commands;
using Files.App.Extensions;
using Microsoft.UI.Windowing;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class ToggleFullScreenAction : IToggleAction
	{
		public string Label { get; } = "FullScreen".GetLocalizedResource();

		public HotKey HotKey { get; } = new(VirtualKey.F11);

		public bool IsOn
		{
			get
			{
				var window = App.GetAppWindow(App.Window);
				return window.Presenter.Kind is AppWindowPresenterKind.FullScreen;
			}
		}

		public Task ExecuteAsync()
		{
			var window = App.GetAppWindow(App.Window);

			var newKind = window.Presenter.Kind is AppWindowPresenterKind.FullScreen
				? AppWindowPresenterKind.Overlapped
				: AppWindowPresenterKind.FullScreen;

			window.SetPresenter(newKind);

			return Task.CompletedTask;
		}
	}
}
