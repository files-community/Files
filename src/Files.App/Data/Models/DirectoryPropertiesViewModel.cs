// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Items;
using Files.App.Utils.Storage;
using Files.Shared.Helpers;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;

namespace Files.App.ViewModels.UserControls
{
	public sealed partial class StatusBarViewModel : ObservableObject
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IDevToolsSettingsService DevToolsSettingsService = Ioc.Default.GetRequiredService<IDevToolsSettingsService>();
		private readonly IStorageArchiveService StorageArchiveService = Ioc.Default.GetRequiredService<IStorageArchiveService>();
		private CurrentInstanceViewModel? InstanceViewModel => ContentPageContext.ShellPage?.InstanceViewModel;

		// The first branch will always be the active one.
		public const int ACTIVE_BRANCH_INDEX = 0;

		private string? _gitRepositoryPath;

		private readonly ObservableCollection<BranchItem> _localBranches = [];

		private readonly ObservableCollection<BranchItem> _remoteBranches = [];

		public bool IsBranchesFlyoutExpanded { get; set; } = false;

		private string? _DirectoryItemCount;
		public string? DirectoryItemCount
		{
			get => _DirectoryItemCount;
			set => SetProperty(ref _DirectoryItemCount, value);
		}

		private string? _GitBranchDisplayName;
		public string? GitBranchDisplayName
		{
			get => _GitBranchDisplayName;
			private set => SetProperty(ref _GitBranchDisplayName, value);
		}

		private int _SelectedBranchIndex;
		public int SelectedBranchIndex
		{
			get => _SelectedBranchIndex;
			set
			{
				if (SetProperty(ref _SelectedBranchIndex, value) &&
					value != -1 &&
					(value != ACTIVE_BRANCH_INDEX || !_ShowLocals) &&
					value < Branches.Count)
				{
					CheckoutRequested?.Invoke(this, Branches[value].Name);
				}
			}
		}

		private bool _ShowLocals = true;
		public bool ShowLocals
		{
			get => _ShowLocals;
			set
			{
				if (SetProperty(ref _ShowLocals, value))
				{
					OnPropertyChanged(nameof(Branches));

					if (value)
						SelectedBranchIndex = ACTIVE_BRANCH_INDEX;
				}
			}
		}

		private string _StatusInfo = "0 / 0";
		public string StatusInfo
		{
			get => _StatusInfo;
			set => SetProperty(ref _StatusInfo, value);
		}

		private string _ExtendedStatusInfo = string.Format("CommitsNumber".GetLocalizedResource(), 0);
		public string ExtendedStatusInfo
		{
			get => _ExtendedStatusInfo;
			set => SetProperty(ref _ExtendedStatusInfo, value);
		}

		private bool _IsZipEncodingSelectorVisible;
		public bool IsZipEncodingSelectorVisible
		{
			get => _IsZipEncodingSelectorVisible;
			set => SetProperty(ref _IsZipEncodingSelectorVisible, value);
		}

		public List<EncodingItem> ZipEncodingOptions { get; } = EncodingItem.Defaults.ToList();

		private EncodingItem? _SelectedZipEncoding;
		public EncodingItem? SelectedZipEncoding
		{
			get => _SelectedZipEncoding;
			set
			{
				if (SetProperty(ref _SelectedZipEncoding, value) && value is not null)
					_ = OnZipEncodingChangedAsync(value);
			}
		}

		public bool ShowOpenInIDEButton
		{
			get
			{
				return DevToolsSettingsService.OpenInIDEOption == OpenInIDEOption.AllLocations ||
					   (DevToolsSettingsService.OpenInIDEOption == OpenInIDEOption.GitRepos && GitBranchDisplayName is not null);
			}
		}

		public ObservableCollection<BranchItem> Branches => _ShowLocals
			? _localBranches
			: _remoteBranches;

		public EventHandler<string>? CheckoutRequested;

		public ICommand NewBranchCommand { get; }

		public StatusBarViewModel()
		{
			NewBranchCommand = new AsyncRelayCommand(()
				=> GitHelpers.CreateNewBranchAsync(_gitRepositoryPath!, _localBranches[ACTIVE_BRANCH_INDEX].Name));

			DevToolsSettingsService.PropertyChanged += (s, e) =>
			{
				switch (e.PropertyName)
				{
					case nameof(DevToolsSettingsService.OpenInIDEOption):
						OnPropertyChanged(nameof(ShowOpenInIDEButton));
						break;
				}
			};

			SubscribeToShellPage();
			ContentPageContext.PropertyChanged += OnContentPageContextPropertyChanged;
		}

