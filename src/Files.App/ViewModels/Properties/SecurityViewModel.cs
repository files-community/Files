using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.Permissions;
using Files.App.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.ViewModels.Properties
{
	public class SecurityViewModel : ObservableObject
	{
		public ListedItem Item { get; }

		public SecurityViewModel(ListedItem item)
		{
			Item = item;

			IsFolder = Item.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !Item.IsShortcut;

			InitCommands();
		}

		public SecurityViewModel(DriveItem item)
		{
			Item = new ListedItem()
			{
				ItemNameRaw = item.Text,
				ItemPath = item.Path,
				PrimaryItemAttribute = Windows.Storage.StorageItemTypes.Folder
			};

			IsFolder = true;

			InitCommands();
		}

		private void InitCommands()
		{
			EditOwnerCommand = new RelayCommand(EditOwner, () => FilePermissions is not null);
			AddRulesForUserCommand = new RelayCommand(AddRulesForUser, () => FilePermissions is not null && FilePermissions.CanReadFilePermissions);
			RemoveRulesForUserCommand = new RelayCommand(RemoveRulesForUser, () => FilePermissions is not null && FilePermissions.CanReadFilePermissions && SelectedRuleForUser is not null);
			AddAccessRuleCommand = new RelayCommand(AddAccessRule, () => FilePermissions is not null && FilePermissions.CanReadFilePermissions);
			RemoveAccessRuleCommand = new RelayCommand(RemoveAccessRule, () => FilePermissions is not null && FilePermissions.CanReadFilePermissions && SelectedAccessRules is not null);
			DisableInheritanceCommand = new RelayCommand(DisableInheritance, () => FilePermissions is not null && FilePermissions.CanReadFilePermissions && (FilePermissions.AreAccessRulesProtected != isProtected));
			SetDisableInheritanceOptionCommand = new RelayCommand<string>(SetDisableInheritanceOption);
			ReplaceChildPermissionsCommand = new RelayCommand(ReplaceChildPermissions, () => FilePermissions is not null && FilePermissions.CanReadFilePermissions);
		}

		public RelayCommand EditOwnerCommand { get; set; }
		public RelayCommand AddRulesForUserCommand { get; set; }
		public RelayCommand RemoveRulesForUserCommand { get; set; }
		public RelayCommand AddAccessRuleCommand { get; set; }
		public RelayCommand RemoveAccessRuleCommand { get; set; }
		public RelayCommand DisableInheritanceCommand { get; set; }
		public RelayCommand<string> SetDisableInheritanceOptionCommand { get; set; }
		public RelayCommand ReplaceChildPermissionsCommand { get; set; }

		private FilePermissionsManager filePermissions;
		public FilePermissionsManager FilePermissions
		{
			get => filePermissions;
			set
			{
				if (SetProperty(ref filePermissions, value))
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

		private AccessControlEntry selectedRuleForUser;
		public AccessControlEntry SelectedRuleForUser
		{
			get => selectedRuleForUser;
			set
			{
				if (SetProperty(ref selectedRuleForUser, value))
				{
					RemoveRulesForUserCommand.NotifyCanExecuteChanged();
				}
			}
		}

		private List<AccessControlEntryAdvanced> selectedAccessRules;
		public List<AccessControlEntryAdvanced> SelectedAccessRules
		{
			get => selectedAccessRules;
			set
			{
				if (SetProperty(ref selectedAccessRules, value))
				{
					RemoveAccessRuleCommand.NotifyCanExecuteChanged();
					OnPropertyChanged(nameof(SelectedAccessRule));
				}
			}
		}

		public AccessControlEntryAdvanced SelectedAccessRule
			=> SelectedAccessRules?.FirstOrDefault();

		private bool isFolder;
		public bool IsFolder
		{
			get => isFolder;
			set => SetProperty(ref isFolder, value);
		}

		private bool isProtected;

		private bool preserveInheritance;

		public string DisableInheritanceOption
		{
			get
			{
				if (!isProtected)
				{
					return "SecurityAdvancedInheritedEnable/Text".GetLocalizedResource();
				}
				else if (preserveInheritance)
				{
					return "SecurityAdvancedInheritedConvert/Text".GetLocalizedResource();
				}
				else
				{
					return "SecurityAdvancedInheritedRemove/Text".GetLocalizedResource();
				}
			}
		}

		private async void DisableInheritance()
		{
			if (SetAccessRuleProtection(isProtected, preserveInheritance))
			{
				GetFilePermissions(); // Refresh file permissions
			}
		}

		private void SetDisableInheritanceOption(string options)
		{
			isProtected = Boolean.Parse(options.Split(',')[0]);
			preserveInheritance = Boolean.Parse(options.Split(',')[1]);
			OnPropertyChanged(nameof(DisableInheritanceOption));
			DisableInheritanceCommand.NotifyCanExecuteChanged();
		}

		private void ReplaceChildPermissions()
		{
		}

		private async void AddAccessRule()
		{
			var pickedObject = await OpenObjectPicker();
			if (pickedObject is not null)
			{
				FilePermissions.AccessRules.Add(new AccessControlEntryAdvanced(IsFolder)
				{
					AccessControlType = AccessControlType.Allow,
					FileSystemRights = AccessMask.ReadAndExecute,
					PrincipalSid = pickedObject,
					InheritanceFlags = IsFolder ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit : InheritanceFlags.None,
					PropagationFlags = PropagationFlags.None
				});
			}
		}

		private void RemoveAccessRule()
		{
			if (SelectedAccessRules is not null)
			{
				foreach (var rule in SelectedAccessRules)
					FilePermissions.AccessRules.Remove(rule);
			}
		}

		private async void EditOwner()
		{
			var pickedObject = await OpenObjectPicker();
			if (pickedObject is not null)
			{
				// Refresh file permissions
				if (SetFileOwner(pickedObject))
					GetFilePermissions();
			}
		}

		private async void AddRulesForUser()
		{
			var pickedObject = await OpenObjectPicker();
			if (pickedObject is not null)
			{
				if (!FilePermissions.RulesForUsers.Any(x => x.UserGroup.Sid == pickedObject))
				{
					// No existing rules, add user to list
					FilePermissions.RulesForUsers.Add(AccessControlEntry.ForUser(FilePermissions.AccessRules, IsFolder, pickedObject));
				}
			}
		}

		private void RemoveRulesForUser()
		{
			if (SelectedRuleForUser is not null)
			{
				SelectedRuleForUser.AllowRights = 0;
				SelectedRuleForUser.DenyRights = 0;

				if (!FilePermissions.AccessRules.Any(x => x.PrincipalSid == SelectedRuleForUser.UserGroup.Sid))
				{
					// No remaining rules, remove user from list
					FilePermissions.RulesForUsers.Remove(SelectedRuleForUser);
				}
			}
		}

		public void GetFilePermissions()
		{
			bool isFolder = Item.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !Item.IsShortcut;
			FilePermissions = new FilePermissionsManager(FileOperationsHelpers.GetFilePermissions(Item.ItemPath, isFolder));
		}

		public bool SetFilePermissions()
		{
			if (FilePermissions is null || !FilePermissions.CanReadFilePermissions)
				return true;

			return FilePermissions.ToFilePermissions().SetPermissions();
		}

		public bool SetFileOwner(string ownerSid)
		{
			bool isFolder = Item.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !Item.IsShortcut;

			return FileOperationsHelpers.SetFileOwner(Item.ItemPath, isFolder, ownerSid);
		}

		public Task<string?> OpenObjectPicker()
			=> FileOperationsHelpers.OpenObjectPickerAsync(NativeWinApiHelper.CoreWindowHandle.ToInt64());

		public bool SetAccessRuleProtection(bool isProtected, bool preserveInheritance)
		{
			bool isFolder = Item.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder && !Item.IsShortcut;

			return FileOperationsHelpers.SetAccessRuleProtection(Item.ItemPath, isFolder, isProtected, preserveInheritance);
		}
	}
}
