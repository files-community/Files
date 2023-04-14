using LibGit2Sharp;
using System;
using Files.App.Filesystem.StorageItems;

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
				ShellStorageFolder.IsShellPath(path)
				) 
				return null;

			return Repository.IsValid(path)
				? path
				: GetGitRepositoryPath(PathNormalization.GetParentDir(path), root);
		}
	}
}