		private void OnContentPageContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ContentPageContext.ShellPage))
			{
				UnsubscribeFromInstanceViewModel();
				SubscribeToShellPage();
			}
		}

		private void SubscribeToShellPage()
		{
			SubscribeToInstanceViewModel();
			_ = UpdateZipEncodingStateAsync();
		}

		public void UpdateGitInfo(bool isGitRepository, string? repositoryPath, BranchItem? head)
		{
			GitBranchDisplayName =
				isGitRepository &&
				head is not null &&
				ContentPageContext.ShellPage is not null &&
				!ContentPageContext.ShellPage.InstanceViewModel.IsPageTypeSearchResults
					? head.Name
					: null;

			_gitRepositoryPath = repositoryPath;

			// Change ShowLocals value only if branches flyout is closed
			if (!IsBranchesFlyoutExpanded)
				ShowLocals = true;

			var behind = head is not null ? head.BehindBy ?? 0 : 0;
			var ahead = head is not null ? head.AheadBy ?? 0 : 0;

			ExtendedStatusInfo = string.Format(Strings.GitSyncStatusExtendedInfo.GetLocalizedResource(), ahead, behind);
			StatusInfo = $"{ahead} / {behind}";

			OnPropertyChanged(nameof(ShowOpenInIDEButton));
		}

		public async Task LoadBranches()
		{
			if (string.IsNullOrEmpty(_gitRepositoryPath))
				return;

			var branches = await GitHelpers.GetBranchNames(_gitRepositoryPath);

			_localBranches.Clear();
			_remoteBranches.Clear();

			foreach (var branch in branches)
			{
				if (branch.IsRemote)
					_remoteBranches.Add(branch);
				else
					_localBranches.Add(branch);
			}

			SelectedBranchIndex = ShowLocals ? ACTIVE_BRANCH_INDEX : -1;
		}

		public Task ExecuteDeleteBranch(string? branchName)
		{
			return GitHelpers.DeleteBranchAsync(_gitRepositoryPath, GitBranchDisplayName, branchName);
		}

		public async Task UpdateZipEncodingStateAsync()
		{
			if (ContentPageContext.ShellPage?.SlimContentPage?.StatusBarViewModel != this)
				return;

			var instanceVM = InstanceViewModel;
			if (instanceVM is null)
				return;

			if (!instanceVM.IsPageTypeZipFolder)
			{
				IsZipEncodingSelectorVisible = false;
				return;
			}

			var workingDir = ContentPageContext.ShellPage?.ShellViewModel.WorkingDirectory;
			if (string.IsNullOrEmpty(workingDir) || !ZipStorageFolder.IsZipPath(workingDir))
				return;

			if (TryRestoreZipEncodingFromContainerPath(workingDir))
				return;

			try
			{
				var isUndetermined = await StorageArchiveService.IsEncodingUndeterminedAsync(workingDir);
				instanceVM.IsZipEncodingUndetermined = isUndetermined;

				if (!isUndetermined)
				{
					IsZipEncodingSelectorVisible = false;
					return;
				}

				var detected = await StorageArchiveService.DetectEncodingAsync(workingDir);
				if (detected is not null)
				{
					instanceVM.ZipEncodingName = detected.WebName;
					EncodingItem? ZipEncodingItem = ZipEncodingOptions.FirstOrDefault(e =>
						e.Encoding?.WebName.Equals(detected.WebName, StringComparison.OrdinalIgnoreCase) == true);
					if(ZipEncodingItem == null)
					{
						ZipEncodingItem = new EncodingItem(detected, detected.EncodingName);
					}
				    ZipEncodingOptions.Add(ZipEncodingItem);
					SelectedZipEncoding = ZipEncodingItem;
				}
				else
				{
					instanceVM.ZipEncodingName = null;
					SelectedZipEncoding = ZipEncodingOptions.FirstOrDefault(e => e.Encoding is null);
				}

				IsZipEncodingSelectorVisible = true;
			}
			catch (Exception ex)
			{
				App.Logger.LogError(ex, "Error checking zip encoding.");
				IsZipEncodingSelectorVisible = false;
			}
		}

		private bool TryRestoreZipEncodingFromContainerPath(string workingDir)
		{
			if (!FileExtensionHelpers.IsBrowsableZipFile(workingDir, out var ext))
				return false;

			var marker = workingDir.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
			if (marker is -1)
				return false;

			var containerPath = workingDir.Substring(0, marker + ext.Length);
			if (!ZipStorageFolder.TryGetEncodingForContainerPath(containerPath, out var encoding))
				return false;

			EncodingItem? match = encoding is not null
				? ZipEncodingOptions.FirstOrDefault(e => e.Encoding == encoding)
				: ZipEncodingOptions.FirstOrDefault(e => e.Encoding is null);

			if (match is null && encoding is not null)
			{
				match = new EncodingItem(encoding, encoding.EncodingName);
				ZipEncodingOptions.Add(match);
			}

			if (match is not null)
			{
				SelectedZipEncoding = match;
				IsZipEncodingSelectorVisible = true;
				return true;
			}

			return false;
		}

		private async Task OnZipEncodingChangedAsync(EncodingItem encodingItem)
		{
			if (ContentPageContext.ShellPage is null)
				return;

			if (ContentPageContext.ShellPage?.SlimContentPage?.StatusBarViewModel != this)
				return;

			var workingDir = ContentPageContext.ShellPage.ShellViewModel.WorkingDirectory;
			if (string.IsNullOrEmpty(workingDir))
				return;

			if (FileExtensionHelpers.IsBrowsableZipFile(workingDir, out var ext))
			{
				var marker = workingDir.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
				if (marker is not -1)
				{
					var containerPath = workingDir.Substring(0, marker + ext.Length);
					ZipStorageFolder.SetEncodingForContainerPath(containerPath, encodingItem.Encoding);
				}
			}

			ContentPageContext.ShellPage.ShellViewModel.RefreshItems(null);
		}

		internal void SubscribeToInstanceViewModel()
		{
			var instanceVM = InstanceViewModel;
			if (instanceVM is not null)
				instanceVM.PropertyChanged += OnInstanceViewModelPropertyChanged;
		}

		internal void UnsubscribeFromInstanceViewModel()
		{
			var instanceVM = InstanceViewModel;
			if (instanceVM is not null)
				instanceVM.PropertyChanged -= OnInstanceViewModelPropertyChanged;
		}

		private void OnInstanceViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(CurrentInstanceViewModel.IsPageTypeZipFolder))
			{
				_ = UpdateZipEncodingStateAsync();
			}
		}
	}
}
