using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Files.App.ViewModels.Properties
{
    public class LibraryViewModel : ObservableObject
    {
		public ObservableCollection<LibraryFolder> LibraryFolders { get; } = new();

		public bool IsLibraryEmpty
			=> LibraryFolders.Count == 0;

		private int _SelectedFolderIndex = -1;
		public int SelectedFolderIndex
		{
			get => _SelectedFolderIndex;
			set
			{
				if (SetProperty(ref _SelectedFolderIndex, value))
				{
					OnPropertyChanged(nameof(IsNotDefaultFolderSelected));
				}
			}
		}

		public bool IsNotDefaultFolderSelected
			=> SelectedFolderIndex >= 0 && !LibraryFolders[SelectedFolderIndex].IsDefault;

		private bool _IsPinned;
		public bool IsPinned
		{
			get => _IsPinned;
			set => SetProperty(ref _IsPinned, value);
		}

		public IRelayCommand AddLocationCommand { get; }
		public IRelayCommand SetDefaultLocationCommand { get; }
		public IRelayCommand RemoveLocationCommand { get; }

		public LibraryViewModel()
		{
			AddLocationCommand = new AsyncRelayCommand(AddLocation);
			SetDefaultLocationCommand = new RelayCommand(SetDefaultLocation);
			RemoveLocationCommand = new RelayCommand(RemoveLocation);
		}

		public void Initialize(BaseProperties BaseProperties)
		{
			if (BaseProperties is LibraryProperties props)
			{
				LibraryFolders.Clear();
				if (!props.Library.IsEmpty)
				{
					foreach (var path in props.Library.Folders)
					{
						LibraryFolders.Add(new LibraryFolder
						{
							Path = path,
							IsDefault = string.Equals(path, props.Library.DefaultSaveFolder, StringComparison.OrdinalIgnoreCase),
						});
					}

					OnPropertyChanged(nameof(IsLibraryEmpty));
				}
			}
		}

		public async Task<bool> SaveChanges(BaseProperties BaseProperties)
		{
			if (BaseProperties is LibraryProperties props)
			{
				// Skip checks / updates and close dialog when the library is empty
				if (IsLibraryEmpty)
					return true;

				// Skip updates and close dialog when nothing changed
				if (!IsChanged(props.Library, out string newDefaultSaveFolder, out string[] newFolders, out bool? newIsPinned))
					return true;

				while (true)
				{
					var dialog = DynamicDialogFactory.GetFor_PropertySaveErrorDialog();

					try
					{
						var library = await Task.Run(() => App.LibraryManager.UpdateLibrary(props.Library.ItemPath, newDefaultSaveFolder, newFolders, newIsPinned));
						if (library is not null)
						{
							props.UpdateLibrary(new LibraryItem(library));
							return true;
						}
						// TODO: show / throw error about the failure?
						return false;
					}
					catch
					{
						await SetContentDialogRoot(dialog).TryShowAsync();
						switch (dialog.DynamicResult)
						{
							case DynamicDialogResult.Primary:
								break;
							case DynamicDialogResult.Secondary:
								return true;
							case DynamicDialogResult.Cancel:
								return false;
						}
					}
				}
			}

			return false;
		}

		// WINUI3
		private static FolderPicker InitializeWithWindow(FolderPicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, App.WindowHandle);

			return obj;
		}

		// WINUI3
		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (FilePropertiesHelpers.IsWinUI3)
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;

			return contentDialog;
		}

		private bool IsChanged(LibraryItem lib, out string newDefaultSaveFolder, out string[] newFolders, out bool? newIsPinned)
		{
			bool isChanged = false;

			newDefaultSaveFolder = null;
			newFolders = null;
			newIsPinned = null;

			var defaultSaveFolderPath = LibraryFolders.FirstOrDefault(f => f.IsDefault)?.Path;
			if (!string.Equals(defaultSaveFolderPath, lib.DefaultSaveFolder, StringComparison.OrdinalIgnoreCase))
			{
				newDefaultSaveFolder = defaultSaveFolderPath;
				isChanged = true;
			}

			if ((lib.Folders?.Count ?? 0) != LibraryFolders.Count || lib.Folders?.SequenceEqual(LibraryFolders.Select(f => f.Path), StringComparer.OrdinalIgnoreCase) != true)
			{
				newFolders = LibraryFolders.Select(f => f.Path).ToArray();
				isChanged = true;
			}

			if (IsPinned != lib.IsPinned)
			{
				newIsPinned = IsPinned;
				isChanged = true;
			}

			return isChanged;
		}

		private async Task AddLocation()
		{
			var folderPicker = InitializeWithWindow(new FolderPicker());
			folderPicker.FileTypeFilter.Add("*");

			var folder = await folderPicker.PickSingleFolderAsync();
			if (folder is not null && !LibraryFolders.Any((f) => string.Equals(folder.Path, f.Path, StringComparison.OrdinalIgnoreCase)))
			{
				bool isDefault = LibraryFolders.Count == 0;
				LibraryFolders.Add(new LibraryFolder { Path = folder.Path, IsDefault = isDefault });
				if (isDefault)
				{
					OnPropertyChanged(nameof(IsLibraryEmpty));
				}
			}
		}

		private void SetDefaultLocation()
		{
			int index = SelectedFolderIndex;
			if (index >= 0)
			{
				foreach (var f in LibraryFolders)
				{
					f.IsDefault = false;
				}
				LibraryFolders[index].IsDefault = true;
			}
		}

		private void RemoveLocation()
		{
			int index = SelectedFolderIndex;
			if (index >= 0)
			{
				LibraryFolders.RemoveAt(index);
				if (index > 0)
				{
					SelectedFolderIndex = index - 1;
				}
				else if (LibraryFolders.Count > 0)
				{
					SelectedFolderIndex = 0;
				}
			}
		}
	}

	public class LibraryFolder : ObservableObject
	{
		public string? Path { get; set; }

		private bool _IsDefault;
		public bool IsDefault
		{
			get => _IsDefault;
			set => SetProperty(ref _IsDefault, value);
		}
	}
}
