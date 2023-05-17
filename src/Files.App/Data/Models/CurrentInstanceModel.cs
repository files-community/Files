// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using LibGit2Sharp;

namespace Files.App.Data.Models
{
	/// <summary>
	/// Represents a model for the current instance.
	/// </summary>
	/// <remarks>
	/// TODO: In the future, we should consolidate these public variables into
	/// a single enum property providing simplified customization of the
	/// values being manipulated inside the setter blocks.
	/// </remarks>
	public class CurrentInstanceModel : ObservableObject
	{
		public FolderSettingsService FolderSettings { get; }

		private bool isPageTypeSearchResults = false;
		public bool IsPageTypeSearchResults
		{
			get => isPageTypeSearchResults;
			set
			{
				SetProperty(ref isPageTypeSearchResults, value);

				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanCopyPathInPage));
				OnPropertyChanged(nameof(ShowSearchUnindexedItemsMessage));
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
			=> !SearchedUnindexedItems && IsPageTypeSearchResults;

		private bool isPageTypeNotHome;
		public bool IsPageTypeNotHome
		{
			get => isPageTypeNotHome;
			set
			{
				SetProperty(ref isPageTypeNotHome, value);
				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanCopyPathInPage));
			}
		}

		private bool isPageTypeMtpDevice;
		public bool IsPageTypeMtpDevice
		{
			get => isPageTypeMtpDevice;
			set
			{
				SetProperty(ref isPageTypeMtpDevice, value);

				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanCopyPathInPage));
			}
		}

		private bool isPageTypeRecycleBin;
		public bool IsPageTypeRecycleBin
		{
			get => isPageTypeRecycleBin;
			set
			{
				SetProperty(ref isPageTypeRecycleBin, value);

				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanCopyPathInPage));
				OnPropertyChanged(nameof(CanTagFilesInPage));
			}
		}

		private bool isPageTypeFtp;
		public bool IsPageTypeFtp
		{
			get => isPageTypeFtp;
			set
			{
				SetProperty(ref isPageTypeFtp, value);

				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanTagFilesInPage));
			}
		}

		private bool isPageTypeCloudDrive;
		public bool IsPageTypeCloudDrive
		{
			get => isPageTypeCloudDrive;
			set => SetProperty(ref isPageTypeCloudDrive, value);
		}

		private bool isPageTypeZipFolder;
		public bool IsPageTypeZipFolder
		{
			get => isPageTypeZipFolder;
			set
			{
				SetProperty(ref isPageTypeZipFolder, value);

				OnPropertyChanged(nameof(CanCreateFileInPage));
				OnPropertyChanged(nameof(CanTagFilesInPage));
			}
		}

		private bool isPageTypeLibrary;
		public bool IsPageTypeLibrary
		{
			get => isPageTypeLibrary;
			set => SetProperty(ref isPageTypeLibrary, value);
		}

		public bool CanCopyPathInPage
			=> !isPageTypeMtpDevice && !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeSearchResults;

		public bool CanCreateFileInPage
			=> !isPageTypeMtpDevice && !isPageTypeRecycleBin && isPageTypeNotHome && !isPageTypeSearchResults && !isPageTypeFtp && !isPageTypeZipFolder;

		public bool CanTagFilesInPage
			=> !isPageTypeRecycleBin && !isPageTypeFtp && !isPageTypeZipFolder;

		public bool IsGitRepository
			=> !string.IsNullOrWhiteSpace(gitRepositoryPath);

		private string? gitRepositoryPath;
		public string? GitRepositoryPath
		{
			get => gitRepositoryPath;
			set
			{
				if (SetProperty(ref gitRepositoryPath, value))
				{
					OnPropertyChanged(nameof(IsGitRepository));
					OnPropertyChanged(nameof(GitBranchName));
				}
			}
		}

		public string GitBranchName
		{
			get
			{
				if (IsGitRepository)
				{
					using var repository = new Repository(gitRepositoryPath);
					return repository.Branches.FirstOrDefault(branch =>
						branch.IsCurrentRepositoryHead)?.FriendlyName ?? string.Empty;
				}

				return string.Empty;
			}
		}

		public CurrentInstanceModel()
		{
			FolderSettings = new();
		}

		public CurrentInstanceModel(FolderLayoutModes rootLayoutMode)
		{
			FolderSettings = new(rootLayoutMode);
		}

		public void UpdateCurrentBranchName()
		{
			OnPropertyChanged(nameof(GitBranchName));
		}
	}
}
