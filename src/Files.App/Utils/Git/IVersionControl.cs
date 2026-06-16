// Copyright (c) Files Community
// Licensed under the MIT License.

using LibGit2Sharp;
using System.ComponentModel;

namespace Files.App.Utils.Git
{
	/// <summary>
	/// Defines a version control abstraction
	/// </summary>
	/// <remarks>
	/// This interface is intended to decouple the app from a specific backend implementation (e.g. a library such as LibGit2Sharp, or a command-line implementation backed by the <c>git.exe</c> executable).
	/// </remarks>
	internal interface IVersionControl
	{
		/// <summary>
		/// Attempts to locate the root of a version control repository (by walking up the directory hierarchy).
		/// </summary>
		/// <param name="path">The starting path to search from.</param>
		/// <param name="root">The filesystem root boundary (for example, a drive root) at which the search must stop.</param>
		/// <returns>
		/// The repository root path if one is found; otherwise, <see langword="null"/>.
		/// </returns>
		/// <remarks>
		/// This method is used for determining whether a directory is within a repository and, if so,
		/// which directory should be treated as the repository root.
		/// </remarks>
		string? GetGitRepositoryPath(string? path, string root);

		/// <summary>
		/// Gets the repository name.
		/// </summary>
		/// <param name="path">A path to the repository working directory.</param>
		/// <returns>
		/// The remote repository name (without the <c>.git</c> suffix), or an empty string if it cannot be determined.
		/// </returns>
		string GetOriginRepositoryName(string? path);

		/// <summary>
		/// Retrieves branch information (names and tracking status) for the given repository.
		/// </summary>
		/// <param name="path">A path to the repository working directory.</param>
		/// <returns>
		/// A task producing an array of branches; returns an empty array when the repository is invalid or unavailable.
		/// </returns>
		Task<BranchItem[]> GetBranchesNames(string? path);

		/// <summary>
		/// Gets the current repository HEAD reference.
		/// </summary>
		/// <param name="path">A path to the repository working directory.</param>
		/// <returns>
		/// A task producing a <see cref="BranchItem"/> representing the HEAD, or <see langword="null"/> if not available.
		/// </returns>
		Task<BranchItem?> GetRepositoryHead(string? path);

		/// <summary>
		/// Checks out the specified branch.
		/// </summary>
		/// <param name="repositoryPath">A path to the repository working directory.</param>
		/// <param name="branch">The branch name to check out.</param>
		/// <returns>
		/// A task producing <see langword="true"/> if checkout succeeded; otherwise <see langword="false"/>.
		/// </returns>
		/// <remarks>
		/// Implementations should prompt the user when there are conflicts or uncommitted changes.
		/// </remarks>
		Task<bool> Checkout(string? repositoryPath, string? branch);

		/// <summary>
		/// Creates a new local branch and optionally checks it out.
		/// </summary>
		/// <param name="repositoryPath">A path to the repository working directory.</param>
		/// <param name="activeBranch">The currently active branch name.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		/// <remarks>
		/// Implementations should involve UI for branch name selection and validation.
		/// </remarks>
		Task CreateNewBranchAsync(string repositoryPath, string activeBranch);

		/// <summary>
		/// Deletes a local branch.
		/// </summary>
		/// <param name="repositoryPath">A path to the repository working directory.</param>
		/// <param name="activeBranch">The current branch; the implementation should not delete the active branch.</param>
		/// <param name="branchToDelete">The branch name to delete.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task DeleteBranchAsync(string? repositoryPath, string? activeBranch, string? branchToDelete);

		/// <summary>
		/// Validates whether a branch name is valid and does not already exist within the repository.
		/// </summary>
		/// <param name="branchName">The proposed branch name.</param>
		/// <param name="repositoryPath">A path to the repository working directory.</param>
		/// <returns>
		/// <see langword="true"/> if the name is acceptable for the repository; otherwise <see langword="false"/>.
		/// </returns>
		bool ValidateBranchNameForRepository(string branchName, string repositoryPath);

