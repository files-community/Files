// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Filesystem.StorageItems;
using Files.App.ViewModels.Dialogs;
using Files.Backend.Services;
using LibGit2Sharp;
using Microsoft.AppCenter.Analytics;
using System.Text.RegularExpressions;

namespace Files.App.Helpers
{
	public static class GitHelpers
	{
		private const string BRANCH_NAME_PATTERN = @"^(?!/)(?!.*//)[^\000-\037\177 ~^:?*[]+(?!.*\.\.)(?!.*@\{)(?!.*\\)(?<!/\.)(?<!\.)(?<!/)(?<!\.lock)$";
		
		private const int END_OF_ORIGIN_PREFIX = 7;

		public static string? GetGitRepositoryPath(string? path, string root)
		{
			if (root.EndsWith('\\'))
				root = root.Substring(0, root.Length - 1);

			if (
				string.IsNullOrWhiteSpace(path) ||
				path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
				path.Equals("Home", StringComparison.OrdinalIgnoreCase) ||
				ShellStorageFolder.IsShellPath(path)
				)
				return null;

			try
			{
				return Repository.IsValid(path)
					? path
					: GetGitRepositoryPath(PathNormalization.GetParentDir(path), root);
			}
			catch (LibGit2SharpException)
			{
				return null;
			}
		}

		public static string[] GetBranchesNames(string? path)
		{
			if (string.IsNullOrWhiteSpace(path) || !Repository.IsValid(path))
				return Array.Empty<string>();

			using var repository = new Repository(path);
			return repository.Branches
				.Where(b => !b.IsRemote || b.RemoteName == "origin")
				.OrderByDescending(b => b.IsCurrentRepositoryHead)
				.ThenBy(b => b.IsRemote)
				.ThenByDescending(b => b.Tip.Committer.When)
				.Select(b => b.FriendlyName)
				.ToArray();
		}

		public static async Task<bool> Checkout(string? repositoryPath, string? branch)
		{
			if (string.IsNullOrWhiteSpace(repositoryPath) || !Repository.IsValid(repositoryPath))
				return false;

			using var repository = new Repository(repositoryPath);
			var checkoutBranch = repository.Branches[branch];
			if (checkoutBranch is null)
				return false;

			var options = new CheckoutOptions();
			var isBringingChanges = false;

			Analytics.TrackEvent($"Triggered git checkout");

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
						repository.Stashes.Add(repository.Config.BuildSignature(DateTimeOffset.Now));

						isBringingChanges = resolveConflictOption is GitCheckoutOptions.BringChanges;
						break;
				}
			}

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

		public static async Task CreateNewBranch(string repositoryPath, string activeBranch)
		{
			var viewModel = new AddBranchDialogViewModel(repositoryPath, activeBranch);
			var dialog = Ioc.Default.GetRequiredService<IDialogService>().GetDialog(viewModel);

			var result = await dialog.TryShowAsync();

			if (result != DialogResult.Primary)
				return;

			using var repository = new Repository(repositoryPath);

			if (repository.Head.FriendlyName.Equals(viewModel.NewBranchName) ||
				await Checkout(repositoryPath, viewModel.BasedOn))
			{
				Analytics.TrackEvent($"Triggered git branch");

				repository.CreateBranch(viewModel.NewBranchName);

				if (viewModel.Checkout)
					await Checkout(repositoryPath, viewModel.NewBranchName);
			}
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
