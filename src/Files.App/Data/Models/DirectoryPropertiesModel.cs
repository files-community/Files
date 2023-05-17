// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Models
{
	public class DirectoryPropertiesModel : ObservableObject
	{
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
				if (SetProperty(ref _SelectedBranchIndex, value) && value != -1 && value != ActiveBranchIndex)
					CheckoutRequested?.Invoke(this, BranchesNames[value]);
			}
		}

		public int ActiveBranchIndex { get; private set; }

		public ObservableCollection<string> BranchesNames { get; } = new();

		public EventHandler<string>? CheckoutRequested;

		public void UpdateGitInfo(bool isGitRepository, string activeBranch, string[] branches)
		{
			GitBranchDisplayName = isGitRepository
				? string.Format("Branch".GetLocalizedResource(), activeBranch)
				: null;

			if (isGitRepository)
			{
				BranchesNames.Clear();
				foreach (var name in branches)
					BranchesNames.Add(name);

				ActiveBranchIndex = BranchesNames.IndexOf(activeBranch);
				SelectedBranchIndex = ActiveBranchIndex;
			}
		}
	}
}
