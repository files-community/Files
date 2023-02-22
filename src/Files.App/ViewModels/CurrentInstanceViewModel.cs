using CommunityToolkit.Mvvm.ComponentModel;
using Files.Core.Enums;

namespace Files.App.ViewModels
{
	public class CurrentInstanceViewModel : ObservableObject
	{
		 // TODO:
		 //  In the future, we should consolidate these public variables into
		 //  a single enum property providing simplified customization of the
		 //  values being manipulated inside the setter blocks

		public FolderSettingsViewModel FolderSettings { get; }

		public CurrentInstanceViewModel()
		{
			FolderSettings = new FolderSettingsViewModel();
		}

		public CurrentInstanceViewModel(FolderLayoutModes rootLayoutMode)
		{
			FolderSettings = new FolderSettingsViewModel(rootLayoutMode);
		}

		private bool isPageTypeSearchResults = false;
		public bool IsPageTypeSearchResults
		{
			get => isPageTypeSearchResults;
			set
			{
				SetProperty(ref isPageTypeSearchResults, value);
				OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanPasteInPage));
				OnPropertyChanged(nameof(CanCopyPathInPage));
				OnPropertyChanged(nameof(ShowSearchUnindexedItemsMessage));
				OnPropertyChanged(nameof(CanShareInPage));
				OnPropertyChanged(nameof(CanTagFilesInPage));
			}
		}

		private string currentSearchQuery;
		public string CurrentSearchQuery
		{
			get => currentSearchQuery;
			set => SetProperty(ref currentSearchQuery, value);
		}

		private bool searchedUnindexedItems;
		public bool SearchedUnindexedItems
		{
			get => searchedUnindexedItems;
			set
			{
				if (SetProperty(ref searchedUnindexedItems, value))
				{
					OnPropertyChanged(nameof(ShowSearchUnindexedItemsMessage));
				}
			}
		}

		public bool ShowSearchUnindexedItemsMessage
		{
			get => !SearchedUnindexedItems && IsPageTypeSearchResults;
		}

		private bool isPageTypeNotHome = false;
		public bool IsPageTypeNotHome
		{
			get => isPageTypeNotHome;
			set
			{
				SetProperty(ref isPageTypeNotHome, value);
				OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanPasteInPage));
				OnPropertyChanged(nameof(CanCopyPathInPage));
				OnPropertyChanged(nameof(ShowSearchUnindexedItemsMessage));
				OnPropertyChanged(nameof(CanShareInPage));
				OnPropertyChanged(nameof(CanTagFilesInPage));
			}
		}

		private bool isPageTypeMtpDevice = false;
		public bool IsPageTypeMtpDevice
		{
			get => isPageTypeMtpDevice;
			set
			{
				SetProperty(ref isPageTypeMtpDevice, value);
				OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanPasteInPage));
				OnPropertyChanged(nameof(CanCopyPathInPage));
				OnPropertyChanged(nameof(ShowSearchUnindexedItemsMessage));
				OnPropertyChanged(nameof(CanShareInPage));
				OnPropertyChanged(nameof(CanTagFilesInPage));
			}
		}

		private bool isPageTypeRecycleBin = false;
		public bool IsPageTypeRecycleBin
		{
			get => isPageTypeRecycleBin;
			set
			{
				SetProperty(ref isPageTypeRecycleBin, value);
				OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanPasteInPage));
				OnPropertyChanged(nameof(CanCopyPathInPage));
				OnPropertyChanged(nameof(ShowSearchUnindexedItemsMessage));
				OnPropertyChanged(nameof(CanShareInPage));
				OnPropertyChanged(nameof(CanTagFilesInPage));
			}
		}

		private bool isPageTypeFtp = false;
		public bool IsPageTypeFtp
		{
			get => isPageTypeFtp;
			set
			{
				SetProperty(ref isPageTypeFtp, value);
				OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanPasteInPage));
				OnPropertyChanged(nameof(CanCopyPathInPage));
				OnPropertyChanged(nameof(ShowSearchUnindexedItemsMessage));
				OnPropertyChanged(nameof(CanShareInPage));
				OnPropertyChanged(nameof(CanTagFilesInPage));
			}
		}

		private bool isPageTypeCloudDrive = false;
		public bool IsPageTypeCloudDrive
		{
			get => isPageTypeCloudDrive;
			set
			{
				SetProperty(ref isPageTypeCloudDrive, value);
				OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanPasteInPage));
				OnPropertyChanged(nameof(CanCopyPathInPage));
				OnPropertyChanged(nameof(ShowSearchUnindexedItemsMessage));
				OnPropertyChanged(nameof(CanShareInPage));
				OnPropertyChanged(nameof(CanTagFilesInPage));
			}
		}

		private bool isPageTypeZipFolder = false;
		public bool IsPageTypeZipFolder
		{
			get => isPageTypeZipFolder;
			set
			{
				SetProperty(ref isPageTypeZipFolder, value);
				OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanPasteInPage));
				OnPropertyChanged(nameof(CanCopyPathInPage));
				OnPropertyChanged(nameof(ShowSearchUnindexedItemsMessage));
				OnPropertyChanged(nameof(CanShareInPage));
				OnPropertyChanged(nameof(CanTagFilesInPage));
			}
		}

		private bool isPageTypeLibrary = false;
		public bool IsPageTypeLibrary
		{
			get => isPageTypeLibrary;
			set
			{
				SetProperty(ref isPageTypeLibrary, value);
				OnPropertyChanged(nameof(IsCreateButtonEnabledInPage));
				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanPasteInPage));
				OnPropertyChanged(nameof(CanCopyPathInPage));
				OnPropertyChanged(nameof(ShowSearchUnindexedItemsMessage));
				OnPropertyChanged(nameof(CanShareInPage));
				OnPropertyChanged(nameof(CanTagFilesInPage));
			}
		}

		public bool IsCreateButtonEnabledInPage
		{
			get => !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeSearchResults;
		}

		public bool CanCopyPathInPage
		{
			get => !isPageTypeMtpDevice && !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeSearchResults;
		}

		public bool CanCreateFileInPage
		{
			get => !isPageTypeMtpDevice && !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeSearchResults && !isPageTypeFtp && !isPageTypeZipFolder;
		}

		public bool CanPasteInPage
		{
			get => !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeSearchResults;
		}

		public bool CanShareInPage
		{
			get => !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeFtp && !isPageTypeZipFolder;
		}

		public bool CanTagFilesInPage
		{
			get => !isPageTypeRecycleBin && !isPageTypeFtp && !isPageTypeZipFolder;
		}
	}
}
