// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace Files.App.Actions
{
	internal class EnterCompactOverlayAction : ObservableObject, IAction
	{
		private readonly IWindowContext windowContext = Ioc.Default.GetRequiredService<IWindowContext>();

		public string Label { get; } = "EnterCompactOverlay".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new(opacityStyle: "EnterCompactOverlay");

		public HotKey HotKey { get; } = new(Keys.Up, KeyModifiers.MenuCtrl);

		public string Description => "EnterCompactOverlayDescription".GetLocalizedResource();

		public bool IsExecutable => !windowContext.IsCompactOverlay;

		public EnterCompactOverlayAction()
		{
			windowContext.PropertyChanged += WindowContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			var window = App.GetAppWindow(App.Window);
			window.SetPresenter(AppWindowPresenterKind.CompactOverlay);
			window.Resize(new SizeInt32(400, 350));

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
