// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Sidebar;

namespace Files.App.Actions
{
	internal sealed class ToggleSidebarAction : ObservableObject, IToggleAction
	{
		private readonly SidebarViewModel SidebarViewModel = Ioc.Default.GetRequiredService<SidebarViewModel>();

		public string Label
			=> "ToggleSidebar".GetLocalizedResource();

		public string Description
			=> "ToggleSidebarDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.S, KeyModifiers.CtrlAlt);

		public bool IsOn =>
			SidebarViewModel.SidebarDisplayMode is SidebarDisplayMode.Expanded
				? true
				: false;

		public ToggleSidebarAction()
		{
			SidebarViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			SidebarViewModel.SidebarDisplayMode = !IsOn
				? SidebarDisplayMode.Expanded
				: SidebarDisplayMode.Compact;
			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(SidebarViewModel.SidebarDisplayMode))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
