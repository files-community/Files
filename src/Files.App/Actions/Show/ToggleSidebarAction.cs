using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
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
		private readonly SidebarViewModel viewModel = Ioc.Default.GetRequiredService<SidebarViewModel>();

		public string Label { get; } = "ToggleSidebar".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(VirtualKey.B, VirtualKeyModifiers.Control);

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