		/// <summary>
		/// Fetches updates from remotes.
		/// </summary>
		/// <param name="repositoryPath">A path to the repository working directory.</param>
		/// <param name="cancellationToken">A token used to cancel the operation.</param>
		/// <remarks>
		/// Implementations should raise <see cref="GitFetchCompleted"/> when the fetch completes successfully.
		/// </remarks>
		void FetchOrigin(string? repositoryPath, CancellationToken cancellationToken = default);

		/// <summary>
		/// Pulls from the default remote.
		/// </summary>
		/// <param name="repositoryPath">A path to the repository working directory.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task PullOriginAsync(string? repositoryPath);

		/// <summary>
		/// Pushes a branch to the default remote.
		/// </summary>
		/// <param name="repositoryPath">A path to the repository working directory.</param>
		/// <param name="branchName">The branch name to push.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task PushToOriginAsync(string? repositoryPath, string? branchName);

		/// <summary>
		/// Initiates an authentication flow suitable for the configured remote.
		/// </summary>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task RequireGitAuthenticationAsync();

		/// <summary>
		/// Determines whether the given path is within a repository and returns the resolved repository root.
		/// </summary>
		/// <param name="path">The path to test.</param>
		/// <param name="repoRootPath">When this method returns <see langword="true"/>, contains the repository root path.</param>
		/// <returns>
		/// <see langword="true"/> if a repository was found; otherwise, <see langword="false"/>.
		/// </returns>
		bool IsRepositoryEx(string path, out string repoRootPath);

		/// <summary>
		/// Gets version control information for a filesystem item.
		/// </summary>
		/// <param name="repository">The opened repository instance used to retrieve information.</param>
		/// <param name="path">The full path to the filesystem item.</param>
		/// <param name="getStatus">Whether to compute status (working tree/index changes).</param>
		/// <param name="getCommit">Whether to compute the last commit affecting the item.</param>
		/// <returns>A <see cref="GitItemModel"/> describing the item.</returns>
		GitItemModel GetGitInformationForItem(Repository repository, string path, bool getStatus = true, bool getCommit = true);

		/// <summary>
		/// Removes any stored credentials associated with the version control provider.
		/// </summary>
		void RemoveSavedCredentials();

		/// <summary>
		/// Gets any stored credentials associated with the version control provider.
		/// </summary>
		/// <returns>The stored credential (typically a token), or an empty string if none exists.</returns>
		string GetSavedCredentials();

		/// <summary>
		/// Initialises a new repository at the specified location.
		/// </summary>
		/// <param name="path">The target directory path.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task InitializeRepositoryAsync(string? path);

		/// <summary>
		/// Parses a repository URL and returns normalised information.
		/// </summary>
		/// <param name="url">The input URL.</param>
		/// <returns>
		/// A tuple containing the normalised repository URL and the repository name.
		/// </returns>
		/// <remarks>
		/// The set of recognised URLs is defined by the implementation (github, gitlab, etc.)
		/// </remarks>
		(string RepoUrl, string RepoName) GetRepoInfo(string url);

		/// <summary>
		/// Checks whether the provided URL is recognised as a repository URL.
		/// </summary>
		/// <param name="url">The URL to validate.</param>
		/// <returns><see langword="true"/> if the URL is valid; otherwise, <see langword="false"/>.</returns>
		bool IsValidRepoUrl(string url);

		/// <summary>
		/// Clones a repository into the specified target directory.
		/// </summary>
		/// <param name="repoUrl">The repository URL to clone.</param>
		/// <param name="repoName">A display-friendly repository name.</param>
		/// <param name="targetDirectory">The directory where the repository should be cloned.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task CloneRepoAsync(string repoUrl, string repoName, string targetDirectory);

		/// <summary>
		/// Gets a value indicating whether a version control operation is currently running.
		/// </summary>
		/// <value>
		/// <see langword="true"/> while an operation is in progress; otherwise <see langword="false"/>.
		/// </value>
		bool IsExecutingGitAction { get; }

		event PropertyChangedEventHandler? IsExecutingGitActionChanged;

		/// <summary>
		/// Raised when a fetch operation completes.
		/// </summary>
		event EventHandler? GitFetchCompleted;
	}
}
