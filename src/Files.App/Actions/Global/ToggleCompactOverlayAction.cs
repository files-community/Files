// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Microsoft.UI.Windowing;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Graphics;

namespace Files.App.Actions
{
	internal class ToggleCompactOverlayAction : ObservableObject, IToggleAction
	{
		private readonly IWindowContext windowContext = Ioc.Default.GetRequiredService<IWindowContext>();

		public string Label { get; } = "ToggleCompactOverlay".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.F12);

		public string Description => "ToggleCompactOverlayDescription".GetLocalizedResource();

		public bool IsOn => windowContext.IsCompactOverlay;

		public ToggleCompactOverlayAction()
		{
			windowContext.PropertyChanged += WindowContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			var window = App.GetAppWindow(App.Window);

			if (windowContext.IsCompactOverlay)
			{
				window.SetPresenter(AppWindowPresenterKind.Overlapped);
			}
			else
			{
				window.SetPresenter(AppWindowPresenterKind.CompactOverlay);
				window.Resize(new SizeInt32(400, 350));
			}

			return Task.CompletedTask;
		}

		private void WindowContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IWindowContext.IsCompactOverlay):
					OnPropertyChanged(nameof(IsOn));
					break;
			}
		}
	}
}
