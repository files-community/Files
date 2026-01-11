// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Files.App.Utils.Git
{
	internal static partial class GitHelpers
	{
		private static readonly IVersionControl _impl = new LibGit2();

		public static event PropertyChangedEventHandler? IsExecutingGitActionChanged
		{
			add => _impl.IsExecutingGitActionChanged += value;
			remove => _impl.IsExecutingGitActionChanged -= value;
		}

		public static event EventHandler? GitFetchCompleted
		{
			add => _impl.GitFetchCompleted += value;
			remove => _impl.GitFetchCompleted -= value;
		}

		public static bool IsExecutingGitAction => _impl.IsExecutingGitAction;

		public static string? GetGitRepositoryPath(string? path, string root) => _impl.GetGitRepositoryPath(path, root);
		public static string GetOriginRepositoryName(string? path) => _impl.GetOriginRepositoryName(path);
		public static Task<BranchItem[]> GetBranchesNames(string? path) => _impl.GetBranchesNames(path);
		public static Task<BranchItem?> GetRepositoryHead(string? path) => _impl.GetRepositoryHead(path);
		public static Task<bool> Checkout(string? repositoryPath, string? branch) => _impl.Checkout(repositoryPath, branch);
		public static Task CreateNewBranchAsync(string repositoryPath, string activeBranch) => _impl.CreateNewBranchAsync(repositoryPath, activeBranch);
		public static Task DeleteBranchAsync(string? repositoryPath, string? activeBranch, string? branchToDelete) => _impl.DeleteBranchAsync(repositoryPath, activeBranch, branchToDelete);
		public static bool ValidateBranchNameForRepository(string branchName, string repositoryPath) => _impl.ValidateBranchNameForRepository(branchName, repositoryPath);
		public static void FetchOrigin(string? repositoryPath, CancellationToken cancellationToken = default) => _impl.FetchOrigin(repositoryPath, cancellationToken);
		public static Task PullOriginAsync(string? repositoryPath) => _impl.PullOriginAsync(repositoryPath);
		public static Task PushToOriginAsync(string? repositoryPath, string? branchName) => _impl.PushToOriginAsync(repositoryPath, branchName);
		public static Task RequireGitAuthenticationAsync() => _impl.RequireGitAuthenticationAsync();
		public static bool IsRepositoryEx(string path, out string repoRootPath) => _impl.IsRepositoryEx(path, out repoRootPath);
		public static GitItemModel GetGitInformationForItem(Repository repository, string path, bool getStatus = true, bool getCommit = true) => _impl.GetGitInformationForItem(repository, path, getStatus, getCommit);
		public static void RemoveSavedCredentials() => _impl.RemoveSavedCredentials();
		public static string GetSavedCredentials() => _impl.GetSavedCredentials();
		public static Task InitializeRepositoryAsync(string? path) => _impl.InitializeRepositoryAsync(path);
		public static (string RepoUrl, string RepoName) GetRepoInfo(string url) => _impl.GetRepoInfo(url);
		public static bool IsValidRepoUrl(string url) => _impl.IsValidRepoUrl(url);
		public static Task CloneRepoAsync(string repoUrl, string repoName, string targetDirectory) => _impl.CloneRepoAsync(repoUrl, repoName, targetDirectory);
	}
}
