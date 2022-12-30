using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.Views.SettingsPages.Appearance;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Windows.UI;

namespace Files.App.ViewModels.SettingsViewModels
{
	public class AppearanceViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public List<string> Themes { get; private set; }

		public ObservableCollection<AppThemeResource> AppThemeResources { get; }

		public AppearanceViewModel()
		{
			Themes = new List<string>()
			{
				"Default".GetLocalizedResource(),
				"LightTheme".GetLocalizedResource(),
				"DarkTheme".GetLocalizedResource()
			};

			AppThemeResources = AppThemeResourceFactory.AppThemeResources;
			UpdateSelectedBackground();
		}

		/// <summary>
		/// Selects the AppThemeResource corresponding to the AppThemeBackgroundColor setting
		/// </summary>
		private void UpdateSelectedBackground()
		{
			var backgroundColor = AppThemeBackgroundColor;

			// Add color to the collection if it's not already there
			if (!AppThemeResources.Any(p => p.BackgroundColor == backgroundColor))
			{
				var appThemeBackgroundColor = new AppThemeResource
				{
					BackgroundColor = backgroundColor,
					PreviewColor = new SolidColorBrush(Color.FromArgb(255, backgroundColor.R, backgroundColor.G, backgroundColor.B)),
					Name = "Custom"
				};

				AppThemeResources.Insert(1, appThemeBackgroundColor);
			}

			SelectedAppBackgroundColor = AppThemeResources
					.Where(p => p.BackgroundColor == AppThemeBackgroundColor)
					.FirstOrDefault() ?? AppThemeResources[0];
		}


		private AppThemeResource selectedAppBackgroundColor;
		public AppThemeResource SelectedAppBackgroundColor
		{
			get => selectedAppBackgroundColor;
			set
			{
				if (SetProperty(ref selectedAppBackgroundColor, value))
				{
					AppThemeBackgroundColor = SelectedAppBackgroundColor.BackgroundColor;
					OnPropertyChanged(nameof(selectedAppBackgroundColor));
				}
			}
		}

		private int selectedThemeIndex = (int)Enum.Parse(typeof(ElementTheme), ThemeHelper.RootTheme.ToString());
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

					App.AppThemeResourcesHelper.SetCompactSpacing(UseCompactStyles);
					App.AppThemeResourcesHelper.ApplyResources();

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

		public Color AppThemeBackgroundColor
		{
			get => ColorHelper.ToColor(UserSettingsService.AppearanceSettingsService.AppThemeBackgroundColor);
			set
			{
				if (value != ColorHelper.ToColor(UserSettingsService.AppearanceSettingsService.AppThemeBackgroundColor))
				{
					UserSettingsService.AppearanceSettingsService.AppThemeBackgroundColor = value.ToString();

					App.AppThemeResourcesHelper.SetAppThemeBackgroundColor(AppThemeBackgroundColor);
					App.AppThemeResourcesHelper.ApplyResources();
				}
			}
		}
	}
}