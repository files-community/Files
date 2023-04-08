using LibGit2Sharp;
using Files.Shared.Enums;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
	public static class GitHelpers
	{
		public static string? GetGitRepositoryPath(string? path, string root)
		{
			if (
				string.IsNullOrWhiteSpace(path) || 
				path.Equals(root, StringComparison.OrdinalIgnoreCase)
				) 
				return null;

			return Repository.IsValid(path)
				? path
				: GetGitRepositoryPath(PathNormalization.GetParentDir(path), root);
		}

		public static async Task CreateNewBranch(string repositoryPath)
		{
			var dialog = DynamicDialogFactory.GetFor_AddBranchDialog();
			await dialog.TryShowAsync();

			if (dialog.DynamicResult != DynamicDialogResult.Primary)
				return;

			using var repository = new Repository(repositoryPath);
			repository.CreateBranch(dialog.ViewModel.AdditionalData as string);
		}

		public static bool IsBranchNameValid(string name)
		{
			// TODO: Validate branch name
			return true;
		}
	}
}
