// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;

namespace Files.App.ViewModels.Settings
{
	public sealed class AppearanceViewModel : ObservableObject
	{
		private IAppThemeModeService AppThemeModeService { get; } = Ioc.Default.GetRequiredService<IAppThemeModeService>();
		private readonly IUserSettingsService UserSettingsService;
		private readonly IResourcesService ResourcesService;

		public List<string> Themes { get; private set; }
		public Dictionary<BackdropMaterialType, string> BackdropMaterialTypes { get; private set; } = [];

		public ObservableCollection<AppThemeResourceItem> AppThemeResources { get; }

		public AppearanceViewModel(IUserSettingsService userSettingsService, IResourcesService resourcesService)
		{
			UserSettingsService = userSettingsService;
			ResourcesService = resourcesService;
			selectedThemeIndex = (int)Enum.Parse<ElementTheme>(AppThemeModeService.AppThemeMode.ToString());

			Themes =
			[
				"Default".GetLocalizedResource(),
				"LightTheme".GetLocalizedResource(),
				"DarkTheme".GetLocalizedResource()
			];

			// TODO: Re-add Solid and regular Mica when theming is revamped
			//BackdropMaterialTypes.Add(BackdropMaterialType.Solid, "Solid".GetLocalizedResource());

			BackdropMaterialTypes.Add(BackdropMaterialType.Acrylic, "Acrylic".GetLocalizedResource());

			//BackdropMaterialTypes.Add(BackdropMaterialType.Mica, "Mica".GetLocalizedResource());
			BackdropMaterialTypes.Add(BackdropMaterialType.MicaAlt, "MicaAlt".GetLocalizedResource());

			selectedBackdropMaterial = BackdropMaterialTypes[UserSettingsService.AppearanceSettingsService.AppThemeBackdropMaterial];

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
				.FirstOrDefault(p => p.BackgroundColor == themeBackgroundColor) ?? AppThemeResources[0];
		}

		private AppThemeResourceItem selectedAppThemeResources;
		public AppThemeResourceItem SelectedAppThemeResources
		{
			get => selectedAppThemeResources;
			set
			{
				if (value is not null && SetProperty(ref selectedAppThemeResources, value))
				{
					AppThemeBackgroundColor = SelectedAppThemeResources.BackgroundColor;
					OnPropertyChanged(nameof(selectedAppThemeResources));
				}
			}
		}

		private int selectedThemeIndex;
		public int SelectedThemeIndex
		{
			get => selectedThemeIndex;
			set
			{
				if (SetProperty(ref selectedThemeIndex, value))
				{
					AppThemeModeService.AppThemeMode = (ElementTheme)value;
					OnPropertyChanged(nameof(SelectedElementTheme));
				}
			}
		}

		public ElementTheme SelectedElementTheme
		{
			get => (ElementTheme)selectedThemeIndex;
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
					try
					{
						ResourcesService.SetAppThemeBackgroundColor(ColorHelper.ToColor(value).FromWindowsColor());
					}
					catch
					{
						ResourcesService.SetAppThemeBackgroundColor(ColorHelper.ToColor("#00000000").FromWindowsColor());
					}
					ResourcesService.ApplyResources();

					OnPropertyChanged();
				}
			}
		}

		private string selectedBackdropMaterial;
		public string SelectedBackdropMaterial
		{
			get => selectedBackdropMaterial;
			set
			{
				if (SetProperty(ref selectedBackdropMaterial, value))
				{
					UserSettingsService.AppearanceSettingsService.AppThemeBackdropMaterial = BackdropMaterialTypes.First(e => e.Value == value).Key;
				}
			}
		}

	}
}
