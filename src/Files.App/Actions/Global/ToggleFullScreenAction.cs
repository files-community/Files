// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Windowing;

namespace Files.App.Actions
{
	internal class ToggleFullScreenAction : IToggleAction
	{
		public string Label
			=> "FullScreen".GetLocalizedResource();

		public string Description
			=> "ToggleFullScreenDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.F11);

		public bool IsOn
		{
			get
			{
				var appWindow = MainWindow.Instance.AppWindow;
				return appWindow.Presenter.Kind is AppWindowPresenterKind.FullScreen;
			}
		}

		public Task ExecuteAsync()
		{
			var appWindow = MainWindow.Instance.AppWindow;
			var newKind = appWindow.Presenter.Kind is AppWindowPresenterKind.FullScreen
				? AppWindowPresenterKind.Overlapped
				: AppWindowPresenterKind.FullScreen;

			appWindow.SetPresenter(newKind);
			return Task.CompletedTask;
		}
	}
}
