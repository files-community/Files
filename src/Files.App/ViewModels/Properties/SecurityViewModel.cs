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
	public class SecurityViewModel : ObservableObject
	{
		public SecurityViewModel(ListedItem item)
		{
			IsFolder = item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsShortcut;
			Item = item;

			InitializeCommands();
			GetAccessControlList();
		}

		public SecurityViewModel(DriveItem item)
		{
			IsFolder = true;
			Item = new ListedItem()
			{
				ItemNameRaw = item.Text,
				ItemPath = item.Path,
				PrimaryItemAttribute = StorageItemTypes.Folder
			};

			InitializeCommands();
			GetAccessControlList();
		}

		public ListedItem Item { get; }

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

		private void InitializeCommands()
		{
			ChangeOwnerCommand = new RelayCommand(ChangeOwner, () => AccessControlList is not null);
			AddAccessControlEntryCommand = new RelayCommand(AddAccessControlEntry, () => AccessControlList is not null && AccessControlList.CanReadAccessControl);
			RemoveAccessControlEntryCommand = new RelayCommand(RemoveAccessControlEntry, () => AccessControlList is not null && AccessControlList.CanReadAccessControl && SelectedAccessControlEntry is not null);
			DisableInheritanceCommand = new RelayCommand(DisableInheritance, () => AccessControlList is not null && AccessControlList.CanReadAccessControl && (AccessControlList.IsAccessControlListProtected != _isProtected));
			SetDisableInheritanceOptionCommand = new RelayCommand<string>(SetDisableInheritanceOption);
			ReplaceChildPermissionsCommand = new RelayCommand(ReplaceChildPermissions, () => AccessControlList is not null && AccessControlList.CanReadAccessControl);
		}

		private async void ChangeOwner()
		{
			var pickedObject = await OpenObjectPicker();
			if (pickedObject is not null)
			{
				bool isFolder = Item.PrimaryItemAttribute == StorageItemTypes.Folder && !Item.IsShortcut;

				// Set owner and refresh file permissions
				if (FileOperationsHelpers.SetFileOwner(Item.ItemPath, isFolder, pickedObject))
					GetAccessControlList();
			}
		}

		private async void AddAccessControlEntry()
		{
			var pickedSid = await OpenObjectPicker();
			if (pickedSid is not null)
			{
				var mapping = new AccessControlEntryPrimitiveMapping()
				{
					AccessControlType = System.Security.AccessControl.AccessControlType.Allow,
					FileSystemRights = System.Security.AccessControl.FileSystemRights.ReadAndExecute,
					InheritanceFlags = IsFolder
						? System.Security.AccessControl.InheritanceFlags.ContainerInherit | System.Security.AccessControl.InheritanceFlags.ObjectInherit
						: System.Security.AccessControl.InheritanceFlags.None,
					IsInherited = false,
					PrincipalSid = pickedSid,
					PropagationFlags = System.Security.AccessControl.PropagationFlags.None,
				};

				AccessControlList.AccessControlEntryPrimitiveMappings.Add(mapping);
				AccessControlList.AccessControlEntries.Add(new(mapping, IsFolder));

				SaveChangedAccessControlList();
			}
		}

		private void RemoveAccessControlEntry()
		{
			AccessControlList.AccessControlEntryPrimitiveMappings.RemoveAll(x =>
				x.AccessControlType == (System.Security.AccessControl.AccessControlType)SelectedAccessControlEntry.AccessControlType &&
				x.FileSystemRights == (System.Security.AccessControl.FileSystemRights)SelectedAccessControlEntry.AccessMaskFlags &&
				x.InheritanceFlags == (System.Security.AccessControl.InheritanceFlags)SelectedAccessControlEntry.InheritanceFlags &&
				x.IsInherited == SelectedAccessControlEntry.IsInherited &&
				x.PrincipalSid == SelectedAccessControlEntry.PrincipalSid &&
				x.PropagationFlags == (System.Security.AccessControl.PropagationFlags)SelectedAccessControlEntry.PropagationFlags);
			AccessControlList.AccessControlEntries.Remove(SelectedAccessControlEntry);

			SaveChangedAccessControlList();
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
			return AccessControlList.SetAccessControl();
		}

		public Task<string?> OpenObjectPicker()
		{
			return FileOperationsHelpers.OpenObjectPickerAsync(NativeWinApiHelper.CoreWindowHandle.ToInt64());
		}
	}
}
