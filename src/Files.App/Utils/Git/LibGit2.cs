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

	public string GetOriginRepositoryName(string? path)
	{
		if (string.IsNullOrWhiteSpace(path) || !IsRepoValid(path))
			return string.Empty;

		using var repository = new Repository(path);
		var repositoryUrl = repository.Network.Remotes.FirstOrDefault()?.Url;

		if (string.IsNullOrEmpty(repositoryUrl))
			return string.Empty;

		var repositoryName = repositoryUrl.Split('/').Last();
		return repositoryName[..repositoryName.LastIndexOf(".git")];
	}

	public async Task<BranchItem[]> GetBranchNames(string? path)
	{
		if (string.IsNullOrWhiteSpace(path) || !IsRepoValid(path))
			return [];

		var (result, returnValue) = await DoGitOperationAsync<(GitOperationResult, BranchItem[])>(() =>
		{
			var branches = Array.Empty<BranchItem>();
			var result = GitOperationResult.Success;
			try
			{
				using var repository = new Repository(path);

				branches = GetValidBranches(repository.Branches)
					.OrderByDescending(b => b.Tip?.Committer.When)
					.GroupBy(b => b.IsRemote)
					.SelectMany(g => g.Take(MAX_NUMBER_OF_BRANCHES))
					.OrderByDescending(b => b.IsCurrentRepositoryHead)
					.Select(b => new BranchItem(b.FriendlyName, b.IsCurrentRepositoryHead, b.IsRemote, TryGetTrackingDetails(b)?.AheadBy ?? 0, TryGetTrackingDetails(b)?.BehindBy ?? 0))
					.ToArray();
			}
			catch (Exception)
			{
				result = GitOperationResult.GenericError;
			}

			return (result, branches);
		});

		return returnValue;
	}

	private static bool IsRepoValid(string path)
	{
		return SafetyExtensions.IgnoreExceptions(() => Repository.IsValid(path));
	}

	private static async Task<T?> DoGitOperationAsync<T>(Func<object> payload, bool useSemaphore = false)
	{
		if (useSemaphore)
			await GitOperationSemaphore.WaitAsync();
		else
			await Task.Yield();

		try
		{
			return (T)payload();
		}
		finally
		{
			if (useSemaphore)
				GitOperationSemaphore.Release();
		}
	}

	private static IEnumerable<Branch> GetValidBranches(BranchCollection branches)
	{
		foreach (var branch in branches)
		{
			try
			{
				var throwIfInvalid = branch.IsCurrentRepositoryHead;
			}
			catch (LibGit2SharpException)
			{
				continue;
			}

			yield return branch;
		}
	}
}
