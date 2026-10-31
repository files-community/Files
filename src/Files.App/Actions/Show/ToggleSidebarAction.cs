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

		// SidebarViewModel is registered in DI later than ToggleSidebarAction is constructed; resolve it lazily on first access so startup doesn't throw.
		private SidebarViewModel? sidebarViewModel;
		private SidebarViewModel SidebarViewModel
		{
			get
			{
				if (sidebarViewModel is null)
				{
					sidebarViewModel = Ioc.Default.GetRequiredService<SidebarViewModel>();
					sidebarViewModel.PropertyChanged += SidebarViewModel_PropertyChanged;
				}
				return sidebarViewModel;
			}
		}

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

		public bool IsExecutable
			=> SidebarViewModel.ActualDisplayMode != SidebarDisplayMode.Minimal;

		public ToggleSidebarAction()
		{
			AppearanceSettingsService.PropertyChanged += AppearanceSettingsService_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			SidebarViewModel.SidebarDisplayMode = IsOn
				? SidebarDisplayMode.Compact
				: SidebarDisplayMode.Expanded;

			return Task.CompletedTask;
		}

		private void AppearanceSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(AppearanceSettingsService.IsSidebarOpen))
				OnPropertyChanged(nameof(IsOn));
		}

		private void SidebarViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(SidebarViewModel.ActualDisplayMode))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
