// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Files.App.ViewModels.UserControls;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class ToggleSidebarAction : ObservableObject, IToggleAction
	{
		private IAppearanceSettingsService AppearanceSettingsService { get; } = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();

		public string Label
			=> Strings.ToggleSidebar.GetLocalizedResource();

		public string Description
			=> Strings.ToggleSidebarDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Show;

		public HotKey HotKey
			=> new(Keys.B, KeyModifiers.Ctrl);

		public bool IsOn
			=> AppearanceSettingsService.IsSidebarOpen;

		public ToggleSidebarAction()
		{
			AppearanceSettingsService.PropertyChanged += AppearanceSettingsService_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var sidebarViewModel = Ioc.Default.GetRequiredService<SidebarViewModel>();
			sidebarViewModel.SidebarDisplayMode = IsOn
				? SidebarDisplayMode.Compact
				: SidebarDisplayMode.Expanded;

			return Task.CompletedTask;
		}

		private void AppearanceSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AppearanceSettingsService.IsSidebarOpen))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
