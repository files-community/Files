using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.Security;
using Files.App.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	public class SecurityViewModel : ObservableObject
	{
		public SecurityViewModel(ListedItem item)
		{
			InitializeCommands();

			IsFolder = item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsShortcut;
			Item = item;

			GetAccessControlList();
		}

		public SecurityViewModel(DriveItem item)
		{
			InitializeCommands();

			IsFolder = true;
			Item = new ListedItem()
			{
				ItemNameRaw = item.Text,
				ItemPath = item.Path,
				PrimaryItemAttribute = StorageItemTypes.Folder
			};

			GetAccessControlList();
		}

		#region Fields, Properties, Commands
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
					AddAdvancedAccessControlEntryCommand.NotifyCanExecuteChanged();
					RemoveAdvancedAccessControlEntryCommand.NotifyCanExecuteChanged();
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
				if (SetProperty(ref _selectedAccessControlEntry, value))
					RemoveAccessControlEntryCommand.NotifyCanExecuteChanged();
			}
		}

		private List<AccessControlEntryAdvanced> _selectedAdvancedAccessControlEntries;
		public List<AccessControlEntryAdvanced> SelectedAdvancedAccessControlEntries
		{
			get => _selectedAdvancedAccessControlEntries;
			set
			{
				if (SetProperty(ref _selectedAdvancedAccessControlEntries, value))
				{
					RemoveAdvancedAccessControlEntryCommand.NotifyCanExecuteChanged();
					OnPropertyChanged(nameof(SelectedAdvancedAccessControlEntry));
				}
			}
		}

		public AccessControlEntryAdvanced SelectedAdvancedAccessControlEntry
			=> SelectedAdvancedAccessControlEntries?.FirstOrDefault();

		private bool _isFolder;
		public bool IsFolder
		{
			get => _isFolder;
			set => SetProperty(ref _isFolder, value);
		}

		private bool _isProtected;

		private bool _preserveInheritance;

		public string DisableInheritanceOption
		{
			get
			{
				if (!_isProtected)
				{
					return "SecurityAdvancedInheritedEnable/Text".GetLocalizedResource();
				}
				else if (_preserveInheritance)
				{
					return "SecurityAdvancedInheritedConvert/Text".GetLocalizedResource();
				}
				else
				{
					return "SecurityAdvancedInheritedRemove/Text".GetLocalizedResource();
				}
			}
		}

		public RelayCommand ChangeOwnerCommand { get; set; }

		public RelayCommand AddAccessControlEntryCommand { get; set; }
		public RelayCommand RemoveAccessControlEntryCommand { get; set; }

		public RelayCommand AddAdvancedAccessControlEntryCommand { get; set; }
		public RelayCommand RemoveAdvancedAccessControlEntryCommand { get; set; }

		public RelayCommand DisableInheritanceCommand { get; set; }
		public RelayCommand<string> SetDisableInheritanceOptionCommand { get; set; }
		public RelayCommand ReplaceChildPermissionsCommand { get; set; }
		#endregion

		#region Methods
		private void InitializeCommands()
		{
			ChangeOwnerCommand = new RelayCommand(ChangeOwner, () => AccessControlList is not null);
			AddAccessControlEntryCommand = new RelayCommand(AddACE, () => AccessControlList is not null && AccessControlList.CanReadAccessControl);
			RemoveAccessControlEntryCommand = new RelayCommand(RemoveACE, () => AccessControlList is not null && AccessControlList.CanReadAccessControl && SelectedAccessControlEntry is not null);
			AddAdvancedAccessControlEntryCommand = new RelayCommand(AddAdvancedACE, () => AccessControlList is not null && AccessControlList.CanReadAccessControl);
			RemoveAdvancedAccessControlEntryCommand = new RelayCommand(RemoveAdvancedACE, () => AccessControlList is not null && AccessControlList.CanReadAccessControl && SelectedAdvancedAccessControlEntries is not null);
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

		private async void AddACE()
		{
			var pickedObject = await OpenObjectPicker();
			if (pickedObject is not null)
			{
				if (!AccessControlList.AccessControlEntries.Any(x => x.Principal.Sid == pickedObject))
				{
					// No existing rules, add user to list
					AccessControlList.AccessControlEntries.Add(AccessControlEntry.ForUser(AccessControlList.AccessControlEntriesAdvanced, IsFolder, pickedObject));
				}
			}
		}

		private void RemoveACE()
		{
			if (SelectedAccessControlEntry is not null)
			{
				SelectedAccessControlEntry.AllowedAccessMaskFlags = 0;
				SelectedAccessControlEntry.DeniedAccessMaskFlags = 0;

				if (!AccessControlList.AccessControlEntriesAdvanced.Any(x => x.PrincipalSid == SelectedAccessControlEntry.Principal.Sid))
				{
					// No remaining rules, remove user from list
					AccessControlList.AccessControlEntries.Remove(SelectedAccessControlEntry);
				}
			}
		}

		private async void AddAdvancedACE()
		{
			var pickedObject = await OpenObjectPicker();
			if (pickedObject is not null)
			{
				AccessControlList.AccessControlEntriesAdvanced.Add(new AccessControlEntryAdvanced(IsFolder)
				{
					AccessControlType = AccessControlType.Allow,
					AccessMaskFlags = AccessMaskFlags.ReadAndExecute,
					PrincipalSid = pickedObject,
					InheritanceFlags = IsFolder ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit : InheritanceFlags.None,
					PropagationFlags = PropagationFlags.None
				});
			}
		}

		private void RemoveAdvancedACE()
		{
			if (SelectedAdvancedAccessControlEntries is not null)
			{
				foreach (var rule in SelectedAdvancedAccessControlEntries)
					AccessControlList.AccessControlEntriesAdvanced.Remove(rule);
			}
		}

		private void DisableInheritance()
		{
			bool isFolder = Item.PrimaryItemAttribute == StorageItemTypes.Folder && !Item.IsShortcut;

			// Update protection status and refresh access control
			if (FileOperationsHelpers.SetAccessRuleProtection(Item.ItemPath, isFolder, _isProtected, _preserveInheritance))
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
			bool isFolder = Item.PrimaryItemAttribute == StorageItemTypes.Folder && !Item.IsShortcut;

			AccessControlList = FileOperationsHelpers.GetFilePermissions(Item.ItemPath, isFolder);
		}

		public bool SetAccessControlList()
		{
			// If user has no permission to change ACL
			if (AccessControlList is null ||
				!AccessControlList.CanReadAccessControl)
				return true;

			return AccessControlList.SetAccessControl();
		}

		public static Task<string?> OpenObjectPicker()
		{
			return FileOperationsHelpers.OpenObjectPickerAsync(NativeWinApiHelper.CoreWindowHandle.ToInt64());
		}
		#endregion
	}
}
