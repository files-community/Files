using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.Permissions;
using Files.App.Helpers;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	public class SecurityProperties : ObservableObject
	{
		public ListedItem? Item { get; }

		private FilePermissionsManager? _filePermissions;

		public FilePermissionsManager? FilePermissions
		{
			get => _filePermissions;
			set
			{
				if (SetProperty(ref _filePermissions, value))
				{
					EditOwnerCommand?.NotifyCanExecuteChanged();
					AddRulesForUserCommand?.NotifyCanExecuteChanged();
					RemoveRulesForUserCommand?.NotifyCanExecuteChanged();
					AddAccessRuleCommand?.NotifyCanExecuteChanged();
					RemoveAccessRuleCommand?.NotifyCanExecuteChanged();
					DisableInheritanceCommand?.NotifyCanExecuteChanged();
					ReplaceChildPermissionsCommand?.NotifyCanExecuteChanged();
				}
			}
		}

		private RulesForUser? _selectedRuleForUser;

		public RulesForUser? SelectedRuleForUser
		{
			get => _selectedRuleForUser;
			set
			{
				if (SetProperty(ref _selectedRuleForUser, value))
				{
					RemoveRulesForUserCommand?.NotifyCanExecuteChanged();
				}
			}
		}

		private List<FileSystemAccessRuleForUI>? _selectedAccessRules;

		public List<FileSystemAccessRuleForUI>? SelectedAccessRules
		{
			get => _selectedAccessRules;
			set
			{
				if (SetProperty(ref _selectedAccessRules, value))
				{
					RemoveAccessRuleCommand?.NotifyCanExecuteChanged();
					OnPropertyChanged(nameof(SelectedAccessRule));
				}
			}
		}

		public FileSystemAccessRuleForUI? SelectedAccessRule => SelectedAccessRules?.FirstOrDefault();

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
					return "SecurityAdvancedInheritedEnable/Text".GetLocalizedResource();
				else if (_preserveInheritance)
					return "SecurityAdvancedInheritedConvert/Text".GetLocalizedResource();
				else
					return "SecurityAdvancedInheritedRemove/Text".GetLocalizedResource();
			}
		}

		public RelayCommand? EditOwnerCommand { get; private set; }
		public RelayCommand? AddRulesForUserCommand { get; private set; }
		public RelayCommand? RemoveRulesForUserCommand { get; private set; }
		public RelayCommand? AddAccessRuleCommand { get; private set; }
		public RelayCommand? RemoveAccessRuleCommand { get; private set; }
		public RelayCommand? DisableInheritanceCommand { get; private set; }
		public RelayCommand<string>? SetDisableInheritanceOptionCommand { get; private set; }
		public RelayCommand? ReplaceChildPermissionsCommand { get; private set; }

		public SecurityProperties(ListedItem item)
		{
			Item = item;
			IsFolder = Item.PrimaryItemAttribute == StorageItemTypes.Folder && !Item.IsShortcut;

			InitCommands();
		}

		public SecurityProperties(DriveItem item)
		{
			Item = new ListedItem
			{
				ItemNameRaw = item.Text,
				ItemPath = item.Path,
				PrimaryItemAttribute = StorageItemTypes.Folder
			};

			IsFolder = true;

			InitCommands();
		}

		private void InitCommands()
		{
			EditOwnerCommand = new RelayCommand(EditOwner, () => FilePermissions != null);
			AddRulesForUserCommand = new RelayCommand(AddRulesForUser, () => FilePermissions is { CanReadFilePermissions: true });
			RemoveRulesForUserCommand = new RelayCommand(RemoveRulesForUser, () => FilePermissions is { CanReadFilePermissions: true } && SelectedRuleForUser != null);
			AddAccessRuleCommand = new RelayCommand(AddAccessRule, () => FilePermissions is { CanReadFilePermissions: true });
			RemoveAccessRuleCommand = new RelayCommand(RemoveAccessRule, () => FilePermissions is { CanReadFilePermissions: true } && SelectedAccessRules != null);
			DisableInheritanceCommand = new RelayCommand(DisableInheritance, () => FilePermissions is { CanReadFilePermissions: true } && FilePermissions.AreAccessRulesProtected != _isProtected);
			SetDisableInheritanceOptionCommand = new RelayCommand<string>(SetDisableInheritanceOption);
			ReplaceChildPermissionsCommand = new RelayCommand(ReplaceChildPermissions, () => FilePermissions is { CanReadFilePermissions: true });
		}

		private async void DisableInheritance()
		{
			if (await SetAccessRuleProtection(_isProtected, _preserveInheritance))
				GetFilePermissions(); // Refresh file permissions
		}

		private void SetDisableInheritanceOption(string? options)
		{
			if (string.IsNullOrEmpty(options))
				return;

			_isProtected = bool.Parse(options.Split(',')[0]);
			_preserveInheritance = bool.Parse(options.Split(',')[1]);
			OnPropertyChanged(nameof(DisableInheritanceOption));
			DisableInheritanceCommand?.NotifyCanExecuteChanged();
		}

		private void ReplaceChildPermissions()
		{
			// Doesn't exist in FileOperationsHandler?
			throw new NotImplementedException();
		}

		private async void AddAccessRule()
		{
			var pickedObject = await OpenObjectPicker();

			if (pickedObject == null)
				return;

			FilePermissions?.AccessRules.Add(new FileSystemAccessRuleForUI(IsFolder)
			{
				AccessControlType = AccessControlType.Allow,
				FileSystemRights = FileSystemRights.ReadAndExecute,
				IdentityReference = pickedObject,
				InheritanceFlags = IsFolder ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit : InheritanceFlags.None,
				PropagationFlags = PropagationFlags.None
			});
		}

		private void RemoveAccessRule()
		{
			if (SelectedAccessRules == null)
				return;

			foreach (var rule in SelectedAccessRules)
				FilePermissions?.AccessRules.Remove(rule);
		}

		private async void EditOwner()
		{
			var pickedObject = await OpenObjectPicker();

			if (pickedObject == null)
				return;

			if (await SetFileOwner(pickedObject))
				GetFilePermissions(); // Refresh file permissions
		}

		private async void AddRulesForUser()
		{
			var pickedObject = await OpenObjectPicker();

			if (pickedObject == null)
				return;

			if (FilePermissions != null && FilePermissions.RulesForUsers.All(x => x.UserGroup.Sid != pickedObject))
			{
				// No existing rules, add user to list
				FilePermissions.RulesForUsers.Add(RulesForUser.ForUser(FilePermissions.AccessRules, IsFolder, pickedObject));
			}
		}

		private void RemoveRulesForUser()
		{
			if (SelectedRuleForUser == null)
				return;

			SelectedRuleForUser.AllowRights = 0;
			SelectedRuleForUser.DenyRights = 0;

			if (FilePermissions != null && FilePermissions.AccessRules.All(x => x.IdentityReference != SelectedRuleForUser.UserGroup.Sid))
			{
				// No remaining rules, remove user from list
				FilePermissions.RulesForUsers.Remove(SelectedRuleForUser);
			}
		}

		public void GetFilePermissions()
		{
			if (Item == null)
				return;

			var permissions = FilePermissionHelpers.FromFilePath(Item.ItemPath, Item.PrimaryItemAttribute == StorageItemTypes.Folder && !Item.IsShortcut);

			if (permissions is not null)
				FilePermissions = new FilePermissionsManager(permissions);
		}

		public Task<bool> SetFilePermissions() =>
			Task.FromResult(FilePermissions != null && (!FilePermissions.CanReadFilePermissions || FilePermissions.SetPermissions()));

		private Task<bool> SetFileOwner(string ownerSid) => Task.FromResult(FilePermissions != null && FilePermissions.SetOwner(ownerSid));

		private Task<bool> SetAccessRuleProtection(bool isProtected, bool preserveInheritance) =>
			Task.FromResult(FilePermissions != null && FilePermissions.SetAccessRuleProtection(isProtected, preserveInheritance));

		private static Task<string?> OpenObjectPicker() =>
			FilePermissionHelpers.OpenObjectPicker(NativeWinApiHelper.CoreWindowHandle.ToInt64());
	}
}