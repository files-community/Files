// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using Files.Backend.Services;
using Microsoft.UI.Xaml;

namespace Files.App.ViewModels.Settings
{
	public class AppearanceViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; }

		private IResourcesService ResourcesService { get; }

		private AppThemeResourceItem? _SelectedAppThemeResources;
		public AppThemeResourceItem? SelectedAppThemeResources
		{
			get => _SelectedAppThemeResources;
			set
			{
				if (value is not null && SetProperty(ref _SelectedAppThemeResources, value))
				{
					AppThemeBackgroundColor = value.BackgroundColor;

					OnPropertyChanged(nameof(_SelectedAppThemeResources));
				}
			}
		}

		private int _SelectedThemeIndex;
		public int SelectedThemeIndex
		{
			get => _SelectedThemeIndex;
			set
			{
				if (SetProperty(ref _SelectedThemeIndex, value))
				{
					ThemeHelper.RootTheme = (ElementTheme)value;

					OnPropertyChanged(nameof(SelectedElementTheme));
				}
			}
		}

		public List<string> Themes { get; private set; }

		public ObservableCollection<AppThemeResourceItem> AppThemeResources { get; }

		public ElementTheme SelectedElementTheme
			=> (ElementTheme)SelectedThemeIndex;

		public bool UseCompactStyles
		{
			get => UserSettingsService.AppearanceSettingsService.UseCompactStyles;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.UseCompactStyles)
				{
					UserSettingsService.AppearanceSettingsService.UseCompactStyles = value;

					// Apply the updated compact spacing resource
					ResourcesService.SetCompactSpacing(UseCompactStyles);
					ResourcesService.ApplyResources();

					OnPropertyChanged();
				}
			}
		}

		public string AppThemeBackgroundColor
		{
			get => UserSettingsService.AppearanceSettingsService.AppThemeBackgroundColor;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.AppThemeBackgroundColor)
				{
					UserSettingsService.AppearanceSettingsService.AppThemeBackgroundColor = value;

					// Apply the updated background resource
					ResourcesService.SetAppThemeBackgroundColor(ColorHelper.ToColor(value).FromWindowsColor());
					ResourcesService.ApplyResources();

					OnPropertyChanged();
				}
			}
		}

		public AppearanceViewModel(IUserSettingsService userSettingsService, IResourcesService resourcesService)
		{
			UserSettingsService = userSettingsService;
			ResourcesService = resourcesService;

			SelectedThemeIndex = (int)Enum.Parse(typeof(ElementTheme), ThemeHelper.RootTheme.ToString());

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

				var appThemeBackgroundColor = new AppThemeResourceItem
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
	}
}
