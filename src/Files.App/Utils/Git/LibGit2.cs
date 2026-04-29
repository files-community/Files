using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace Files.App.Utils.Git;

internal sealed partial class LibGit2 // : IVersionControl
{
	private const string GIT_RESOURCE_NAME = "Files:https://github.com";
	private const string GIT_RESOURCE_USERNAME = "Personal Access Token";
	private const string CLIENT_ID_SECRET = Constants.AutomatedWorkflowInjectionKeys.GitHubClientId;

	private const int END_OF_ORIGIN_PREFIX = 7;
	private const int MAX_NUMBER_OF_BRANCHES = 30;

	private static readonly SemaphoreSlim GitOperationSemaphore = new(1, 1);
	private static readonly FetchOptions _fetchOptions = new() { Prune = true };
	private static readonly PullOptions _pullOptions = new();
	private static readonly string _clientId = AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev
		? string.Empty
		: CLIENT_ID_SECRET;

	private bool _isExecutingGitAction;

	private static readonly StatusCenterViewModel StatusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
	private static readonly ILogger _logger = Ioc.Default.GetRequiredService<ILogger<App>>();
	private static readonly IDialogService _dialogService = Ioc.Default.GetRequiredService<IDialogService>();

	public bool IsExecutingGitAction
	{
		get => _isExecutingGitAction;
		private set
		{
			if (_isExecutingGitAction != value)
			{
				_isExecutingGitAction = value;
				IsExecutingGitActionChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExecutingGitAction)));
			}
		}
	}

	public event PropertyChangedEventHandler? IsExecutingGitActionChanged;
	public event EventHandler? GitFetchCompleted;

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

	private static bool IsRepoValid(string path)
	{
		return SafetyExtensions.IgnoreExceptions(() => Repository.IsValid(path));
	}

	private static IEnumerable<Branch> GetValidBranches(BranchCollection branches)
	{
		foreach (var branch in branches)
		{
			try
			{
				_ = branch.IsCurrentRepositoryHead;
			}
			catch (LibGit2SharpException)
			{
				continue;
			}

			yield return branch;
		}
	}

	private static BranchTrackingDetails? TryGetTrackingDetails(Branch branch)
	{
		try
		{
			return branch.TrackingDetails;
		}
		catch (LibGit2SharpException)
		{
			return null;
		}
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

		// TODO: This is a temp fix to avoid an issue where Files would create many branches in a loop
		if (repository.Branches.Any(b => !b.IsRemote && b.FriendlyName == uniqueName))
			return;

		//var discriminator = 0;
		//while (repository.Branches.Any(b => !b.IsRemote && b.FriendlyName == uniqueName))
		//	uniqueName = $"{branch.FriendlyName}_{++discriminator}";

		var newBranch = repository.CreateBranch(uniqueName, branch.Tip);
		repository.Branches.Update(newBranch, b => b.TrackedBranch = branch.CanonicalName);

		LibGit2Sharp.Commands.Checkout(repository, newBranch);
	}

	private static bool IsAuthorizationException(Exception ex)
	{
		return
			ex.Message.Contains("status code: 401", StringComparison.OrdinalIgnoreCase) ||
			ex.Message.Contains("authentication replays", StringComparison.OrdinalIgnoreCase);
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

	[GeneratedRegex(@"^(?:https?:\/\/)?(?:www\.)?(?<domain>github|gitlab)\.com\/(?<user>[^\/]+)\/(?<repo>[^\/]+?)(?=\.git|\/|$)(?:\.git)?(?:\/)?", RegexOptions.IgnoreCase)]
	private static partial Regex GitHubRepositoryRegex();
}
