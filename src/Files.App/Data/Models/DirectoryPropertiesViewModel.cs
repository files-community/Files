// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.Data.Models
{
	public class DirectoryPropertiesViewModel : ObservableObject
	{
		// The first branch will always be the active one.
		public const int ACTIVE_BRANCH_INDEX = 0;

		private string? _gitRepositoryPath;

		private readonly ObservableCollection<string> _localBranches = new();

		private readonly ObservableCollection<string> _remoteBranches = new();

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
					(value != ACTIVE_BRANCH_INDEX || !_ShowLocals))
				{
					CheckoutRequested?.Invoke(this, BranchesNames[value]);
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
					OnPropertyChanged(nameof(BranchesNames));

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

		public ObservableCollection<string> BranchesNames => _ShowLocals 
			? _localBranches 
			: _remoteBranches;

		public EventHandler<string>? CheckoutRequested;

		public ICommand NewBranchCommand { get; }

		public DirectoryPropertiesViewModel()
		{
			NewBranchCommand = new AsyncRelayCommand(() 
				=> GitHelpers.CreateNewBranch(_gitRepositoryPath!, _localBranches[ACTIVE_BRANCH_INDEX]));
		}

		public void UpdateGitInfo(bool isGitRepository, string? repositoryPath, BranchItem[] branches)
		{
			GitBranchDisplayName = isGitRepository && branches.Any()
				? branches[ACTIVE_BRANCH_INDEX].Name
				: null;

			_gitRepositoryPath = repositoryPath;
			ShowLocals = true;

			var behind = branches.Any() ? branches[0].BehindBy ?? 0 : 0;
			var ahead = branches.Any() ? branches[0].AheadBy ?? 0 : 0;

			ExtendedStatusInfo = string.Format("GitSyncStatusExtendedInfo".GetLocalizedResource(), ahead, behind);
			StatusInfo = $"{ahead} / {behind}";

			if (isGitRepository)
			{
				_localBranches.Clear();
				_remoteBranches.Clear();

				foreach (var branch in branches)
				{
					if (branch.IsRemote)
						_remoteBranches.Add(branch.Name);
					else
						_localBranches.Add(branch.Name);
				}

				SelectedBranchIndex = ACTIVE_BRANCH_INDEX;
			}
		}
	}
}
