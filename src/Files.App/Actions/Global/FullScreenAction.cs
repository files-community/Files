using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Commands;
using Files.App.DataModels;
using Microsoft.UI.Windowing;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
    internal class FullScreenAction : ObservableObject, IAction
	{
		public CommandCodes Code => CommandCodes.FullScreen;
		public string Label => "Full Screen";

		public HotKey HotKey => new(VirtualKey.F11);

		public bool IsOn
		{
			get
			{
				var window = App.GetAppWindow(App.Window);
				return window.Presenter.Kind is AppWindowPresenterKind.FullScreen;
			}
			set
			{
				var window = App.GetAppWindow(App.Window);
				var kind = value ? AppWindowPresenterKind.FullScreen : AppWindowPresenterKind.Overlapped;

				if (kind == window.Presenter.Kind)
					return;

				window.SetPresenter(kind);
				OnPropertyChanged();
			}
		}

		public Task ExecuteAsync()
		{
			IsOn = !IsOn;
			return Task.CompletedTask;
		}
	}
}
