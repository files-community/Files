// Copyright (c) Files Community
// Licensed under the MIT License.

using LibGit2Sharp;
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

		private string repoUrl;
		public string RepoUrl
		{
			get => repoUrl;
			set
			{
				if (SetProperty(ref repoUrl, value))
					OnPropertyChanged(nameof(CanCloneRepo));
			}
		}

		private string repoName;
		public string RepoName
		{
			get => repoName;
			set => SetProperty(ref repoName, value);

		}

		private string targetDirectory;
		public string TargetDirectory
		{
			get => targetDirectory;
			set => SetProperty(ref targetDirectory, value);

		}

		public ICommand CloneRepoCommand { get; private set; }

		public CloneRepoDialogViewModel(string repoUrl, string workingDirectory)
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

			TargetDirectory = Path.Combine(workingDirectory, RepoName);

			CloneRepoCommand = new AsyncRelayCommand(DoCloneRepo);
		}

		private async Task DoCloneRepo()
		{
			await GitHelpers.CloneRepoAsync(RepoUrl, repoName, targetDirectory);
		}
	}
}
