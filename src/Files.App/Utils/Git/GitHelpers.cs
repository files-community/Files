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

		/// <inheritdoc cref="IVersionControl.IsExecutingGitActionChanged"/>
		public static event PropertyChangedEventHandler? IsExecutingGitActionChanged
		{
			add => _impl.IsExecutingGitActionChanged += value;
			remove => _impl.IsExecutingGitActionChanged -= value;
		}

		/// <inheritdoc cref="IVersionControl.GitFetchCompleted"/>
		public static event EventHandler? GitFetchCompleted
		{
			add => _impl.GitFetchCompleted += value;
			remove => _impl.GitFetchCompleted -= value;
		}

		/// <inheritdoc cref="IVersionControl.IsExecutingGitAction"/>
		public static bool IsExecutingGitAction => _impl.IsExecutingGitAction;

		/// <inheritdoc cref="IVersionControl.GetGitRepositoryPath(string?,string)"/>
		public static string? GetGitRepositoryPath(string? path, string root) => _impl.GetGitRepositoryPath(path, root);

		/// <inheritdoc cref="IVersionControl.GetOriginRepositoryName(string?)"/>
		public static string GetOriginRepositoryName(string? path) => _impl.GetOriginRepositoryName(path);

		/// <inheritdoc cref="IVersionControl.GetBranchesNames(string?)"/>
		public static Task<BranchItem[]> GetBranchesNames(string? path) => _impl.GetBranchesNames(path);

		/// <inheritdoc cref="IVersionControl.GetRepositoryHead(string?)"/>
		public static Task<BranchItem?> GetRepositoryHead(string? path) => _impl.GetRepositoryHead(path);

		/// <inheritdoc cref="IVersionControl.Checkout(string?,string?)"/>
		public static Task<bool> Checkout(string? repositoryPath, string? branch) => _impl.Checkout(repositoryPath, branch);

		/// <inheritdoc cref="IVersionControl.CreateNewBranchAsync(string,string)"/>
		public static Task CreateNewBranchAsync(string repositoryPath, string activeBranch) => _impl.CreateNewBranchAsync(repositoryPath, activeBranch);

		/// <inheritdoc cref="IVersionControl.DeleteBranchAsync(string?,string?,string?)"/>
		public static Task DeleteBranchAsync(string? repositoryPath, string? activeBranch, string? branchToDelete) => _impl.DeleteBranchAsync(repositoryPath, activeBranch, branchToDelete);

		/// <inheritdoc cref="IVersionControl.ValidateBranchNameForRepository(string,string)"/>
		public static bool ValidateBranchNameForRepository(string branchName, string repositoryPath) => _impl.ValidateBranchNameForRepository(branchName, repositoryPath);

		/// <inheritdoc cref="IVersionControl.FetchOrigin(string?,CancellationToken)"/>
		public static void FetchOrigin(string? repositoryPath, CancellationToken cancellationToken = default) => _impl.FetchOrigin(repositoryPath, cancellationToken);

		/// <inheritdoc cref="IVersionControl.PullOriginAsync(string?)"/>
		public static Task PullOriginAsync(string? repositoryPath) => _impl.PullOriginAsync(repositoryPath);

		/// <inheritdoc cref="IVersionControl.PushToOriginAsync(string?,string?)"/>
		public static Task PushToOriginAsync(string? repositoryPath, string? branchName) => _impl.PushToOriginAsync(repositoryPath, branchName);

		/// <inheritdoc cref="IVersionControl.RequireGitAuthenticationAsync"/>
		public static Task RequireGitAuthenticationAsync() => _impl.RequireGitAuthenticationAsync();

		/// <inheritdoc cref="IVersionControl.IsRepositoryEx(string,out string)"/>
		public static bool IsRepositoryEx(string path, out string repoRootPath) => _impl.IsRepositoryEx(path, out repoRootPath);

		/// <inheritdoc cref="IVersionControl.GetGitInformationForItem(Repository,string,bool,bool)"/>
		public static GitItemModel GetGitInformationForItem(Repository repository, string path, bool getStatus = true, bool getCommit = true) => _impl.GetGitInformationForItem(repository, path, getStatus, getCommit);

		/// <inheritdoc cref="IVersionControl.RemoveSavedCredentials"/>
		public static void RemoveSavedCredentials() => _impl.RemoveSavedCredentials();

		/// <inheritdoc cref="IVersionControl.GetSavedCredentials"/>
		public static string GetSavedCredentials() => _impl.GetSavedCredentials();

		/// <inheritdoc cref="IVersionControl.InitializeRepositoryAsync(string?)"/>
		public static Task InitializeRepositoryAsync(string? path) => _impl.InitializeRepositoryAsync(path);

		/// <inheritdoc cref="IVersionControl.GetRepoInfo(string)"/>
		public static (string RepoUrl, string RepoName) GetRepoInfo(string url) => _impl.GetRepoInfo(url);

		/// <inheritdoc cref="IVersionControl.IsValidRepoUrl(string)"/>
		public static bool IsValidRepoUrl(string url) => _impl.IsValidRepoUrl(url);

		/// <inheritdoc cref="IVersionControl.CloneRepoAsync(string,string,string)"/>
		public static Task CloneRepoAsync(string repoUrl, string repoName, string targetDirectory) => _impl.CloneRepoAsync(repoUrl, repoName, targetDirectory);
	}
}
