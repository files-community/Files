using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Files.App.Utils.Git;

internal sealed partial class LibGit2 // : IVersionControl
{
	private static readonly ILogger _logger = Ioc.Default.GetRequiredService<ILogger<App>>();

	public string? GetGitRepositoryPath(string? path, string root)
	{
		if (string.IsNullOrEmpty(root))
			return null;

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
			if (IsRepoValid(path))
				return path;
			else
			{
				var parentDir = PathNormalization.GetParentDir(path);
				if (parentDir == path)
					return null;
				else
					return GetGitRepositoryPath(parentDir, root);
			}
		}
		catch (Exception ex) when (ex is LibGit2SharpException or EncoderFallbackException)
		{
			_logger.LogWarning(ex.Message);

			return null;
		}
	}

	private static bool IsRepoValid(string path)
	{
		return SafetyExtensions.IgnoreExceptions(() => Repository.IsValid(path));
	}
}
