// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class ToggleSidebarAction : ObservableObject, IToggleAction
	{
		private readonly IAppearanceSettingsService AppearanceSettingsService = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();
		private readonly ISidebarViewModel SidebarViewModel = Ioc.Default.GetRequiredService<ISidebarViewModel>();

		public string Label
			=> "ToggleSidebar".GetLocalizedResource();

		public string Description
			=> "ToggleSidebarDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.S, KeyModifiers.CtrlAlt);

		public bool IsOn
			=> AppearanceSettingsService.IsSidebarOpen;

		public ToggleSidebarAction()
		{
			AppearanceSettingsService.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			AppearanceSettingsService.IsSidebarOpen = !IsOn;
			SidebarViewModel.UpdateTabControlMargin();
			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AppearanceSettingsService.IsSidebarOpen))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
