using LibGit2Sharp;
using Files.Shared.Enums;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using System.Linq;
using Files.App.Filesystem.StorageItems;

namespace Files.App.Helpers
{
	public static class GitHelpers
	{
		private const string BRANCH_NAME_PATTERN = @"^(?!/)(?!.*//)[^\000-\037\177 ~^:?*[]+(?!.*\.\.)(?!.*@\{)(?!.*\\)(?<!/\.)(?<!\.)(?<!/)(?<!\.lock)$";
		private readonly static Regex branchValidator = new(BRANCH_NAME_PATTERN);

		public static string? GetGitRepositoryPath(string? path, string root)
		{
			if (root.EndsWith('\\'))
				root = root.Substring(0, root.Length - 1);

			if (
				string.IsNullOrWhiteSpace(path) || 
				path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
				ShellStorageFolder.IsShellPath(path)
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
			if (string.IsNullOrEmpty(name) || !branchValidator.IsMatch(name))
				return false;

			var repositoryPath = Ioc.Default.GetRequiredService<IContentPageContext>().GitRepositoryPath;
			if (string.IsNullOrEmpty(repositoryPath))
				return false;

			using var repository = new Repository(repositoryPath);
			return !repository.Branches.Any(branch => 
				branch.FriendlyName.Equals(name, StringComparison.OrdinalIgnoreCase));
		}
	}
}
