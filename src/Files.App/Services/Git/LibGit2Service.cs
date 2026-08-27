using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Sentry;
using System.Text;
using System.Text.RegularExpressions;

namespace Files.App.Services.Git;

internal sealed partial class LibGit2Service // : IVersionControl
{
	private const string GIT_RESOURCE_NAME = "Files:https://github.com";
	private const string GIT_RESOURCE_USERNAME = "Personal Access Token";
	private const string CLIENT_ID_SECRET = Constants.AutomatedWorkflowInjectionKeys.GitHubClientId;

	private const int END_OF_ORIGIN_PREFIX = 7;
	private const int MAX_NUMBER_OF_BRANCHES = 30;

	private static readonly PullOptions _pullOptions = new();
	private static readonly string _clientId = AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev
		? string.Empty
		: CLIENT_ID_SECRET;

	private bool _isExecutingGitAction;
	private int _activeFetchCount;

	private static readonly StatusCenterViewModel StatusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
	private static readonly ILogger _logger = Ioc.Default.GetRequiredService<ILogger<App>>();
	private static readonly IDialogService _dialogService = Ioc.Default.GetRequiredService<IDialogService>();

	public bool IsExecutingGitAction
	{
		get => _isExecutingGitAction;
		internal set // TODO: Make set method private again when move finished
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

	public string? GetGitRepositoryPath(string? path, string? root)
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

	public async Task<BranchItem?> GetRepositoryHead(string? path)
	{
		if (string.IsNullOrWhiteSpace(path) || !IsRepoValid(path))
			return null;

		var (_, returnValue) = await DoGitOperationAsync<(GitOperationResult, BranchItem?)>(() =>
		{
			BranchItem? head = null;
			try
			{
				using var repository = new Repository(path);
				var branch = repository.Head;
				if (branch?.Tip is not null)
				{
					var trackingDetails = TryGetTrackingDetails(branch);
					head = new BranchItem(
						branch.FriendlyName,
						true,
						branch.IsRemote,
						trackingDetails?.AheadBy ?? 0,
						trackingDetails?.BehindBy ?? 0
					);
				}
			}
			catch
			{
				return (GitOperationResult.GenericError, head);
			}

			return (GitOperationResult.Success, head);
		});

		return returnValue;
	}

	public Task<string?> GetRepositoryHeadName(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return Task.FromResult<string?>(null);

		return Task.Run(() =>
		{
			try
			{
				using var repository = new Repository(path);
				var branch = repository.Head;
				return branch?.Tip is null ? null : branch.FriendlyName;
			}
			// The repository may have been removed or corrupted after discovery returned its path
			catch (LibGit2SharpException)
			{
				return null;
			}
		});
	}

	public async Task<bool> Checkout(string? repositoryPath, string? branch)
	{
		SentrySdk.Metrics.EmitCounter("Triggered git checkout", 1);

		if (string.IsNullOrWhiteSpace(repositoryPath) || !IsRepoValid(repositoryPath))
			return false;

		using var repository = new Repository(repositoryPath);
		var checkoutBranch = repository.Branches[branch];
		if (checkoutBranch is null)
			return false;

		var options = new CheckoutOptions();
		var isBringingChanges = false;

		IsExecutingGitAction = true;

		if (repository.Index.Conflicts.Any())
		{
			var dialog = DynamicDialogFactory.GetFor_GitMergeConflicts(checkoutBranch.FriendlyName, repository.Head.FriendlyName);
			await dialog.ShowAsync();

			var resolveConflictOption = dialog.ViewModel.AdditionalData is GitCheckoutOptions option
				? option
				: GitCheckoutOptions.None;

			switch (resolveConflictOption)
			{
				case GitCheckoutOptions.None:
					IsExecutingGitAction = false;
					return false;
				case GitCheckoutOptions.AbortMerge:
					repository.Reset(ResetMode.Hard);
					break;
			}
		}
		else if (repository.RetrieveStatus().IsDirty)
		{
			var dialog = DynamicDialogFactory.GetFor_GitCheckoutConflicts(checkoutBranch.FriendlyName, repository.Head.FriendlyName);
			await dialog.ShowAsync();

			var resolveConflictOption = dialog.ViewModel.AdditionalData is GitCheckoutOptions option
				? option
				: GitCheckoutOptions.None;

			switch (resolveConflictOption)
			{
				case GitCheckoutOptions.None:
					IsExecutingGitAction = false;
					return false;
				case GitCheckoutOptions.DiscardChanges:
					options.CheckoutModifiers = CheckoutModifiers.Force;
					break;
				case GitCheckoutOptions.BringChanges:
				case GitCheckoutOptions.StashChanges:
					var signature = repository.Config.BuildSignature(DateTimeOffset.Now);
					if (signature is null)
					{
						IsExecutingGitAction = false;
						return false;
					}

					repository.Stashes.Add(signature);

					isBringingChanges = resolveConflictOption is GitCheckoutOptions.BringChanges;
					break;
			}
		}

		var statusOperation = new GitStatusCenterOperation(
			GitStatusCenterOperationKind.Checkout,
			repositoryPath,
			canProvideProgress: true,
			operationTarget: checkoutBranch.FriendlyName);
		options.OnCheckoutProgress = (path, completed, total) => statusOperation.ReportProgress(
			completed,
			total,
			path);
		var result = GitOperationResult.GenericError;

		try
		{
			result = await DoGitOperationAsync<GitOperationResult>(() =>
			{
				try
				{
					if (checkoutBranch.IsRemote)
						CheckoutRemoteBranch(repository, checkoutBranch, options);
					else
						LibGit2Sharp.Commands.Checkout(repository, checkoutBranch, options);

					if (isBringingChanges)
						repository.Stashes.Pop(0, new StashApplyOptions());
				}
				catch (Exception)
				{
					return GitOperationResult.GenericError;
				}

				return GitOperationResult.Success;
			});

			return result is GitOperationResult.Success;
		}
		finally
		{
			statusOperation.Complete(result is GitOperationResult.Success
				? ReturnResult.Success
				: ReturnResult.Failed);
			IsExecutingGitAction = false;
		}
	}

	public async Task CreateNewBranchAsync(string repositoryPath, string activeBranch)
	{
		SentrySdk.Metrics.EmitCounter("Triggered create git branch", 1);

		var viewModel = new AddBranchDialogViewModel(repositoryPath, activeBranch);
		var loadBranchesTask = viewModel.LoadBranches();
		var dialog = _dialogService.GetDialog(viewModel);

		await loadBranchesTask;
		var result = await dialog.TryShowAsync();

		if (result != DialogResult.Primary)
			return;

		using var repository = new Repository(repositoryPath);

		IsExecutingGitAction = true;

		if (repository.Head.FriendlyName.Equals(viewModel.NewBranchName) ||
			await Checkout(repositoryPath, viewModel.BasedOn))
		{
			repository.CreateBranch(viewModel.NewBranchName);

			if (viewModel.Checkout)
				await Checkout(repositoryPath, viewModel.NewBranchName);
		}

		IsExecutingGitAction = false;
	}

	public async Task DeleteBranchAsync(string? repositoryPath, string? activeBranch, string? branchToDelete)
	{
		SentrySdk.Metrics.EmitCounter("Triggered delete git branch", 1);

		if (string.IsNullOrWhiteSpace(repositoryPath) ||
			string.IsNullOrWhiteSpace(activeBranch) ||
			string.IsNullOrWhiteSpace(branchToDelete) ||
			activeBranch.Equals(branchToDelete, StringComparison.OrdinalIgnoreCase) ||
			!IsRepoValid(repositoryPath))
		{
			return;
		}

		var dialog = DynamicDialogFactory.GetFor_DeleteGitBranchConfirmation(branchToDelete);
		await dialog.TryShowAsync();
		if (!(dialog.ViewModel.AdditionalData as bool? ?? false))
			return;

		IsExecutingGitAction = true;

		await DoGitOperationAsync<GitOperationResult>(() =>
		{
			try
			{
				using var repository = new Repository(repositoryPath);
				repository.Branches.Remove(branchToDelete);
			}
			catch (Exception)
			{
				return GitOperationResult.GenericError;
			}

			return GitOperationResult.Success;
		});

		IsExecutingGitAction = false;
	}

	public bool ValidateBranchNameForRepository(string branchName, string repositoryPath)
	{
		if (string.IsNullOrEmpty(branchName) || !IsRepoValid(repositoryPath))
			return false;

		var nameValidator = RegexHelpers.GitBranchName();
		if (!nameValidator.IsMatch(branchName))
			return false;

		using var repository = new Repository(repositoryPath);
		return !repository.Branches.Any(branch =>
			branch.FriendlyName.Equals(branchName, StringComparison.OrdinalIgnoreCase));
	}

	public async Task FetchOriginAsync(string? repositoryPath, bool reportProgress, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(repositoryPath))
			return;

		var statusOperation = reportProgress
			? new GitStatusCenterOperation(
				GitStatusCenterOperationKind.Fetch,
				repositoryPath,
				canProvideProgress: true)
			: null;
		var fetchStarted = false;
		var fetchCompleted = false;
		var fetchResult = ReturnResult.Failed;
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			Interlocked.Increment(ref _activeFetchCount);
			fetchStarted = true;
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(
				() => IsExecutingGitAction = Volatile.Read(ref _activeFetchCount) > 0);

			await Task.Run(() =>
			{
				using var repository = new Repository(repositoryPath);
				var signature = repository.Config.BuildSignature(DateTimeOffset.Now);
				var token = CredentialsHelpers.GetPassword(GIT_RESOURCE_NAME, GIT_RESOURCE_USERNAME);
				var remotes = repository.Network.Remotes.ToArray();
				var hasFetchFailure = false;

				for (var remoteIndex = 0; remoteIndex < remotes.Length; remoteIndex++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					var remote = remotes[remoteIndex];
					var startPercentage = remoteIndex * 100.0 / remotes.Length;
					var endPercentage = (remoteIndex + 1) * 100.0 / remotes.Length;
					var fetchOptions = new FetchOptions
					{
						Prune = true,
						OnProgress = _ => !cancellationToken.IsCancellationRequested,
						OnTransferProgress = progress =>
						{
							statusOperation?.ReportProgress(
								progress.ReceivedObjects,
								progress.TotalObjects,
								startPercentage: startPercentage,
								endPercentage: endPercentage);
							return !cancellationToken.IsCancellationRequested;
						},
					};

					if (signature is not null && !string.IsNullOrWhiteSpace(token))
					{
						fetchOptions.CredentialsProvider = (url, user, cred)
							=> new UsernamePasswordCredentials
							{
								Username = signature.Name,
								Password = token
							};
					}

					try
					{
						LibGit2Sharp.Commands.Fetch(
							repository,
							remote.Name,
							remote.FetchRefSpecs.Select(rs => rs.Specification),
							fetchOptions,
							"git fetch updated a ref");
					}
					catch (Exception ex)
					{
						cancellationToken.ThrowIfCancellationRequested();
						hasFetchFailure = true;
						// An unreachable remote (e.g. a deleted fork answering 401) must not prevent fetching the remaining remotes
						_logger.LogWarning(ex, "Failed to fetch remote {RemoteName} in {RepositoryPath}", remote.Name, LogPathHelper.RedactPath(repositoryPath));
					}
				}

				fetchResult = hasFetchFailure ? ReturnResult.Failed : ReturnResult.Success;
			}, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			fetchCompleted = true;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			fetchResult = ReturnResult.Cancelled;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to fetch repository {RepositoryPath}", LogPathHelper.RedactPath(repositoryPath));
		}
		finally
		{
			if (fetchStarted)
				Interlocked.Decrement(ref _activeFetchCount);

			if (fetchStarted || statusOperation is not null)
			{
				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
				{
					IsExecutingGitAction = Volatile.Read(ref _activeFetchCount) > 0;
					statusOperation?.Complete(fetchResult);
					if (fetchCompleted && !cancellationToken.IsCancellationRequested)
						GitFetchCompleted?.Invoke(this, EventArgs.Empty);
				});
			}
		}
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

	private static void CheckoutRemoteBranch(Repository repository, Branch branch, CheckoutOptions options)
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

		LibGit2Sharp.Commands.Checkout(repository, newBranch, new CheckoutOptions
		{
			OnCheckoutProgress = options.OnCheckoutProgress,
		});
	}

	private static async Task<T?> DoGitOperationAsync<T>(Func<object> payload)
	{
		return (T)await Task.Run(payload);
	}
}
