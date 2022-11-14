using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Files.App.ViewModels.SettingsViewModels
{
	public class AppearanceViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private int selectedThemeIndex = (int)Enum.Parse(typeof(ElementTheme), ThemeHelper.RootTheme.ToString());
		private AppTheme selectedTheme = App.AppSettings.SelectedTheme;

		public AppearanceViewModel()
		{
			Themes = new List<string>()
			{
				"Default".GetLocalizedResource(),
				"LightTheme".GetLocalizedResource(),
				"DarkTheme".GetLocalizedResource()
			};
		}

		public List<string> Themes { get; private set; }
		public ObservableCollection<AppTheme> CustomThemes => App.ExternalResourcesHelper.Themes;

		public int SelectedThemeIndex
		{
			get => selectedThemeIndex;
			set
			{
				if (SetProperty(ref selectedThemeIndex, value))
				{
					ThemeHelper.RootTheme = (ElementTheme)value;
					OnPropertyChanged(nameof(SelectedElementTheme));
				}
			}
		}

		public ElementTheme SelectedElementTheme
		{
			get => (ElementTheme)selectedThemeIndex;
		}

		public bool MoveOverflowMenuItemsToSubMenu
		{
			get => UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu)
				{
					UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu = value;
					OnPropertyChanged();
				}
			}
		}

		public AppTheme SelectedTheme
		{
			get
			{
				return selectedTheme;
			}
			set
			{
				if (SetProperty(ref selectedTheme, value))
				{
					if (selectedTheme is not null)
					{
						// Remove the old resource file and load the new file
						App.ExternalResourcesHelper.UpdateTheme(App.AppSettings.SelectedTheme, selectedTheme)
							.ContinueWith(t =>
							{
								App.AppSettings.SelectedTheme = selectedTheme;
								ForceReloadResourceFile(); // Force the application to use the correct resource file
							}, TaskScheduler.FromCurrentSynchronizationContext());
					}
				}
			}
		}

		/// <summary>
		/// Forces the application to use the correct resource styles
		/// </summary>
		private void ForceReloadResourceFile()
		{
			// Get the index of the current theme
			var selTheme = SelectedThemeIndex;

			// Toggle between the themes to force the controls to use the new resource styles
			SelectedThemeIndex = 0;
			SelectedThemeIndex = 1;
			SelectedThemeIndex = 2;

			// Restore the theme to the correct theme
			SelectedThemeIndex = selTheme;
		}

		public bool ShowFavoritesSection
		{
			get => UserSettingsService.AppearanceSettingsService.ShowFavoritesSection;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowFavoritesSection)
				{
					UserSettingsService.AppearanceSettingsService.ShowFavoritesSection = value;
					OnPropertyChanged();
				}
			}
		}

		public bool UseCompactStyles
		{
			get => UserSettingsService.AppearanceSettingsService.UseCompactStyles;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.UseCompactStyles)
				{
					UserSettingsService.AppearanceSettingsService.UseCompactStyles = value;

					App.ExternalResourcesHelper.OverrideAppResources(UseCompactStyles); // Override the app resources the correct styles
					ForceReloadResourceFile(); // Force the application to use the correct resource file

					OnPropertyChanged();
				}
			}
		}

		public bool ShowLibrarySection
		{
			get => UserSettingsService.AppearanceSettingsService.ShowLibrarySection;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowLibrarySection)
				{
					UserSettingsService.AppearanceSettingsService.ShowLibrarySection = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowDrivesSection
		{
			get => UserSettingsService.AppearanceSettingsService.ShowDrivesSection;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowDrivesSection)
				{
					UserSettingsService.AppearanceSettingsService.ShowDrivesSection = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowCloudDrivesSection
		{
			get => UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection)
				{
					UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowNetworkDrivesSection
		{
			get => UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection)
				{
					UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowWslSection
		{
			get => UserSettingsService.AppearanceSettingsService.ShowWslSection;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowWslSection)
				{
					UserSettingsService.AppearanceSettingsService.ShowWslSection = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowFileTagsSection
		{
			get => UserSettingsService.AppearanceSettingsService.ShowFileTagsSection;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowFileTagsSection)
				{
					UserSettingsService.AppearanceSettingsService.ShowFileTagsSection = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowFoldersWidget
		{
			get => UserSettingsService.AppearanceSettingsService.ShowFoldersWidget;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowFoldersWidget)
					UserSettingsService.AppearanceSettingsService.ShowFoldersWidget = value;
			}
		}

		public bool ShowDrivesWidget
		{
			get => UserSettingsService.AppearanceSettingsService.ShowDrivesWidget;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowDrivesWidget)
					UserSettingsService.AppearanceSettingsService.ShowDrivesWidget = value;
			}
		}

		public bool ShowBundlesWidget
		{
			get => UserSettingsService.AppearanceSettingsService.ShowBundlesWidget;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowBundlesWidget)
					UserSettingsService.AppearanceSettingsService.ShowBundlesWidget = value;
			}
		}

		public bool ShowRecentFilesWidget
		{
			get => UserSettingsService.AppearanceSettingsService.ShowRecentFilesWidget;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowRecentFilesWidget)
					UserSettingsService.AppearanceSettingsService.ShowRecentFilesWidget = value;
			}
		}

		private bool isLoadingThemes;
		public bool IsLoadingThemes
		{
			get => isLoadingThemes;
			set
			{
				isLoadingThemes = value;
				OnPropertyChanged();
			}
		}

		public Task OpenThemesFolder()
		{
			//await CoreApplication.MainView.Dispatcher.YieldAsync(); // WINUI3
			return NavigationHelpers.OpenPathInNewTab(App.ExternalResourcesHelper.ImportedThemesFolder.Path);
		}
	}
}