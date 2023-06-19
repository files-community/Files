﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Files.App.Filesystem.StorageItems;
using Files.App.ViewModels.Dialogs;
using Files.Backend.Services;
using LibGit2Sharp;
using Microsoft.AppCenter.Analytics;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Files.App.Helpers
{
	internal static class GitHelpers
	{
		private const string BRANCH_NAME_PATTERN = @"^(?!/)(?!.*//)[^\000-\037\177 ~^:?*[]+(?!.*\.\.)(?!.*@\{)(?!.*\\)(?<!/\.)(?<!\.)(?<!/)(?<!\.lock)$";

		private const int END_OF_ORIGIN_PREFIX = 7;

		private static readonly ILogger _logger = Ioc.Default.GetRequiredService<ILogger<App>>();

		private static readonly FetchOptions _fetchOptions = new()
		{
			Prune = true
		};

		private static readonly PullOptions _pullOptions = new();

		private static bool _IsExecutingGitAction;
		public static bool IsExecutingGitAction
		{
			get => _IsExecutingGitAction;
			private set
			{
				if (_IsExecutingGitAction != value)
				{
					_IsExecutingGitAction = value;
					IsExecutingGitActionChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(IsExecutingGitAction)));
				}
			}
		}

		public static event PropertyChangedEventHandler? IsExecutingGitActionChanged;

		public static event EventHandler? GitFetchCompleted;

		public static string? GetGitRepositoryPath(string? path, string root)
		{
			if (root.EndsWith('\\'))
				root = root.Substring(0, root.Length - 1);

			if (string.IsNullOrWhiteSpace(path) ||
				path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
				path.Equals("Home", StringComparison.OrdinalIgnoreCase) ||
				ShellStorageFolder.IsShellPath(path))
			{
				return null;
			}

			try
			{
				return
					Repository.IsValid(path)
						? path
						: GetGitRepositoryPath(PathNormalization.GetParentDir(path), root);
			}
			catch (LibGit2SharpException ex)
			{
				_logger.LogWarning(ex.Message);

				return null;
			}
		}

		public static BranchItem[] GetBranchesNames(string? path)
		{
			if (string.IsNullOrWhiteSpace(path) || !Repository.IsValid(path))
				return Array.Empty<BranchItem>();

			using var repository = new Repository(path);
			return repository.Branches
				.Where(b => !b.IsRemote || b.RemoteName == "origin")
				.OrderByDescending(b => b.IsCurrentRepositoryHead)
				.ThenBy(b => b.IsRemote)
				.ThenByDescending(b => b.Tip?.Committer.When)
				.Select(b => new BranchItem(b.FriendlyName, b.IsRemote, b.TrackingDetails.AheadBy, b.TrackingDetails.BehindBy))
				.ToArray();
		}

		public static async Task<bool> Checkout(string? repositoryPath, string? branch)
		{
			Analytics.TrackEvent("Triggered git checkout");

			if (string.IsNullOrWhiteSpace(repositoryPath) || !Repository.IsValid(repositoryPath))
				return false;

			using var repository = new Repository(repositoryPath);
			var checkoutBranch = repository.Branches[branch];
			if (checkoutBranch is null)
				return false;

			var options = new CheckoutOptions();
			var isBringingChanges = false;

			IsExecutingGitAction = true;

			if (repository.RetrieveStatus().IsDirty)
			{
				var dialog = DynamicDialogFactory.GetFor_GitCheckoutConflicts(checkoutBranch.FriendlyName, repository.Head.FriendlyName);
				await dialog.ShowAsync();

				var resolveConflictOption = (GitCheckoutOptions)dialog.ViewModel.AdditionalData;

				switch (resolveConflictOption)
				{
					case GitCheckoutOptions.None:
						return false;
					case GitCheckoutOptions.DiscardChanges:
						options.CheckoutModifiers = CheckoutModifiers.Force;
						break;
					case GitCheckoutOptions.BringChanges:
					case GitCheckoutOptions.StashChanges:
						var signature = repository.Config.BuildSignature(DateTimeOffset.Now);
						if (signature is null)
							return false;

						repository.Stashes.Add(signature);

						isBringingChanges = resolveConflictOption is GitCheckoutOptions.BringChanges;
						break;
				}
			}

			try
			{
				if (checkoutBranch.IsRemote)
					CheckoutRemoteBranch(repository, checkoutBranch);
				else
					LibGit2Sharp.Commands.Checkout(repository, checkoutBranch, options);

				if (isBringingChanges)
				{
					var lastStashIndex = repository.Stashes.Count() - 1;
					repository.Stashes.Pop(lastStashIndex, new StashApplyOptions());
				}
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex.Message);
				return false;
			}
			finally
			{
				IsExecutingGitAction = false;
			}
		}

		public static async Task CreateNewBranch(string repositoryPath, string activeBranch)
		{
			Analytics.TrackEvent("Triggered create git branch");

			var viewModel = new AddBranchDialogViewModel(repositoryPath, activeBranch);
			var dialog = Ioc.Default.GetRequiredService<IDialogService>().GetDialog(viewModel);

			var result = await dialog.TryShowAsync();

			if (result != DialogResult.Primary)
				return;

			using var repository = new Repository(repositoryPath);

			IsExecutingGitAction = true;

			if (repository.Head.FriendlyName.Equals(viewModel.NewBranchName) ||
				await Checkout(repositoryPath, viewModel.BasedOn))
			{
				repository.CreateBranch(viewModel.NewBranchName);

				if (viewModel.Checkout)
					await Checkout(repositoryPath, viewModel.NewBranchName);
			}

			IsExecutingGitAction = false;
		}

		public static bool ValidateBranchNameForRepository(string branchName, string repositoryPath)
		{
			if (string.IsNullOrEmpty(branchName) || !Repository.IsValid(repositoryPath))
				return false;

			var nameValidator = new Regex(BRANCH_NAME_PATTERN);
			if (!nameValidator.IsMatch(branchName))
				return false;

			using var repository = new Repository(repositoryPath);
			return !repository.Branches.Any(branch =>
				branch.FriendlyName.Equals(branchName, StringComparison.OrdinalIgnoreCase));
		}

		public static void FetchOrigin(string? repositoryPath)
		{
			if (string.IsNullOrWhiteSpace(repositoryPath))
				return;

			IsExecutingGitAction = true;
			using var repository = new Repository(repositoryPath);

			try
			{
				foreach (var remote in repository.Network.Remotes)
				{
					LibGit2Sharp.Commands.Fetch(
						repository,
						remote.Name,
						remote.FetchRefSpecs.Select(rs => rs.Specification),
						_fetchOptions,
						"git fetch updated a ref");
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex.Message);
			}

			App.Window.DispatcherQueue.TryEnqueue(() =>
			{
				IsExecutingGitAction = false;
				GitFetchCompleted?.Invoke(null, EventArgs.Empty);
			});
		}

		public static async void PullOrigin(string? repositoryPath)
		{
			if (string.IsNullOrWhiteSpace(repositoryPath))
				return;

			using var repository = new Repository(repositoryPath);
			var signature = repository.Config.BuildSignature(DateTimeOffset.Now);
			if (signature is null)
				return;

			IsExecutingGitAction = true;

			try
			{
				LibGit2Sharp.Commands.Pull(
					repository,
					signature,
					_pullOptions);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex.Message);

				var viewModel = new DynamicDialogViewModel()
				{
					TitleText = "GitError".GetLocalizedResource(),
					SubtitleText = "PullTimeoutError".GetLocalizedResource(),
					CloseButtonText = "Close".GetLocalizedResource(),
					DynamicButtons = DynamicDialogButtons.Cancel
				};
				var dialog = new DynamicDialog(viewModel);
				await dialog.TryShowAsync();
			}

			IsExecutingGitAction = false;
		}

		public static bool IsRepositoryEx(string path, out string repoRootPath)
		{
			repoRootPath = path;

			var rootPath = SystemIO.Path.GetPathRoot(path);
			if (rootPath is null)
				return false;

			var repositoryRootPath = GetGitRepositoryPath(path, rootPath);
			if (string.IsNullOrEmpty(repositoryRootPath))
				return false;

			if (Repository.IsValid(repositoryRootPath))
			{
				repoRootPath = repositoryRootPath;
				return true;
			}

			return false;
		}

		public static GitItemModel GetGitInformationForItem(Repository repository, string path)
		{
			var rootRepoPath = repository.Info.WorkingDirectory;
			var relativePath = path.Substring(rootRepoPath.Length).Replace('\\', '/');

			var commit = GetLastCommitForFile(repository, relativePath);
			//var commit = repository.Commits.QueryBy(relativePath).FirstOrDefault()?.Commit; // Considers renames but slow

			var changeKind = ChangeKind.Unmodified;
			//foreach (TreeEntryChanges c in repository.Diff.Compare<TreeChanges>())
			foreach (TreeEntryChanges c in repository.Diff.Compare<TreeChanges>(repository.Commits.FirstOrDefault()?.Tree, DiffTargets.Index | DiffTargets.WorkingDirectory))
			{
				if (c.Path.StartsWith(relativePath))
				{
					changeKind = c.Status;
					break;
				}
			}

			string? changeKindHumanized = "";
			if (changeKind is not ChangeKind.Ignored)
			{
				changeKindHumanized = changeKind switch
				{
					ChangeKind.Added => "A",
					ChangeKind.Deleted => "D",
					ChangeKind.Modified => "M",
					ChangeKind.Untracked => "U",
					_ => "",
				};
			}

			var gitItemModel = new GitItemModel()
			{
				Status = changeKind,
				StatusHumanized = changeKindHumanized,
				LastCommit = commit,
				Path = relativePath,
			};

			return gitItemModel;
		}

		private static Commit? GetLastCommitForFile(Repository repository, string currentPath)
		{
			foreach (var currentCommit in repository.Commits)
			{
				var currentTreeEntry = currentCommit.Tree[currentPath];
				if (currentTreeEntry == null)
					return null;

				var parentCount = currentCommit.Parents.Take(2).Count();
				if (parentCount == 0)
				{
					return currentCommit;
				}
				else if (parentCount == 1)
				{
					var parentCommit = currentCommit.Parents.Single();

					// Does not consider renames
					var parentPath = currentPath; 

					var parentTreeEntry = parentCommit.Tree[parentPath];

					if (parentTreeEntry == null ||
						parentTreeEntry.Target.Id != currentTreeEntry.Target.Id ||
						parentPath != currentPath)
					{
						return currentCommit;
					}
				}
			}

			return null;
		}

		private static void CheckoutRemoteBranch(Repository repository, Branch branch)
		{
			var uniqueName = branch.FriendlyName.Substring(END_OF_ORIGIN_PREFIX);

			var discriminator = 0;
			while (repository.Branches.Any(b => !b.IsRemote && b.FriendlyName == uniqueName))
				uniqueName = $"{branch.FriendlyName}_{++discriminator}";

			var newBranch = repository.CreateBranch(uniqueName, branch.Tip);
			repository.Branches.Update(newBranch, b => b.TrackedBranch = branch.CanonicalName);

			LibGit2Sharp.Commands.Checkout(repository, newBranch);
		}
	}
}
