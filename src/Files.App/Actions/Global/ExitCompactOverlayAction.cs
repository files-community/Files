// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;
using Microsoft.UI.Windowing;

namespace Files.App.Actions
{
	internal class ExitCompactOverlayAction : ObservableObject, IAction
	{
		private readonly IWindowContext windowContext = Ioc.Default.GetRequiredService<IWindowContext>();

		public string Label { get; } = "ExitCompactOverlay".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new(opacityStyle: "ExitCompactOverlay");

		public HotKey HotKey { get; } = new(Keys.Down, KeyModifiers.MenuCtrl);

		public string Description => "ExitCompactOverlayDescription".GetLocalizedResource();

		public bool IsExecutable => windowContext.IsCompactOverlay;

		public ExitCompactOverlayAction()
		{
			windowContext.PropertyChanged += WindowContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			var window = App.GetAppWindow(App.Window);
			window.SetPresenter(AppWindowPresenterKind.Overlapped);

			return Task.CompletedTask;
		}

		private void WindowContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IWindowContext.IsCompactOverlay):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
