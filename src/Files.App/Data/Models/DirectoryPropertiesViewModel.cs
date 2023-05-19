// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Windows.Input;

namespace Files.App.Data.Models
{
	public class DirectoryPropertiesViewModel : ObservableObject
	{
		// The first branch will always be the active one.
		public const int ACTIVE_BRANCH_INDEX = 0;

		private string? gitRepositoryPath;

		private readonly ObservableCollection<string> localBranches = new();

		private readonly ObservableCollection<string> remoteBranches = new();

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
					ShowLocals = true;
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

		public ObservableCollection<string> BranchesNames => _ShowLocals 
			? localBranches 
			: remoteBranches;

		public EventHandler<string>? CheckoutRequested;

		public ICommand NewBranchCommand { get; }

		public DirectoryPropertiesViewModel()
		{
			NewBranchCommand = new AsyncRelayCommand(() 
				=> GitHelpers.CreateNewBranch(gitRepositoryPath!, localBranches[ACTIVE_BRANCH_INDEX]));
		}

		public void UpdateGitInfo(bool isGitRepository, string? repositoryPath, BranchItem[] branches)
		{
			GitBranchDisplayName = isGitRepository && branches.Any()
				? string.Format("Branch".GetLocalizedResource(), branches[ACTIVE_BRANCH_INDEX].Name)
				: null;

			gitRepositoryPath = repositoryPath;

			if (isGitRepository)
			{
				localBranches.Clear();
				remoteBranches.Clear();

				foreach (var branch in branches)
				{
					if (branch.IsRemote)
						remoteBranches.Add(branch.Name);
					else
						localBranches.Add(branch.Name);
				}

				SelectedBranchIndex = ACTIVE_BRANCH_INDEX;
			}
		}
	}
}
