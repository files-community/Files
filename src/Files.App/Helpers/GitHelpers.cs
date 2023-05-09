// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using LibGit2Sharp;
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
	}
}
