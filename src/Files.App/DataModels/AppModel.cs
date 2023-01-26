using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.ServicesImplementation.Settings;
using Files.App.ViewModels;
using Files.App.Views;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.System.Profile;

namespace Files.App.DataModels
{
	public class AppModel : ObservableObject
	{
		private IFoldersSettingsService FoldersSettings;

		public bool ShowSelectionCheckboxes
		{
			get => FoldersSettings.ShowSelectionCheckboxes;
			set => FoldersSettings.ShowSelectionCheckboxes = value;
		}

		public AppModel()
		{
			FoldersSettings = Ioc.Default.GetRequiredService<IUserSettingsService>().FoldersSettingsService;
			FoldersSettings.PropertyChanged += FoldersSettings_PropertyChanged; ;
			Clipboard.ContentChanged += Clipboard_ContentChanged;

			//todo: this doesn't belong here
			DetectFontName();
		}

		private void FoldersSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(FoldersSettingsService.ShowSelectionCheckboxes))
			{
				OnPropertyChanged(nameof(ShowSelectionCheckboxes));
			}
		}

		//todo: refactor this method
		public void Clipboard_ContentChanged(object sender, object e)
		{
			try
			{
				DataPackageView packageView = Clipboard.GetContent();
				IsPasteEnabled = packageView.Contains(StandardDataFormats.StorageItems) || packageView.Contains(StandardDataFormats.Bitmap);
			}
			catch
			{
				IsPasteEnabled = false;
			}
		}

		private int tabStripSelectedIndex = 0;
		public int TabStripSelectedIndex
		{
			get => tabStripSelectedIndex;
			set
			{
				if (value >= 0)
				{
					if (tabStripSelectedIndex != value)
					{
						SetProperty(ref tabStripSelectedIndex, value);
					}

					if (value < MainPageViewModel.AppInstances.Count)
					{
						Frame rootFrame = (Frame)App.Window.Content;
						var mainView = (MainPage)rootFrame.Content;
						mainView.ViewModel.SelectedTabItem = MainPageViewModel.AppInstances[value];
					}
				}
			}
		}

		private bool isAppElevated = false;
		public bool IsAppElevated
		{
			get => isAppElevated;
			set => SetProperty(ref isAppElevated, value);
		}

		private bool isPasteEnabled = false;
		public bool IsPasteEnabled
		{
			get => isPasteEnabled;
			set => SetProperty(ref isPasteEnabled, value);
		}

		private bool isQuickLookAvailable;
		public bool IsQuickLookAvailable
		{
			get => isQuickLookAvailable;
			set => SetProperty(ref isQuickLookAvailable, value);
		}

		private FontFamily symbolFontFamily;
		public FontFamily SymbolFontFamily
		{
			get => symbolFontFamily;
			set => SetProperty(ref symbolFontFamily, value);
		}

		//todo: refactor this method
		private void DetectFontName()
		{
			var rawVersion = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
			var currentVersion = new Version((int)((rawVersion & 0xFFFF000000000000) >> 48), (int)((rawVersion & 0x0000FFFF00000000) >> 32), (int)((rawVersion & 0x00000000FFFF0000) >> 16), (int)(rawVersion & 0x000000000000FFFF));
			var newIconsMinVersion = new Version(10, 0, 21327, 1000);
			bool isWindows11 = currentVersion >= newIconsMinVersion;

			SymbolFontFamily = (isWindows11) ? new FontFamily("Segoe Fluent Icons") : new FontFamily("Segoe MDL2 Assets");
		}
	}
}