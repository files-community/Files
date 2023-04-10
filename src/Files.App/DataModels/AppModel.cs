// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Views;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.DataModels
{
	public class AppModel : ObservableObject
	{
		private IFoldersSettingsService FoldersSettings;

		public AppModel()
		{
			FoldersSettings = Ioc.Default.GetRequiredService<IUserSettingsService>().FoldersSettingsService;
			Clipboard.ContentChanged += Clipboard_ContentChanged;
		}

		// TODO: Refactor this method
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
	}
}
