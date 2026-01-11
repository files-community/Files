// Copyright (c) Files Community
// Licensed under the MIT License.

using LibGit2Sharp;
using System.ComponentModel;

namespace Files.App.Utils.Git
{
	internal interface IVersionControl
	{
		string? GetGitRepositoryPath(string? path, string root);
		string GetOriginRepositoryName(string? path);

		Task<BranchItem[]> GetBranchesNames(string? path);
		Task<BranchItem?> GetRepositoryHead(string? path);

		Task<bool> Checkout(string? repositoryPath, string? branch);
		Task CreateNewBranchAsync(string repositoryPath, string activeBranch);
		Task DeleteBranchAsync(string? repositoryPath, string? activeBranch, string? branchToDelete);

		bool ValidateBranchNameForRepository(string branchName, string repositoryPath);

		void FetchOrigin(string? repositoryPath, CancellationToken cancellationToken = default);
		Task PullOriginAsync(string? repositoryPath);
		Task PushToOriginAsync(string? repositoryPath, string? branchName);

		Task RequireGitAuthenticationAsync();

		bool IsRepositoryEx(string path, out string repoRootPath);
		GitItemModel GetGitInformationForItem(Repository repository, string path, bool getStatus = true, bool getCommit = true);

		void RemoveSavedCredentials();
		string GetSavedCredentials();

		Task InitializeRepositoryAsync(string? path);

		(string RepoUrl, string RepoName) GetRepoInfo(string url);
		bool IsValidRepoUrl(string url);
		Task CloneRepoAsync(string repoUrl, string repoName, string targetDirectory);

		event PropertyChangedEventHandler? IsExecutingGitActionChanged;
		event EventHandler? GitFetchCompleted;

		bool IsExecutingGitAction { get; }
	}
}
