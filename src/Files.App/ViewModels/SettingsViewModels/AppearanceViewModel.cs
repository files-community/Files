using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Helpers;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.Views.SettingsPages.Appearance;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Files.App.ViewModels.SettingsViewModels
{
	public class AppearanceViewModel : ObservableObject
	{
		private readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

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
			UpdateSelectedResource();
		}

		/// <summary>
		/// Selects the AppThemeResource corresponding to the current settings
		/// </summary>
		private void UpdateSelectedResource()
		{
			var themeBackgroundColor = AppThemeBackgroundColor;

			// Add color to the collection if it's not already there
			if (!AppThemeResources.Any(p => p.BackgroundColor == themeBackgroundColor))
			{
				// Remove current value before adding a new one
				if (AppThemeResources.Last().Name == "Custom")
					AppThemeResources.Remove(AppThemeResources.Last());

				var appThemeBackgroundColor = new AppThemeResource
				{
					BackgroundColor = themeBackgroundColor,
					Name = "Custom"
				};
				AppThemeResources.Add(appThemeBackgroundColor);
			}

			SelectedAppThemeResources = AppThemeResources
				.Where(p => p.BackgroundColor == themeBackgroundColor)
				.FirstOrDefault() ?? AppThemeResources[0];
		}


		private AppThemeResource selectedAppThemeResources;
		public AppThemeResource SelectedAppThemeResources
		{
			get => selectedAppThemeResources;
			set
			{
				if (SetProperty(ref selectedAppThemeResources, value))
				{
					AppThemeBackgroundColor = SelectedAppThemeResources.BackgroundColor;
					OnPropertyChanged(nameof(selectedAppThemeResources));
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
			get => userSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu;
			set
			{
				if (value != userSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu)
				{
					userSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu = value;
					OnPropertyChanged();
				}
			}
		}

		public bool UseCompactStyles
		{
			get => userSettingsService.AppearanceSettingsService.UseCompactStyles;
			set
			{
				if (value != userSettingsService.AppearanceSettingsService.UseCompactStyles)
				{
					userSettingsService.AppearanceSettingsService.UseCompactStyles = value;

					// Apply the updated compact spacing resource
					App.AppThemeResourcesHelper.SetCompactSpacing(UseCompactStyles);
					App.AppThemeResourcesHelper.ApplyResources();

					OnPropertyChanged();
				}
			}
		}

		public string AppThemeBackgroundColor
		{
			get => userSettingsService.AppearanceSettingsService.AppThemeBackgroundColor;
			set
			{
				if (value != userSettingsService.AppearanceSettingsService.AppThemeBackgroundColor)
				{
					userSettingsService.AppearanceSettingsService.AppThemeBackgroundColor = value;
          
					// Apply the updated background resource
					App.AppThemeResourcesHelper.SetAppThemeBackgroundColor(ColorHelper.ToColor(value));
					App.AppThemeResourcesHelper.ApplyResources();

					OnPropertyChanged();
				}
			}
		}
	}
}