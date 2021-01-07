using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Windows.UI.Xaml.Media.Imaging;
using Files.Filesystem;
using Files.SettingsInterfaces;
using System.Collections.Generic;

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
			get => System.IO.Path.GetFileNameWithoutExtension(this.Path);
		}

		public FilesystemItemType TargetType { get; set; } = FilesystemItemType.File;

		public BitmapImage Icon
		{
			get
			{
				if (TargetType == FilesystemItemType.Directory) // OpenDirectory
				{
					return (BitmapImage)null;
				}
				else // NotADirectory
				{
					return Task.Run(async () => await associatedInstance.FilesystemViewModel.LoadIconOverlayAsync(Path, 80u)).GetAwaiter().GetResult().Icon;
				}
			}
		}

		#endregion

		#region Commands

		public ICommand OpenItemCommand { get; set; }

		public ICommand RemoveItemCommand { get; set; }

		#endregion

		#region Constructor

		public BundleItemViewModel(IShellPage associatedInstance)
		{
			this.associatedInstance = associatedInstance;

			// Create commands
			OpenItemCommand = new RelayCommand(Confirm);
			RemoveItemCommand = new RelayCommand(RemoveItem);
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

		#region IDisposable

		public void Dispose()
		{
			Path = null;
		}

		#endregion
	}
}
