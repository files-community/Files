using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.Security;
using Files.App.Helpers;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	public class SecurityAdvancedViewModel : ObservableObject
	{
		public ListedItem Item { get; }

		private readonly Window Window;

		private AccessControlList _accessControlList;
		public AccessControlList AccessControlList
		{
			get => _accessControlList;
			set
			{
				if (SetProperty(ref _accessControlList, value))
				{
					ChangeOwnerCommand.NotifyCanExecuteChanged();
					AddAccessControlEntryCommand.NotifyCanExecuteChanged();
					RemoveAccessControlEntryCommand.NotifyCanExecuteChanged();
					DisableInheritanceCommand.NotifyCanExecuteChanged();
					ReplaceChildPermissionsCommand.NotifyCanExecuteChanged();
				}
			}
		}

		private AccessControlEntry _selectedAccessControlEntry;
		public AccessControlEntry SelectedAccessControlEntry
		{
			get => _selectedAccessControlEntry;
			set
			{
				if (_selectedAccessControlEntry is not null)
					_selectedAccessControlEntry.IsSelected = false;

				if (SetProperty(ref _selectedAccessControlEntry, value))
				{
					value.IsSelected = true;
					RemoveAccessControlEntryCommand.NotifyCanExecuteChanged();
				}
			}
		}

		private bool _isFolder;
		public bool IsFolder
		{
			get => _isFolder;
			set => SetProperty(ref _isFolder, value);
		}

		public string DisableInheritanceOption
		{
			get
			{
				if (!_isProtected)
					return "SecurityAdvancedInheritedEnable/Text".GetLocalizedResource();
				else if (_preserveInheritance)
					return "SecurityAdvancedInheritedConvert/Text".GetLocalizedResource();
				else
					return "SecurityAdvancedInheritedRemove/Text".GetLocalizedResource();
			}
		}

		private bool _isProtected;

		private bool _preserveInheritance;

		private GridLength _columnType = new(64d);
		public GridLength ColumnType
		{
			get => _columnType;
			set => SetProperty(ref _columnType, value);
		}

		private GridLength _columnPrincipal = new(200d);
		public GridLength ColumnPrincipal
		{
			get => _columnPrincipal;
			set => SetProperty(ref _columnPrincipal, value);
		}

		private GridLength _columnAccess = new(160d);
		public GridLength ColumnAccess
		{
			get => _columnAccess;
			set => SetProperty(ref _columnAccess, value);
		}

		private GridLength _columnInherited = new(70d);
		public GridLength ColumnInherited
		{
			get => _columnInherited;
			set => SetProperty(ref _columnInherited, value);
		}

		public RelayCommand ChangeOwnerCommand { get; set; }
		public RelayCommand AddAccessControlEntryCommand { get; set; }
		public RelayCommand RemoveAccessControlEntryCommand { get; set; }
		public RelayCommand DisableInheritanceCommand { get; set; }
		public RelayCommand<string> SetDisableInheritanceOptionCommand { get; set; }
		public RelayCommand ReplaceChildPermissionsCommand { get; set; }

		public SecurityAdvancedViewModel(ListedItem item, Window window)
		{
			IsFolder = item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsShortcut;
			Item = item;
			Window = window;

			InitializeCommands();
			GetAccessControlList();
		}

		public SecurityAdvancedViewModel(DriveItem item, Window window)
		{
			IsFolder = true;
			Item = new ListedItem()
			{
				ItemNameRaw = item.Text,
				ItemPath = item.Path,
				PrimaryItemAttribute = StorageItemTypes.Folder
			};
			Window = window;

			InitializeCommands();
			GetAccessControlList();
		}

		private void InitializeCommands()
		{
			ChangeOwnerCommand = new RelayCommand(ChangeOwner, () => AccessControlList is not null);
			AddAccessControlEntryCommand = new RelayCommand(AddAccessControlEntry, () => AccessControlList is not null && AccessControlList.IsValid);
			RemoveAccessControlEntryCommand = new RelayCommand(RemoveAccessControlEntry, () => AccessControlList is not null && AccessControlList.IsValid && SelectedAccessControlEntry is not null);
			DisableInheritanceCommand = new RelayCommand(DisableInheritance, () => AccessControlList is not null && AccessControlList.IsValid && (AccessControlList.IsProtected != _isProtected));
			SetDisableInheritanceOptionCommand = new RelayCommand<string>(SetDisableInheritanceOption);
			ReplaceChildPermissionsCommand = new RelayCommand(ReplaceChildPermissions, () => AccessControlList is not null && AccessControlList.IsValid);
		}

		private async void ChangeOwner()
		{
			var pickedObject = await OpenObjectPicker();
			if (pickedObject is not null)
			{
				bool isFolder = Item.PrimaryItemAttribute == StorageItemTypes.Folder && !Item.IsShortcut;

				// Set owner and refresh file permissions
				if (FileOperationsHelpers.SetFileOwner(Item.ItemPath, pickedObject))
					GetAccessControlList();
			}
		}

		private async void AddAccessControlEntry()
		{
			// Pick an user/group
			var sid = await OpenObjectPicker();
			if (string.IsNullOrEmpty(sid))
				return;

			// Initialize
			var ace = FileSecurityHelpers.InitializeDefaultAccessControlEntry(IsFolder, sid);
			AccessControlList.AccessControlEntries.Add(ace);

			// Save
			FileSecurityHelpers.SetAccessControlList(AccessControlList);
		}

		private void RemoveAccessControlEntry()
		{
			// Remove
			AccessControlList.AccessControlEntries.Remove(SelectedAccessControlEntry);

			// Save
			FileSecurityHelpers.SetAccessControlList(AccessControlList);
		}

		private void DisableInheritance()
		{
			// Update protection status and refresh access control
			if (FileOperationsHelpers.SetAccessRuleProtection(Item.ItemPath, IsFolder, _isProtected, _preserveInheritance))
				GetAccessControlList();
		}

		private void SetDisableInheritanceOption(string options)
		{
			_isProtected = bool.Parse(options.Split(',')[0]);
			_preserveInheritance = bool.Parse(options.Split(',')[1]);

			OnPropertyChanged(nameof(DisableInheritanceOption));
			DisableInheritanceCommand.NotifyCanExecuteChanged();
		}

		private void ReplaceChildPermissions()
		{
		}

		public void GetAccessControlList()
		{
			AccessControlList = FileOperationsHelpers.GetFilePermissions(Item.ItemPath, IsFolder);
		}

		public bool SaveChangedAccessControlList()
		{
			return false;
		}

		public Task<string?> OpenObjectPicker()
			=> FileOperationsHelpers.OpenObjectPickerAsync(FilePropertiesHelpers.GetWindowHandle(Window).ToInt64());
	}
}
