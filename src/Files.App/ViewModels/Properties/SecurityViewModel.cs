using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem.Security;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	public class SecurityViewModel : ObservableObject
	{
		private readonly string _path;

		private readonly Window _window;

		public string SelectedItemHeaderText
			=> string.Format("SecurityPermissionsHeaderText".GetLocalizedResource(), SelectedAccessControlEntry.Principal.DisplayName);

		public readonly bool _isFolder;

		private bool _IsAddAccessControlEntryButtonEnabled;
		public bool IsAddAccessControlEntryButtonEnabled
		{
			get => _IsAddAccessControlEntryButtonEnabled;
			set => SetProperty(ref _IsAddAccessControlEntryButtonEnabled, value);
		}

		private bool _IsDeleteAccessControlEntryButtonEnabled;
		public bool IsDeleteAccessControlEntryButtonEnabled
		{
			get => _IsDeleteAccessControlEntryButtonEnabled;
			set => SetProperty(ref _IsDeleteAccessControlEntryButtonEnabled, value);
		}

		private AccessControlList _AccessControlList;
		public AccessControlList AccessControlList
		{
			get => _AccessControlList;
			set => SetProperty(ref _AccessControlList, value);
		}

		private AccessControlEntry _SelectedAccessControlEntry;
		public AccessControlEntry SelectedAccessControlEntry
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
					RemoveAccessControlEntryCommand?.NotifyCanExecuteChanged();
					OnPropertyChanged(nameof(SelectedItemHeaderText));
				}
			}
		}

		public IAsyncRelayCommand AddAccessControlEntryCommand { get; set; }
		public IAsyncRelayCommand RemoveAccessControlEntryCommand { get; set; }

		public SecurityViewModel(ListedItem item, Window window)
		{
			_isFolder = item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsShortcut;
			_path = item.ItemPath;
			AccessControlList = FileSecurityHelpers.GetAccessControlList(_path, _isFolder);
			SelectedAccessControlEntry = AccessControlList.AccessControlEntries.FirstOrDefault();
			_window = window;

			AddAccessControlEntryCommand = new AsyncRelayCommand(ExecuteAddAccessControlEntryCommand, () => AccessControlList is not null && AccessControlList.IsValid);
			RemoveAccessControlEntryCommand = new AsyncRelayCommand(ExecuteRemoveAccessControlEntryCommand, () => AccessControlList is not null && AccessControlList.IsValid && SelectedAccessControlEntry is not null);
		}

		public SecurityViewModel(DriveItem item, Window window)
		{
			_isFolder = true;
			_path = item.Path;
			AccessControlList = FileSecurityHelpers.GetAccessControlList(_path, _isFolder);
			SelectedAccessControlEntry = AccessControlList.AccessControlEntries.FirstOrDefault();
			_window = window;

			AddAccessControlEntryCommand = new AsyncRelayCommand(ExecuteAddAccessControlEntryCommand, () => AccessControlList is not null && AccessControlList.IsValid);
			RemoveAccessControlEntryCommand = new AsyncRelayCommand(ExecuteRemoveAccessControlEntryCommand, () => AccessControlList is not null && AccessControlList.IsValid && SelectedAccessControlEntry is not null);
		}

		private async Task ExecuteAddAccessControlEntryCommand()
		{
			// Pick an user or a group with Object Picker UI
			var sid = await FileOperationsHelpers.OpenObjectPickerAsync(FilePropertiesHelpers.GetWindowHandle(_window).ToInt64());
			if (string.IsNullOrEmpty(sid))
				return;

			// Add a new ACE to the ACL
			var ace = FileSecurityHelpers.InitializeDefaultAccessControlEntry(_isFolder, sid);
			AccessControlList.AccessControlEntries.Insert(0, ace);

			// Apply changes
			await App.Window.DispatcherQueue.EnqueueAsync(() =>
			{
				var win32Result = FileSecurityHelpers.AddAccessControlEntry(_path, sid);
			});
		}

		private async Task ExecuteRemoveAccessControlEntryCommand()
		{
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
				var win32Result = FileSecurityHelpers.RemoveAccessControlEntry(_path, (uint)index);
			});
		}
	}
}
