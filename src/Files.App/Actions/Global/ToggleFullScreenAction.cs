// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Microsoft.UI.Windowing;

namespace Files.App.Actions
{
	internal class ToggleFullScreenAction : IToggleAction
	{
		public string Label { get; } = "FullScreen".GetLocalizedResource();

		public string Description => "ToggleFullScreenDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.F11);

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
