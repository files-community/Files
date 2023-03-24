using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Commands;
using Files.App.Extensions;
using Files.App.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class ToggleSidebarAction : ObservableObject, IToggleAction
	{
		private readonly SidebarViewModel viewModel = null;

		public string Label { get; } = "TogglePreviewPane".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(VirtualKey.S, VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu);

		public bool IsOn => viewModel.IsSidebarOpen;

		public ToggleSidebarAction()
		{
			viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			viewModel.IsSidebarOpen = !IsOn;
			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(SidebarViewModel.IsSidebarOpen))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
