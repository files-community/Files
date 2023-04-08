using LibGit2Sharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Files.App.Helpers
{
	public static class GitHelpers
	{
		public static string? GetGitRepositoryPath(string? path)
		{
			if (
				string.IsNullOrWhiteSpace(path) || 
				path.Equals(Path.GetPathRoot(path), StringComparison.OrdinalIgnoreCase)
				) 
				return null;

			return Repository.IsValid(path)
				? path
				: GetGitRepositoryPath(PathNormalization.GetParentDir(path));
		}

		public static async Task CreateNewBranch(string repositoryPath)
		{
			// TODO: Dialog to prompt for name

			using var repository = new Repository(repositoryPath);
			repository.CreateBranch("Test");
		}

		public static bool IsBranchNameValid(string name)
		{
			// TODO: Validate branch name
			return true;
		}
	}
}
