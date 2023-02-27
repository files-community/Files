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
			Item = item;

			IsFolder = Item.PrimaryItemAttribute == StorageItemTypes.Folder && !Item.IsShortcut;

			InitializeCommands();
		}

		public SecurityViewModel(DriveItem item)
		{
			Item = new ListedItem()
			{
				ItemNameRaw = item.Text,
				ItemPath = item.Path,
				PrimaryItemAttribute = StorageItemTypes.Folder
			};

			IsFolder = true;

			InitializeCommands();
		}

		#region Fields, Properties, Commands
		public ListedItem Item { get; }

		private AccessControlListController _accessControlListController;
		public AccessControlListController AccessControlListController
		{
			get => _accessControlListController;
			set
			{
				if (SetProperty(ref _accessControlListController, value))
				{
					EditOwnerCommand.NotifyCanExecuteChanged();
					AddRulesForUserCommand.NotifyCanExecuteChanged();
					RemoveRulesForUserCommand.NotifyCanExecuteChanged();
					AddAccessRuleCommand.NotifyCanExecuteChanged();
					RemoveAccessRuleCommand.NotifyCanExecuteChanged();
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
				{
					RemoveRulesForUserCommand.NotifyCanExecuteChanged();
				}
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
					RemoveAccessRuleCommand.NotifyCanExecuteChanged();
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

		public RelayCommand EditOwnerCommand { get; set; }
		public RelayCommand AddRulesForUserCommand { get; set; }
		public RelayCommand RemoveRulesForUserCommand { get; set; }
		public RelayCommand AddAccessRuleCommand { get; set; }
		public RelayCommand RemoveAccessRuleCommand { get; set; }
		public RelayCommand DisableInheritanceCommand { get; set; }
		public RelayCommand<string> SetDisableInheritanceOptionCommand { get; set; }
		public RelayCommand ReplaceChildPermissionsCommand { get; set; }
		#endregion

		#region Methods
		private void InitializeCommands()
		{
			EditOwnerCommand = new RelayCommand(ChangeOwner, () => AccessControlListController is not null);
			AddRulesForUserCommand = new RelayCommand(AddACE, () => AccessControlListController is not null && AccessControlListController.CanReadAccessControl);
			RemoveRulesForUserCommand = new RelayCommand(RemoveACE, () => AccessControlListController is not null && AccessControlListController.CanReadAccessControl && SelectedAccessControlEntry is not null);
			AddAccessRuleCommand = new RelayCommand(AddAdvancedACE, () => AccessControlListController is not null && AccessControlListController.CanReadAccessControl);
			RemoveAccessRuleCommand = new RelayCommand(RemoveAdvancedACE, () => AccessControlListController is not null && AccessControlListController.CanReadAccessControl && SelectedAdvancedAccessControlEntries is not null);
			DisableInheritanceCommand = new RelayCommand(DisableInheritance, () => AccessControlListController is not null && AccessControlListController.CanReadAccessControl && (AccessControlListController.IsAccessControlListProtected != _isProtected));
			SetDisableInheritanceOptionCommand = new RelayCommand<string>(SetDisableInheritanceOption);
			ReplaceChildPermissionsCommand = new RelayCommand(ReplaceChildPermissions, () => AccessControlListController is not null && AccessControlListController.CanReadAccessControl);
		}

		private void DisableInheritance()
		{
			if (SetAccessControlProtection(_isProtected, _preserveInheritance))
			{
				// Refresh file permissions
				GetFilePermissions();
			}
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

		private async void AddAdvancedACE()
		{
			var pickedObject = await OpenObjectPicker();
			if (pickedObject is not null)
			{
				AccessControlListController.AccessControlEntriesAdvanced.Add(new AccessControlEntryAdvanced(IsFolder)
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
					AccessControlListController.AccessControlEntriesAdvanced.Remove(rule);
			}
		}

		private async void ChangeOwner()
		{
			var pickedObject = await OpenObjectPicker();
			if (pickedObject is not null)
			{
				// Refresh file permissions
				if (SetFileOwner(pickedObject))
					GetFilePermissions();
			}
		}

		private async void AddACE()
		{
			var pickedObject = await OpenObjectPicker();
			if (pickedObject is not null)
			{
				if (!AccessControlListController.AccessControlEntries.Any(x => x.Principal.Sid == pickedObject))
				{
					// No existing rules, add user to list
					AccessControlListController.AccessControlEntries.Add(AccessControlEntry.ForUser(AccessControlListController.AccessControlEntriesAdvanced, IsFolder, pickedObject));
				}
			}
		}

		private void RemoveACE()
		{
			if (SelectedAccessControlEntry is not null)
			{
				SelectedAccessControlEntry.AllowedAccessMaskFlags = 0;
				SelectedAccessControlEntry.DeniedAccessMaskFlags = 0;

				if (!AccessControlListController.AccessControlEntriesAdvanced.Any(x => x.PrincipalSid == SelectedAccessControlEntry.Principal.Sid))
				{
					// No remaining rules, remove user from list
					AccessControlListController.AccessControlEntries.Remove(SelectedAccessControlEntry);
				}
			}
		}

		public void GetFilePermissions()
		{
			bool isFolder = Item.PrimaryItemAttribute == StorageItemTypes.Folder && !Item.IsShortcut;

			var acl = FileOperationsHelpers.GetFilePermissions(Item.ItemPath, isFolder);
			AccessControlListController = new(acl);
		}

		public bool SetFilePermissions()
		{
			// If user has no permission to change ACL
			if (AccessControlListController is null ||
				!AccessControlListController.CanReadAccessControl)
				return true;

			return AccessControlListController.ToFilePermissions().SetAccessControl();
		}

		public bool SetFileOwner(string ownerSid)
		{
			bool isFolder = Item.PrimaryItemAttribute == StorageItemTypes.Folder && !Item.IsShortcut;

			return FileOperationsHelpers.SetFileOwner(Item.ItemPath, isFolder, ownerSid);
		}

		public bool SetAccessControlProtection(bool isProtected, bool preserveInheritance)
		{
			bool isFolder = Item.PrimaryItemAttribute == StorageItemTypes.Folder && !Item.IsShortcut;

			return FileOperationsHelpers.SetAccessRuleProtection(Item.ItemPath, isFolder, isProtected, preserveInheritance);
		}

		public static Task<string?> OpenObjectPicker()
			=> FileOperationsHelpers.OpenObjectPickerAsync(NativeWinApiHelper.CoreWindowHandle.ToInt64());
		#endregion
	}
}
