using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem.Security;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	public class SecurityAdvancedViewModel : ObservableObject
	{
		private readonly string _path;

		private readonly Window _window;

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

		private bool _IsFolder;
		public bool IsFolder
		{
			get => _IsFolder;
			set => SetProperty(ref _IsFolder, value);
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

		private GridLength _ColumnType = new(64d);
		public GridLength ColumnType
		{
			get => _ColumnType;
			set => SetProperty(ref _ColumnType, value);
		}

		private GridLength _ColumnPrincipal = new(200d);
		public GridLength ColumnPrincipal
		{
			get => _ColumnPrincipal;
			set => SetProperty(ref _ColumnPrincipal, value);
		}

		private GridLength _ColumnAccess = new(160d);
		public GridLength ColumnAccess
		{
			get => _ColumnAccess;
			set => SetProperty(ref _ColumnAccess, value);
		}

		private GridLength _ColumnInherited = new(70d);
		public GridLength ColumnInherited
		{
			get => _ColumnInherited;
			set => SetProperty(ref _ColumnInherited, value);
		}

		public IAsyncRelayCommand ChangeOwnerCommand { get; set; }
		public IAsyncRelayCommand AddAccessControlEntryCommand { get; set; }
		public IAsyncRelayCommand RemoveAccessControlEntryCommand { get; set; }

		public IRelayCommand DisableInheritanceCommand { get; set; }
		public IRelayCommand<string> SetDisableInheritanceOptionCommand { get; set; }
		public IRelayCommand ReplaceChildPermissionsCommand { get; set; }

		public SecurityAdvancedViewModel(ListedItem item, Window window)
		{
			IsFolder = item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsShortcut;
			_path = item.ItemPath;
			_window = window;

			InitializeCommands();
			GetAccessControlList();
		}

		public SecurityAdvancedViewModel(DriveItem item, Window window)
		{
			IsFolder = true;
			_path = item.Path;
			_window = window;

			InitializeCommands();
			GetAccessControlList();
		}

		private void InitializeCommands()
		{
			ChangeOwnerCommand = new AsyncRelayCommand(ExecuteChangeOwnerCommand, () => AccessControlList is not null);
			AddAccessControlEntryCommand = new AsyncRelayCommand(ExecuteAddAccessControlEntryCommand, () => AccessControlList is not null && AccessControlList.IsValid);
			RemoveAccessControlEntryCommand = new AsyncRelayCommand(ExecuteRemoveAccessControlEntryCommand, () => AccessControlList is not null && AccessControlList.IsValid && SelectedAccessControlEntry is not null);

			DisableInheritanceCommand = new RelayCommand(DisableInheritance, () => AccessControlList is not null && AccessControlList.IsValid && (AccessControlList.IsProtected != _isProtected));
			SetDisableInheritanceOptionCommand = new RelayCommand<string>(SetDisableInheritanceOption);
			ReplaceChildPermissionsCommand = new RelayCommand(ReplaceChildPermissions, () => AccessControlList is not null && AccessControlList.IsValid);
		}

		private async Task ExecuteChangeOwnerCommand()
		{
			var sid = await OpenObjectPicker();
			if (string.IsNullOrEmpty(sid))
				return;

			// Set owner and refresh file permissions
			FileOperationsHelpers.SetFileOwner(_path, sid);
		}

		private async Task ExecuteAddAccessControlEntryCommand()
		{
			// Pick an user or a group with Object Picker UI
			var sid = await OpenObjectPicker();
			if (string.IsNullOrEmpty(sid))
				return;

			// Add a new ACE to the ACL
			var ace = FileSecurityHelpers.InitializeDefaultAccessControlEntry(IsFolder, sid);
			AccessControlList.AccessControlEntries.Insert(0, ace);

			// Apply changes
			await App.Window.DispatcherQueue.EnqueueAsync(() =>
			{
				FileSecurityHelpers.AddAccessControlEntry(_path, sid);
			});
		}

		private async Task ExecuteRemoveAccessControlEntryCommand()
		{
			// TODO: Should show a dialog to notify that that entry couldn't removed
			if (SelectedAccessControlEntry.IsInherited)
				return;

			// Get index of the ACE
			var index = AccessControlList.AccessControlEntries.IndexOf(SelectedAccessControlEntry);

			// Remove the ACE
			AccessControlList.AccessControlEntries.Remove(SelectedAccessControlEntry);

			if (AccessControlList.AccessControlEntries.Count == 0)
				return;

			// Re-select item
			SelectedAccessControlEntry = AccessControlList.AccessControlEntries.First();

			// Apply changes
			await App.Window.DispatcherQueue.EnqueueAsync(() =>
			{
				FileSecurityHelpers.RemoveAccessControlEntry(_path, (uint)index);
			});
		}

		// --- TODO: Following methods are unimplemented ---

		private void DisableInheritance()
		{
			// Update protection status and refresh access control
			if (FileOperationsHelpers.SetAccessRuleProtection(_path, IsFolder, _isProtected, _preserveInheritance))
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
			AccessControlList = FileOperationsHelpers.GetFilePermissions(_path, IsFolder);
		}

		public bool SaveChangedAccessControlList()
		{
			return false;
		}

		public Task<string?> OpenObjectPicker()
			=> FileOperationsHelpers.OpenObjectPickerAsync(FilePropertiesHelpers.GetWindowHandle(_window).ToInt64());
	}
}
