using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem.Security;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	public class SecurityViewModel : ObservableObject
	{
		public string Path { get; set; }

		private readonly Window Window;

		private bool _IsFolder;
		public bool IsFolder
		{
			get => _IsFolder;
			set => SetProperty(ref _IsFolder, value);
		}

		private AccessControlList _AccessControlList;
		public AccessControlList AccessControlList
		{
			get => _AccessControlList;
			set => SetProperty(ref _AccessControlList, value);
		}

		private AccessControlEntry _SelectedAccessControlEntry;
		public AccessControlEntry SelectedAccessControlEntry
		{
			get => _SelectedAccessControlEntry;
			set
			{
				// Previous selection
				if (_SelectedAccessControlEntry is not null)
					_SelectedAccessControlEntry.IsSelected = false;

				if (value is not null && SetProperty(ref _SelectedAccessControlEntry, value))
				{
					value.IsSelected = true;
					RemoveAccessControlEntryCommand?.NotifyCanExecuteChanged();
					OnPropertyChanged(nameof(SelectedItemHeaderText));
				}
			}
		}

		public string SelectedItemHeaderText
			=> string.Format("SecurityPermissionsHeaderText".GetLocalizedResource(), SelectedAccessControlEntry.Principal.DisplayName);

		public IRelayCommand AddAccessControlEntryCommand { get; set; }
		public IRelayCommand RemoveAccessControlEntryCommand { get; set; }

		public SecurityViewModel(ListedItem item, Window window)
		{
			IsFolder = item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsShortcut;
			Path = item.ItemPath;
			AccessControlList = FileSecurityHelpers.GetAccessControlList(Path, IsFolder);
			SelectedAccessControlEntry = AccessControlList.AccessControlEntries.FirstOrDefault();
			Window = window;

			AddAccessControlEntryCommand = new AsyncRelayCommand(AddAccessControlEntry, () => AccessControlList is not null && AccessControlList.IsValid);
			RemoveAccessControlEntryCommand = new RelayCommand(RemoveAccessControlEntry, () => AccessControlList is not null && AccessControlList.IsValid && SelectedAccessControlEntry is not null);
		}

		public SecurityViewModel(DriveItem item, Window window)
		{
			IsFolder = true;
			Path = item.Path;
			AccessControlList = FileSecurityHelpers.GetAccessControlList(Path, IsFolder);
			SelectedAccessControlEntry = AccessControlList.AccessControlEntries.FirstOrDefault();
			Window = window;

			AddAccessControlEntryCommand = new AsyncRelayCommand(AddAccessControlEntry, () => AccessControlList is not null && AccessControlList.IsValid);
			RemoveAccessControlEntryCommand = new RelayCommand(RemoveAccessControlEntry, () => AccessControlList is not null && AccessControlList.IsValid && SelectedAccessControlEntry is not null);
		}

		private async Task AddAccessControlEntry()
		{
			// Pick an user or a group
			var sid = await OpenObjectPicker();
			if (string.IsNullOrEmpty(sid))
				return;

			// Add a new one to the list
			var ace = FileSecurityHelpers.InitializeDefaultAccessControlEntry(IsFolder, sid);
			AccessControlList.AccessControlEntries.Insert(0, ace);

			// Apply changes
			FileSecurityHelpers.AddAccessControlEntry(Path, sid);
		}

		private void RemoveAccessControlEntry()
		{
			// Get ACE index
			var index = AccessControlList.AccessControlEntries.IndexOf(SelectedAccessControlEntry);

			// Remove
			AccessControlList.AccessControlEntries.Remove(SelectedAccessControlEntry);
			SelectedAccessControlEntry = AccessControlList.AccessControlEntries.FirstOrDefault();

			// Apply changes
			FileSecurityHelpers.RemoveAccessControlEntry(Path, (uint)index);
		}

		public bool SaveChangedAccessControlList()
		{
			return false;
		}

		private Task<string?> OpenObjectPicker()
			=> FileOperationsHelpers.OpenObjectPickerAsync(FilePropertiesHelpers.GetWindowHandle(Window).ToInt64());
	}
}
