using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.Filesystem.Security;
using Files.App.Helpers;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	public class SecurityViewModel : ObservableObject
	{
		public string Path { get; set; }

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
			set
			{
				if (SetProperty(ref _AccessControlList, value))
				{
					AddAccessControlEntryCommand.NotifyCanExecuteChanged();
					RemoveAccessControlEntryCommand.NotifyCanExecuteChanged();
				}
			}
		}

		private AccessControlEntry _SelectedAccessControlEntry;
		public AccessControlEntry SelectedAccessControlEntry
		{
			get => _SelectedAccessControlEntry;
			set
			{
				if (_SelectedAccessControlEntry is not null)
					_SelectedAccessControlEntry.IsSelected = false;

				if (SetProperty(ref _SelectedAccessControlEntry, value))
				{
					value.IsSelected = true;
					RemoveAccessControlEntryCommand.NotifyCanExecuteChanged();
				}
			}
		}

		public IRelayCommand AddAccessControlEntryCommand { get; set; }
		public IRelayCommand RemoveAccessControlEntryCommand { get; set; }

		public SecurityViewModel(ListedItem item)
		{
			IsFolder = item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsShortcut;
			Path = item.ItemPath;
			AccessControlList = FileSecurityHelpers.GetAccessControlList(Path, IsFolder);

			InitializeCommands();
		}

		public SecurityViewModel(DriveItem item)
		{
			IsFolder = true;
			Path = item.Path;
			AccessControlList = FileSecurityHelpers.GetAccessControlList(Path, IsFolder);

			InitializeCommands();
		}

		private void InitializeCommands()
		{
			AddAccessControlEntryCommand = new RelayCommand(AddAccessControlEntry, () => AccessControlList is not null && AccessControlList.IsValid);
			RemoveAccessControlEntryCommand = new RelayCommand(RemoveAccessControlEntry, () => AccessControlList is not null && AccessControlList.IsValid && SelectedAccessControlEntry is not null);
		}

		private async void AddAccessControlEntry()
		{
			var pickedSid = await OpenObjectPicker();
			if (string.IsNullOrEmpty(pickedSid))
				return;

			// TODO: Add ACE here

			SaveChangedAccessControlList();
		}

		private void RemoveAccessControlEntry()
		{
			// TODO: Remove ACE here

			SaveChangedAccessControlList();
		}

		public bool SaveChangedAccessControlList()
		{
			// TODO: Add saving codes here

			return false;
		}

		private static Task<string?> OpenObjectPicker()
			=> FileOperationsHelpers.OpenObjectPickerAsync(NativeWinApiHelper.CoreWindowHandle.ToInt64());
	}
}
