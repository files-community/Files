using Microsoft.UI.Xaml.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Microsoft.UI.Windowing;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.System;

namespace Files.App.Actions
{
	internal class EnterCompactOverlayAction : XamlUICommand
	{
		private readonly IWindowContext windowContext = Ioc.Default.GetRequiredService<IWindowContext>();

		public string Label { get; } = "EnterCompactOverlay".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new(opacityStyle: "EnterCompactOverlay");

		public HotKey HotKey { get; } = new(VirtualKey.Up, VirtualKeyModifiers.Menu | VirtualKeyModifiers.Control);

		public string Description => "EnterCompactOverlayDescription".GetLocalizedResource();

		public bool CanExecute => !windowContext.IsCompactOverlay;

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
					NotifyCanExecuteChanged();
					break;
			}
		}
	}
}
