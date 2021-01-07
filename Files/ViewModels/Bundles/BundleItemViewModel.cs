using System;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Windows.UI.Xaml.Media.Imaging;
using Files.Filesystem;
using Files.SettingsInterfaces;
using System.Collections.Generic;
using Windows.Storage.FileProperties;
using Windows.Storage;
using Files.Helpers;
using Windows.UI.Xaml;

namespace Files.ViewModels.Bundles
{
	public class BundleItemViewModel : ObservableObject, IDisposable
	{
		#region Singleton

		private IJsonSettings JsonSettings => associatedInstance?.InstanceViewModel.JsonSettings;

		#endregion

		#region Private Members

		private readonly IShellPage associatedInstance;

		#endregion

		#region Public Properties

		/// <summary>
		/// The name of a bundle this item is contained within
		/// </summary>
		public string OriginBundleName { get; set; }

		public string Path { get; set; }

		public string Name
		{
			get => System.IO.Path.GetFileName(this.Path);
		}

		public FilesystemItemType TargetType { get; set; } = FilesystemItemType.File;

		private BitmapImage _Icon = null;
		public BitmapImage Icon
		{
			get => _Icon;
			set => SetProperty(ref _Icon, value);
		}

		public Uri FolderIconUri
		{
			get => new Uri("ms-appx:///Assets/FolderIcon.svg");
		}

		private Visibility _FileIconVisibility = Visibility.Visible;
		public Visibility FileIconVisibility
		{
			get => _FileIconVisibility;
			set => SetProperty(ref _FileIconVisibility, value);
		}

		#endregion

		#region Commands

		public ICommand OpenItemCommand { get; set; }

		public ICommand RemoveItemCommand { get; set; }

		#endregion

		#region Constructor

		public BundleItemViewModel(IShellPage associatedInstance, string path, FilesystemItemType targetType)
		{
			this.associatedInstance = associatedInstance;
			this.Path = path;
			this.TargetType = targetType;

			// Create commands
			OpenItemCommand = new RelayCommand(Confirm);
			RemoveItemCommand = new RelayCommand(RemoveItem);

			SetIcon();
		}

		#endregion

		#region Command Implementation

		private async void Confirm()
		{
			await associatedInstance.InteractionOperations.OpenPath(Path, TargetType);
		}

		private void RemoveItem()
		{
			if (JsonSettings.SavedBundles.ContainsKey(OriginBundleName))
			{
				Dictionary<string, List<string>> allBundles = JsonSettings.SavedBundles; // We need to do it this way for Set() to be called
				allBundles[OriginBundleName].Remove(Path);
				JsonSettings.SavedBundles = allBundles;
			}
		}

		#endregion

		#region Private Helpers

		private async void SetIcon()
		{
			if (TargetType == FilesystemItemType.Directory) // OpenDirectory
			{
				FileIconVisibility = Visibility.Collapsed;
			}
			else // NotADirectory
			{
				BitmapImage icon = new BitmapImage();
				StorageFile file = await StorageItemHelpers.ToStorageFile(Path, associatedInstance);
				StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.ListView, 24u, ThumbnailOptions.UseCurrentScale);

				if (thumbnail != null)
				{
					await icon.SetSourceAsync(thumbnail);

					Icon = icon;
					FileIconVisibility = Visibility.Visible;
					OnPropertyChanged(nameof(Icon));
				}
			}
		}


		#endregion

		#region IDisposable

		public void Dispose()
		{
			Path = null;
		}

		#endregion
	}
}
