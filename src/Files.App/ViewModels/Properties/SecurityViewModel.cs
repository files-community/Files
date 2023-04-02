using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.Filesystem.Security;
using Files.App.Helpers;
using Files.App.Views.Properties;
using Microsoft.UI.Xaml.Controls;
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

		private bool _isFolder;
		public bool IsFolder
		{
			get => _isFolder;
			set => SetProperty(ref _isFolder, value);
		}

		private AccessControlList _accessControlList;
		public AccessControlList AccessControlList
		{
			get => _accessControlList;
			set
			{
				if (SetProperty(ref _accessControlList, value))
				{
					AddAccessControlEntryCommand.NotifyCanExecuteChanged();
					RemoveAccessControlEntryCommand.NotifyCanExecuteChanged();
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

		public RelayCommand AddAccessControlEntryCommand { get; set; }
		public RelayCommand RemoveAccessControlEntryCommand { get; set; }

		private void InitializeCommands()
		{
			AddAccessControlEntryCommand = new RelayCommand(AddAccessControlEntry, () => AccessControlList is not null && AccessControlList.CanReadAccessControl);
			RemoveAccessControlEntryCommand = new RelayCommand(RemoveAccessControlEntry, () => AccessControlList is not null && AccessControlList.CanReadAccessControl && SelectedAccessControlEntry is not null);
			OpenSecurityAdvancedPageCommand = new RelayCommand<Frame>(OpenSecurityAdvancedPage);
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

		public void GetAccessControlList()
		{
			AccessControlList = FileOperationsHelpers.GetFilePermissions(Item.ItemPath, IsFolder);
		}

		public bool SaveChangedAccessControlList()
		{
			return AccessControlList.SetAccessControl();
		}

		private static Task<string?> OpenObjectPicker()
		{
			return FileOperationsHelpers.OpenObjectPickerAsync(NativeWinApiHelper.CoreWindowHandle.ToInt64());
		}
	}
}
