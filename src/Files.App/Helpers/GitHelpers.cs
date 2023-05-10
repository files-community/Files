// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using LibGit2Sharp;
using Files.App.Filesystem.StorageItems;
using Windows.Devices.Display.Core;

namespace Files.App.Helpers
{
	public static class GitHelpers
	{
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

		public static string[] GetLocalBranchesNames(string? path)
		{
			if (string.IsNullOrWhiteSpace(path) || !Repository.IsValid(path))
				return Array.Empty<string>();

			using var repository = new Repository(path);
			return repository.Branches
				.Where(b => !b.IsRemote)
				.Select(b => b.FriendlyName)
				.ToArray();
		}

		public static async Task Checkout(string? repositoryPath, string? branch)
		{
			if (string.IsNullOrWhiteSpace(repositoryPath) || !Repository.IsValid(repositoryPath))
				return;

			using var repository = new Repository(repositoryPath);
			var checkoutBranch = repository.Branches[branch];
			if (checkoutBranch is null)
				return;

			var options = new CheckoutOptions();

			if (repository.RetrieveStatus().IsDirty)
			{
				var dialog = DynamicDialogFactory.GetFor_GitCheckoutConflicts();
				await dialog.ShowAsync();

				var resolveConflictOption = (GitCheckoutOptions)dialog.ViewModel.AdditionalData;

				switch (resolveConflictOption)
				{
					case GitCheckoutOptions.None:
						return;
					case GitCheckoutOptions.BringChanges:
						break;
					case GitCheckoutOptions.DiscardChanges:
						options.CheckoutModifiers = CheckoutModifiers.Force;
						break;
					case GitCheckoutOptions.StashChanges:
						repository.Stashes.Add(repository.Config.BuildSignature(DateTimeOffset.Now));
						break;
				}
			}
			
			LibGit2Sharp.Commands.Checkout(repository, checkoutBranch, options);
		}
	}
}
