// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	public sealed partial class AppearanceViewModel : ObservableObject
	{
		private IAppThemeModeService AppThemeModeService { get; } = Ioc.Default.GetRequiredService<IAppThemeModeService>();
		private ICommonDialogService CommonDialogService { get; } = Ioc.Default.GetRequiredService<ICommonDialogService>();
		private readonly IUserSettingsService UserSettingsService;
		private readonly IResourcesService ResourcesService;

		public List<string> Themes { get; private set; }
		public Dictionary<BackdropMaterialType, string> BackdropMaterialTypes { get; private set; } = [];

		public Dictionary<Stretch, string> ImageStretchTypes { get; private set; } = [];

		public Dictionary<VerticalAlignment, string> ImageVerticalAlignmentTypes { get; private set; } = [];

		public Dictionary<HorizontalAlignment, string> ImageHorizontalAlignmentTypes { get; private set; } = [];

		public Dictionary<StatusCenterVisibility, string> StatusCenterVisibilityOptions { get; private set; } = [];

		public ObservableCollection<AppThemeResourceItem> AppThemeResources { get; }

		public ICommand SelectImageCommand { get; }
		public ICommand RemoveImageCommand { get; }

		public AppearanceViewModel(IUserSettingsService userSettingsService, IResourcesService resourcesService)
		{
			UserSettingsService = userSettingsService;
			ResourcesService = resourcesService;
			selectedThemeIndex = (int)Enum.Parse<ElementTheme>(AppThemeModeService.AppThemeMode.ToString());

			Themes =
			[
				Strings.UseSystemSetting.GetLocalizedResource(),
				Strings.LightTheme.GetLocalizedResource(),
				Strings.DarkTheme.GetLocalizedResource()
			];

			BackdropMaterialTypes.Add(BackdropMaterialType.Solid, Strings.None.GetLocalizedResource());
			BackdropMaterialTypes.Add(BackdropMaterialType.Acrylic, Strings.Acrylic.GetLocalizedResource());
			BackdropMaterialTypes.Add(BackdropMaterialType.ThinAcrylic, Strings.ThinAcrylic.GetLocalizedResource());
			BackdropMaterialTypes.Add(BackdropMaterialType.Mica, Strings.Mica.GetLocalizedResource());
			BackdropMaterialTypes.Add(BackdropMaterialType.MicaAlt, Strings.MicaAlt.GetLocalizedResource());

			selectedBackdropMaterial = BackdropMaterialTypes[UserSettingsService.AppearanceSettingsService.AppThemeBackdropMaterial];

			AppThemeResources = AppThemeResourceFactory.AppThemeResources;


			// Background image fit options
			ImageStretchTypes.Add(Stretch.None, Strings.None.GetLocalizedResource());
			ImageStretchTypes.Add(Stretch.Fill, Strings.Fill.GetLocalizedResource());
			ImageStretchTypes.Add(Stretch.Uniform, Strings.Uniform.GetLocalizedResource());
			ImageStretchTypes.Add(Stretch.UniformToFill, Strings.UniformToFill.GetLocalizedResource());
			SelectedImageStretchType = ImageStretchTypes[UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageFit];

			// Background image allignment options

			// VerticalAlignment
			ImageVerticalAlignmentTypes.Add(VerticalAlignment.Top, Strings.Top.GetLocalizedResource());
			ImageVerticalAlignmentTypes.Add(VerticalAlignment.Center, Strings.Center.GetLocalizedResource());
			ImageVerticalAlignmentTypes.Add(VerticalAlignment.Bottom, Strings.Bottom.GetLocalizedResource());
			SelectedImageVerticalAlignmentType = ImageVerticalAlignmentTypes[UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment];

			// HorizontalAlignment
			ImageHorizontalAlignmentTypes.Add(HorizontalAlignment.Left, Strings.Left.GetLocalizedResource());
			ImageHorizontalAlignmentTypes.Add(HorizontalAlignment.Center, Strings.Center.GetLocalizedResource());
			ImageHorizontalAlignmentTypes.Add(HorizontalAlignment.Right, Strings.Right.GetLocalizedResource());
			SelectedImageHorizontalAlignmentType = ImageHorizontalAlignmentTypes[UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment];

			UpdateSelectedResource();

			// StatusCenterVisibility
			StatusCenterVisibilityOptions.Add(StatusCenterVisibility.Always, Strings.Always.GetLocalizedResource());
			StatusCenterVisibilityOptions.Add(StatusCenterVisibility.DuringOngoingFileOperations, Strings.DuringOngoingFileOperations.GetLocalizedResource());
			SelectedStatusCenterVisibilityOption = StatusCenterVisibilityOptions[UserSettingsService.AppearanceSettingsService.StatusCenterVisibility];

			SelectImageCommand = new RelayCommand(SelectBackgroundImage);
			RemoveImageCommand = new RelayCommand(RemoveBackgroundImage);
		}

		/// <summary>
		/// Opens a file picker to select a background image
		/// </summary>
		private void SelectBackgroundImage()
		{
			string[] extensions =
			[
				Strings.ImageFiles.GetLocalizedResource(), "*.bmp;*.dib;*.jpg;*.jpeg;*.jpe;*.jfif;*.gif;*.tif;*.tiff;*.png;*.heic;*.hif;*.webp",
				Strings.BitmapFiles.GetLocalizedResource(), "*.bmp;*.dib",
				"JPEG", "*.jpg;*.jpeg;*.jpe;*.jfif",
				"GIF", "*.gif",
				"TIFF", "*.tif;*.tiff",
				"PNG", "*.png",
				"HEIC", "*.heic;*.hif",
				"WEBP", "*.webp",
			];

			var result = CommonDialogService.Open_FileOpenDialog(MainWindow.Instance.WindowHandle, false, extensions, Environment.SpecialFolder.MyPictures, out var filePath);
			if (result)
				AppThemeBackgroundImageSource = filePath;
		}

		/// <summary>
		/// Clears the current background image
		/// </summary>
		private void RemoveBackgroundImage()
		{
			AppThemeBackgroundImageSource = string.Empty;
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
				if (AppThemeResources.Last().Name == Strings.Custom.GetLocalizedResource())
					AppThemeResources.Remove(AppThemeResources.Last());

				var appThemeBackgroundColor = new AppThemeResourceItem
				{
					BackgroundColor = themeBackgroundColor,
					Name = Strings.Custom.GetLocalizedResource(),
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
						ResourcesService.SetAppThemeBackgroundColor(value.ToColor());
					}
					catch
					{
						ResourcesService.SetAppThemeBackgroundColor("#00000000".ToColor());
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

		public string AppThemeBackgroundImageSource
		{
			get => UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageSource;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageSource)
				{
					UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageSource = value;

					OnPropertyChanged();
				}
			}
		}

		private string selectedImageStretchType;
		public string SelectedImageStretchType
		{
			get => selectedImageStretchType;
			set
			{
				if (SetProperty(ref selectedImageStretchType, value))
				{
					UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageFit = ImageStretchTypes.First(e => e.Value == value).Key;
				}
			}
		}

		public float AppThemeBackgroundImageOpacity
		{
			get => UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageOpacity;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageOpacity)
				{
					UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageOpacity = value;

					OnPropertyChanged();
				}
			}
		}

		private string selectedImageVerticalAlignmentType;
		public string SelectedImageVerticalAlignmentType
		{
			get => selectedImageVerticalAlignmentType;
			set
			{
				if (SetProperty(ref selectedImageVerticalAlignmentType, value))
				{
					UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageVerticalAlignment = ImageVerticalAlignmentTypes.First(e => e.Value == value).Key;
				}
			}
		}

		private string selectedImageHorizontalAlignmentType;
		public string SelectedImageHorizontalAlignmentType
		{
			get => selectedImageHorizontalAlignmentType;
			set
			{
				if (SetProperty(ref selectedImageHorizontalAlignmentType, value))
				{
					UserSettingsService.AppearanceSettingsService.AppThemeBackgroundImageHorizontalAlignment = ImageHorizontalAlignmentTypes.First(e => e.Value == value).Key;
				}
			}
		}

		public bool ShowToolbar
		{
			get => UserSettingsService.AppearanceSettingsService.ShowToolbar;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowToolbar)
				{
					UserSettingsService.AppearanceSettingsService.ShowToolbar = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowStatusBar
		{
			get => UserSettingsService.AppearanceSettingsService.ShowStatusBar;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowStatusBar)
				{
					UserSettingsService.AppearanceSettingsService.ShowStatusBar = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowTabActions
		{
			get => UserSettingsService.AppearanceSettingsService.ShowTabActions;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowTabActions)
				{
					UserSettingsService.AppearanceSettingsService.ShowTabActions = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowShelfPaneToggleButton
		{
			get => UserSettingsService.AppearanceSettingsService.ShowShelfPaneToggleButton;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.ShowShelfPaneToggleButton)
				{
					UserSettingsService.AppearanceSettingsService.ShowShelfPaneToggleButton = value;

					OnPropertyChanged();
				}
			}
		}

		private string selectedStatusCenterVisibilityOption;
		public string SelectedStatusCenterVisibilityOption
		{
			get => selectedStatusCenterVisibilityOption;
			set
			{
				if (SetProperty(ref selectedStatusCenterVisibilityOption, value))
				{
					UserSettingsService.AppearanceSettingsService.StatusCenterVisibility = StatusCenterVisibilityOptions.First(e => e.Value == value).Key;
				}
			}
		}

		public bool IsAppEnvironmentDev
		{
			get => AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev;
		}
	}
}
