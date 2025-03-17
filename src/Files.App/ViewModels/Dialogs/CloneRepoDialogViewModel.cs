// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Windows.Input;

namespace Files.App.ViewModels.Dialogs
{
	public sealed partial class CloneRepoDialogViewModel : ObservableObject
	{
		public bool CanCloneRepo
		{
			get
			{
				var repoInfo = GitHelpers.GetRepoInfo(RepoUrl);
				if (!string.IsNullOrEmpty(repoInfo.RepoUrl))
				{
					RepoName = repoInfo.RepoName;
					return true;
				}

				return false;
			}
		}

		private string repoUrl = string.Empty;
		public string RepoUrl
		{
			get => repoUrl;
			set
			{
				if (SetProperty(ref repoUrl, value))
					OnPropertyChanged(nameof(CanCloneRepo));
			}
		}

		private string repoName = string.Empty;
		public string RepoName
		{
			get => repoName;
			set => SetProperty(ref repoName, value);

		}

		private string workingDirectory = string.Empty;
		public string WorkingDirectory
		{
			get => workingDirectory;
			set => SetProperty(ref workingDirectory, value);

		}

		public ICommand CloneRepoCommand { get; private set; }

		public CloneRepoDialogViewModel(string repoUrl, string directory)
		{
			var repoInfo = GitHelpers.GetRepoInfo(repoUrl);

			if (!string.IsNullOrEmpty(repoInfo.RepoName))
			{
				RepoUrl = repoInfo.RepoUrl;
				RepoName = repoInfo.RepoName;
			}
			else
			{
				RepoUrl = string.Empty;
				RepoName = string.Empty;
			}

			WorkingDirectory = directory;

			CloneRepoCommand = new AsyncRelayCommand(DoCloneRepo);
		}

		private async Task DoCloneRepo()
		{
			var targetDirectory = Path.Combine(workingDirectory, RepoName);
			await GitHelpers.CloneRepoAsync(RepoUrl, repoName, targetDirectory);
		}
	}
}
