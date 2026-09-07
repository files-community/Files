// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Windowing;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class ToggleFullScreenAction : ObservableObject, IToggleAction
	{
		private readonly IWindowContext windowContext;

		public string Label
			=> Strings.FullScreen.GetLocalizedResource();

		public string Description
			=> Strings.ToggleFullScreenDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Window;

		public HotKey HotKey
			=> new(Keys.F11);

		public bool IsOn
			=> windowContext.IsFullScreen;

		public ToggleFullScreenAction()
		{
			windowContext = Ioc.Default.GetRequiredService<IWindowContext>();

			windowContext.PropertyChanged += WindowContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var appWindow = MainWindow.Instance.AppWindow;
			var newKind = appWindow.Presenter.Kind is AppWindowPresenterKind.FullScreen
				? AppWindowPresenterKind.Overlapped
				: AppWindowPresenterKind.FullScreen;

			appWindow.SetPresenter(newKind);
			return Task.CompletedTask;
		}

		private void WindowContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IWindowContext.IsFullScreen))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
