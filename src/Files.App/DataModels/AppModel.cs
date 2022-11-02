using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.ViewModels;
using Files.App.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.System.Profile;

namespace Files.App.DataModels
{
	public class AppModel : ObservableObject
	{
		// todo: refactor PaneViewModel, this doesn't belong here
		public IPaneViewModel PaneViewModel { get; } = new PaneViewModel();

		public AppModel()
		{
			Clipboard.ContentChanged += Clipboard_ContentChanged;

			//todo: this doesn't belong here
			DetectFontName();
		}

		//todo: refactor this method
		public void Clipboard_ContentChanged(object sender, object e)
		{
			try
			{
				DataPackageView packageView = Clipboard.GetContent();
				if (packageView.Contains(StandardDataFormats.StorageItems) || packageView.Contains(StandardDataFormats.Bitmap))
				{
					IsPasteEnabled = true;
				}
				else
				{
					IsPasteEnabled = false;
				}
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

		private bool multiselectEnabled;
		public bool MultiselectEnabled
		{
			get => multiselectEnabled;
			set => SetProperty(ref multiselectEnabled, value);
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
			bool isRunningNewIconsVersion = currentVersion >= newIconsMinVersion;

			if (isRunningNewIconsVersion)
			{
				SymbolFontFamily = new FontFamily("Segoe Fluent Icons");
			}
			else
			{
				SymbolFontFamily = new FontFamily("Segoe MDL2 Assets");
			}
		}
	}
}