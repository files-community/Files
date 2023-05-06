// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;

namespace Files.App.Actions
{
	internal class ToggleSidebarAction : ObservableObject, IToggleAction
	{
		private readonly SidebarViewModel viewModel = Ioc.Default.GetRequiredService<SidebarViewModel>();

		public string Label { get; } = "ToggleSidebar".GetLocalizedResource();

		public string Description { get; } = "ToggleSidebarDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.B, KeyModifiers.Ctrl);

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
