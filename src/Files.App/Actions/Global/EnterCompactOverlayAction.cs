// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace Files.App.Actions
{
	internal class EnterCompactOverlayAction : ObservableObject, IAction
	{
		private readonly IWindowContext windowContext;

		public string Label
			=> "EnterCompactOverlay".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconEnterCompactOverlay");

		public HotKey HotKey
			=> new(Keys.Up, KeyModifiers.MenuCtrl);

		public string Description
			=> "EnterCompactOverlayDescription".GetLocalizedResource();

		public bool IsExecutable
			=> !windowContext.IsCompactOverlay;

		public EnterCompactOverlayAction()
		{
			windowContext = Ioc.Default.GetRequiredService<IWindowContext>();

			windowContext.PropertyChanged += WindowContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			var appWindow = MainWindow.Instance.AppWindow;
			appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
			appWindow.Resize(new SizeInt32(400, 350));

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
