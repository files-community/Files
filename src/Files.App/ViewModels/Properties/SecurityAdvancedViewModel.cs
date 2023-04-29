// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem.Security;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Vanara.PInvoke;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	public class SecurityAdvancedViewModel : ObservableObject
	{
		private readonly PropertiesPageNavigationParameter _navigationParameter;

		private readonly Window _window;

		private readonly string _path;

		private readonly bool _isFolder;

		public bool DisplayElements { get; private set; }

		public string ErrorMessage { get; private set; }

		public bool IsAddAccessControlEntryButtonEnabled =>
			AccessControlList is not null &&
			AccessControlList.IsValid;

		public bool IsDeleteAccessControlEntryButtonEnabled =>
			AccessControlList is not null &&
			AccessControlList.IsValid &&
			SelectedAccessControlEntry is not null &&
			SelectedAccessControlEntry.IsInherited is false;

		public IconFileInfo ShieldIconFileInfo { get; private set; }

		public bool CurrentUserIsInAdministratorsGroup { get; private set; }

		public string DisableInheritanceOption
		{
			get
			{
				//if (!_isProtected)
				//	return "SecurityAdvancedInheritedEnable/Text".GetLocalizedResource();
				//else if (_preserveInheritance)
				//	return "SecurityAdvancedInheritedConvert/Text".GetLocalizedResource();
				//else
				//	return "SecurityAdvancedInheritedRemove/Text".GetLocalizedResource();

				return string.Empty;
			}
		}

		private AccessControlList _AccessControlList;
		public AccessControlList AccessControlList
		{
			get => _AccessControlList;
			set
			{
				if (SetProperty(ref _AccessControlList, value))
				{
					ChangeOwnerCommand.NotifyCanExecuteChanged();
					AddAccessControlEntryCommand.NotifyCanExecuteChanged();
					RemoveAccessControlEntryCommand.NotifyCanExecuteChanged();
					DisableInheritanceCommand.NotifyCanExecuteChanged();
					ReplaceChildPermissionsCommand.NotifyCanExecuteChanged();
				}
			}
		}

		private AccessControlEntry? _SelectedAccessControlEntry;
		public AccessControlEntry? SelectedAccessControlEntry
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

					OnPropertyChanged(nameof(IsDeleteAccessControlEntryButtonEnabled));
				}
			}
		}

		private GridLength _ColumnType = new(64d);
		public GridLength ColumnTypeGridLength
		{
			get => _ColumnType;
			set => SetProperty(ref _ColumnType, value);
		}

		private GridLength _ColumnPrincipal = new(200d);
		public GridLength ColumnPrincipalGridLength
		{
			get => _ColumnPrincipal;
			set => SetProperty(ref _ColumnPrincipal, value);
		}

		private GridLength _ColumnAccess = new(160d);
		public GridLength ColumnAccessGridLength
		{
			get => _ColumnAccess;
			set => SetProperty(ref _ColumnAccess, value);
		}

		private GridLength _ColumnInherited = new(70d);
		public GridLength ColumnInheritedGridLength
		{
			get => _ColumnInherited;
			set => SetProperty(ref _ColumnInherited, value);
		}

		public IAsyncRelayCommand ChangeOwnerCommand { get; set; }
		public IAsyncRelayCommand AddAccessControlEntryCommand { get; set; }
		public IAsyncRelayCommand RemoveAccessControlEntryCommand { get; set; }

		// --- TODO: Following commands are unimplemented ---

		public IRelayCommand DisableInheritanceCommand { get; set; }
		public IRelayCommand<string> SetDisableInheritanceOptionCommand { get; set; }
		public IRelayCommand ReplaceChildPermissionsCommand { get; set; }

		public SecurityAdvancedViewModel(PropertiesPageNavigationParameter parameter)
		{
			_navigationParameter = parameter;
			_window = parameter.Window;

			switch (parameter.Parameter)
			{
				case ListedItem listedItem:
					_path = listedItem.ItemPath;
					_isFolder = listedItem.PrimaryItemAttribute == StorageItemTypes.Folder && !listedItem.IsShortcut;
					break;
				case DriveItem driveItem:
					_path = driveItem.Path;
					_isFolder = true;
					break;
				default:
					var defaultlistedItem = (ListedItem)parameter.Parameter;
					_path = defaultlistedItem.ItemPath;
					_isFolder = defaultlistedItem.PrimaryItemAttribute == StorageItemTypes.Folder && !defaultlistedItem.IsShortcut;
					break;
			};

			ShieldIconFileInfo = LoadShieldIconResource();

			var error = FileSecurityHelpers.GetAccessControlList(_path, _isFolder, out _AccessControlList);
			SelectedAccessControlEntry = AccessControlList.AccessControlEntries.FirstOrDefault();
			CurrentUserIsInAdministratorsGroup = Shell32.IsUserAnAdmin() == false;

			if (!AccessControlList.IsValid)
			{
				DisplayElements = false;

				if (error == Win32Error.ERROR_ACCESS_DENIED)
				{
					ErrorMessage = "You must have Read permissions to view the properties of this object.";

					if (CurrentUserIsInAdministratorsGroup)
						ErrorMessage += ("\n\n" + " Click Continue to attempt the operation with administrative permissions.");
				}
				else
				{
					ErrorMessage = "Unable to display permissions for one or more errors";
				}
			}
			else
			{
				DisplayElements = true;
				ErrorMessage = string.Empty;
			}

			OnPropertyChanged(nameof(DisplayElements));
			OnPropertyChanged(nameof(ErrorMessage));

			ChangeOwnerCommand = new AsyncRelayCommand(ExecuteChangeOwnerCommand);
			AddAccessControlEntryCommand = new AsyncRelayCommand(ExecuteAddAccessControlEntryCommand);
			RemoveAccessControlEntryCommand = new AsyncRelayCommand(ExecuteRemoveAccessControlEntryCommand);
			DisableInheritanceCommand = new RelayCommand(DisableInheritance);
			SetDisableInheritanceOptionCommand = new RelayCommand<string>(SetDisableInheritanceOption);
			ReplaceChildPermissionsCommand = new RelayCommand(ReplaceChildPermissions, () => AccessControlList is not null && AccessControlList.IsValid);
		}

		private IconFileInfo LoadShieldIconResource()
		{
			string imageres = System.IO.Path.Combine(CommonPaths.SystemRootPath, "System32", "imageres.dll");
			var imageResList = Shell.Win32API.ExtractSelectedIconsFromDLL(
				imageres,
				new List<int>() { Constants.ImageRes.ShieldIcon },
				16);

			return imageResList.First();
		}

		private async Task ExecuteChangeOwnerCommand()
		{
			var sid = await FileOperationsHelpers.OpenObjectPickerAsync(FilePropertiesHelpers.GetWindowHandle(_window).ToInt64());
			if (string.IsNullOrEmpty(sid))
				return;

			// Set owner and refresh file permissions
			FileSecurityHelpers.SetOwner(_path, sid);
		}

		private async Task ExecuteAddAccessControlEntryCommand()
		{
			// Pick an user or a group with Object Picker UI
			var sid = await FileOperationsHelpers.OpenObjectPickerAsync(FilePropertiesHelpers.GetWindowHandle(_window).ToInt64());
			if (string.IsNullOrEmpty(sid))
				return;

			await App.Window.DispatcherQueue.EnqueueAsync(() =>
			{
				// Run Win32API
				var win32Result = FileSecurityHelpers.AddAccessControlEntry(_path, sid);

				// Add a new ACE to the ACL
				var ace = FileSecurityHelpers.InitializeDefaultAccessControlEntry(_isFolder, sid);
				AccessControlList.AccessControlEntries.Insert(0, ace);
			});
		}

		private async Task ExecuteRemoveAccessControlEntryCommand()
		{
			await App.Window.DispatcherQueue.EnqueueAsync(() =>
			{
				// Get index of the ACE
				var index = AccessControlList.AccessControlEntries.IndexOf(SelectedAccessControlEntry);

				// Run Win32API
				var win32Result = FileSecurityHelpers.RemoveAccessControlEntry(_path, (uint)index);

				// Remove the ACE
				AccessControlList.AccessControlEntries.Remove(SelectedAccessControlEntry);

				if (AccessControlList.AccessControlEntries.Count == 0)
					return;

				// Re-select item
				SelectedAccessControlEntry = AccessControlList.AccessControlEntries.First();
			});
		}

		// --- TODO: Following methods are unimplemented ---

		private void DisableInheritance()
		{
			// Update protection status and refresh access control
			//if (FileOperationsHelpers.SetAccessRuleProtection(_path, _isFolder, _isProtected, _preserveInheritance))
			//	GetAccessControlList();
		}

		private void SetDisableInheritanceOption(string? options)
		{
			//_isProtected = bool.Parse(options.Split(',')[0]);
			//_preserveInheritance = bool.Parse(options.Split(',')[1]);

			//OnPropertyChanged(nameof(DisableInheritanceOption));
			//DisableInheritanceCommand.NotifyCanExecuteChanged();
		}

		private void ReplaceChildPermissions()
		{
		}
	}
}
