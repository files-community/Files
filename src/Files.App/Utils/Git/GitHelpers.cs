// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Files.App.Services.Git;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Sentry;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Files.App.Utils.Git
{
	internal static partial class GitHelpers
	{
		// The implementation of the version control interface; it's hardcoded right now but will be made configurable in the future (#16738)
		private static LibGit2Service _implementation = Ioc.Default.GetRequiredService<LibGit2Service>(); // TODO: Replace with IVersionControl abstraction when it is complete

		/// <inheritdoc cref="IVersionControlService.GetGitRepositoryPath(string?,string)"/>
		public static string? GetGitRepositoryPath(string? path, string? root) => _implementation.GetGitRepositoryPath(path, root);

		/// <inheritdoc cref="IVersionControlService.GetOriginRepositoryName(string?)"/>
		public static string GetOriginRepositoryName(string? path) => _implementation.GetOriginRepositoryName(path);

		/// <inheritdoc cref="IVersionControlService.GetBranchNames(string?)"/>
		public static Task<BranchItem[]> GetBranchNames(string? path) => _implementation.GetBranchNames(path);

		/// <inheritdoc cref="IVersionControlService.GetRepositoryHead(string?)"/>
		public static Task<BranchItem?> GetRepositoryHead(string? path) => _implementation.GetRepositoryHead(path);

		/// <inheritdoc cref="IVersionControlService.GetRepositoryHeadName(string?)"/>
		public static Task<string?> GetRepositoryHeadName(string? path) => _implementation.GetRepositoryHeadName(path);

		/// <inheritdoc cref="IVersionControlService.Checkout(string?, string?)"/>
		public static Task<bool> Checkout(string? repositoryPath, string? branch) => _implementation.Checkout(repositoryPath, branch);

		/// <inheritdoc cref="IVersionControlService.CreateNewBranchAsync(string, string)"/>
		public static Task CreateNewBranchAsync(string repositoryPath, string activeBranch) => _implementation.CreateNewBranchAsync(repositoryPath, activeBranch);

		/// <inheritdoc cref="IVersionControlService.DeleteBranchAsync(string?, string?, string?)"/>
		public static Task DeleteBranchAsync(string? repositoryPath, string? activeBranch, string? branchToDelete) => _implementation.DeleteBranchAsync(repositoryPath, activeBranch, branchToDelete);

		/// <inheritdoc cref="IVersionControlService.ValidateBranchNameForRepository(string, string)"/>
		public static bool ValidateBranchNameForRepository(string branchName, string repositoryPath) => _implementation.ValidateBranchNameForRepository(branchName, repositoryPath);

		/// <inheritdoc cref="IVersionControlService.FetchOriginAsync(string?, bool, CancellationToken)"/>
		public static Task FetchOriginAsync(string? repositoryPath, bool reportProgress = false, CancellationToken cancellationToken = default)
			=> _implementation.FetchOriginAsync(repositoryPath, reportProgress, cancellationToken);

		/// <inheritdoc cref="IVersionControlService.IsExecutingGitAction"/>
		public static bool IsExecutingGitAction => _implementation.IsExecutingGitAction;

		/// <inheritdoc cref="IVersionControlService.IsExecutingGitActionChanged"/>
		public static event PropertyChangedEventHandler? IsExecutingGitActionChanged
		{
			add => _implementation.IsExecutingGitActionChanged += value;
			remove => _implementation.IsExecutingGitActionChanged -= value;
		}

		/// <inheritdoc cref="IVersionControlService.GitFetchCompleted"/>
		public static event EventHandler? GitFetchCompleted
		{
			add => _implementation.GitFetchCompleted += value;
			remove => _implementation.GitFetchCompleted -= value;
		}

		#region Legacy implementation

		// Constant already moved into abstraction
		private const string GIT_RESOURCE_NAME = "Files:https://github.com";

		// Constant already moved into abstraction
		private const string GIT_RESOURCE_USERNAME = "Personal Access Token";

		// Constant already moved into abstraction
		private const string CLIENT_ID_SECRET = Constants.AutomatedWorkflowInjectionKeys.GitHubClientId;

		// Constant already moved into abstraction
		private const int END_OF_ORIGIN_PREFIX = 7;

		// Constant already moved into abstraction
		private const int MAX_NUMBER_OF_BRANCHES = 30;

		// Property already moved into abstraction
		private static readonly ILogger _logger = Ioc.Default.GetRequiredService<ILogger<App>>();

		// Property already moved into abstraction
		private static readonly IDialogService _dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		// Property already moved into abstraction
		private static readonly string _clientId = AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev
				? string.Empty
				: CLIENT_ID_SECRET;

		// Property already moved into abstraction
		private static readonly SemaphoreSlim GitOperationSemaphore = new SemaphoreSlim(1, 1);

		public static async Task PullOriginAsync(string? repositoryPath)
		{
			if (string.IsNullOrWhiteSpace(repositoryPath))
				return;

			var statusOperation = new GitStatusCenterOperation(
				GitStatusCenterOperationKind.Pull,
				repositoryPath,
				canProvideProgress: true);
			var result = GitOperationResult.GenericError;

			SetGitActionExecutionState(true);
			try
			{
				var operationResult = await PullOriginCoreAsync(repositoryPath, statusOperation, 0, 100);
				result = operationResult.Result;
				await HandlePullResultAsync(operationResult.Result, operationResult.Message);
			}
			finally
			{
				statusOperation.Complete(ToReturnResult(result));
				SetGitActionExecutionState(false);
			}
		}

		public static async Task PushToOriginAsync(string? repositoryPath, string? branchName)
		{
			if (string.IsNullOrWhiteSpace(repositoryPath) || string.IsNullOrWhiteSpace(branchName))
				return;

			var statusOperation = new GitStatusCenterOperation(
				GitStatusCenterOperationKind.Push,
				repositoryPath,
				canProvideProgress: true);
			var result = GitOperationResult.GenericError;

			SetGitActionExecutionState(true);
			try
			{
				result = await PushToOriginCoreAsync(repositoryPath, branchName, statusOperation, 0, 100);
				if (result is GitOperationResult.AuthorizationError)
					await RequireGitAuthenticationAsync();
			}
			finally
			{
				statusOperation.Complete(ToReturnResult(result));
				SetGitActionExecutionState(false);
			}
		}

		public static async Task SyncOriginAsync(string? repositoryPath, string? branchName)
		{
			if (string.IsNullOrWhiteSpace(repositoryPath) || string.IsNullOrWhiteSpace(branchName))
				return;

			var statusOperation = new GitStatusCenterOperation(
				GitStatusCenterOperationKind.Sync,
				repositoryPath,
				canProvideProgress: true);
			var result = GitOperationResult.GenericError;

			SetGitActionExecutionState(true);
			try
			{
				var pullResult = await PullOriginCoreAsync(repositoryPath, statusOperation, 0, 50);
				result = pullResult.Result;
				await HandlePullResultAsync(pullResult.Result, pullResult.Message);

				if (pullResult.Result is GitOperationResult.Success)
				{
					result = await PushToOriginCoreAsync(repositoryPath, branchName, statusOperation, 50, 100);
					if (result is GitOperationResult.AuthorizationError)
						await RequireGitAuthenticationAsync();
				}
			}
			finally
			{
				statusOperation.Complete(ToReturnResult(result));
				SetGitActionExecutionState(false);
			}
		}

		private static async Task<(GitOperationResult Result, string? Message)> PullOriginCoreAsync(
			string repositoryPath,
			GitStatusCenterOperation statusOperation,
			double startPercentage,
			double endPercentage)
		{
			try
			{
				using var repository = new Repository(repositoryPath);
				var signature = repository.Config.BuildSignature(DateTimeOffset.Now);
				if (signature is null)
					return (GitOperationResult.GenericError, null);

				var fetchEndPercentage = startPercentage + (endPercentage - startPercentage) * 0.8;
				var fetchOptions = new FetchOptions
				{
					Prune = true,
					OnTransferProgress = progress =>
					{
						statusOperation.ReportProgress(
							progress.ReceivedObjects,
							progress.TotalObjects,
							startPercentage: startPercentage,
							endPercentage: fetchEndPercentage);
						return true;
					},
				};

				var token = CredentialsHelpers.GetPassword(GIT_RESOURCE_NAME, GIT_RESOURCE_USERNAME);
				if (!string.IsNullOrWhiteSpace(token))
				{
					fetchOptions.CredentialsProvider = (url, user, cred)
						=> new UsernamePasswordCredentials
						{
							Username = signature.Name,
							Password = token
						};
				}

				var pullOptions = new PullOptions
				{
					FetchOptions = fetchOptions,
					MergeOptions = new MergeOptions
					{
						OnCheckoutProgress = (path, completed, total) => statusOperation.ReportProgress(
							completed,
							total,
							path,
							fetchEndPercentage,
							endPercentage),
					},
				};

				return await DoGitOperationAsync<(GitOperationResult, string?)>(() =>
				{
					try
					{
						LibGit2Sharp.Commands.Pull(repository, signature, pullOptions);
					}
					catch (CheckoutConflictException ex)
					{
						return (GitOperationResult.UncommittedChangesError, ex.Message);
					}
					catch (Exception ex)
					{
						return IsAuthorizationException(ex)
							? (GitOperationResult.AuthorizationError, ex.Message)
							: (GitOperationResult.GenericError, ex.Message);
					}

					return (GitOperationResult.Success, (string?)null);
				});
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex.Message);
				return IsAuthorizationException(ex)
					? (GitOperationResult.AuthorizationError, ex.Message)
					: (GitOperationResult.GenericError, ex.Message);
			}
		}

		private static async Task<GitOperationResult> PushToOriginCoreAsync(
			string repositoryPath,
			string branchName,
			GitStatusCenterOperation statusOperation,
			double startPercentage,
			double endPercentage)
		{
			try
			{
				using var repository = new Repository(repositoryPath);
				var signature = repository.Config.BuildSignature(DateTimeOffset.Now);
				if (signature is null)
					return GitOperationResult.GenericError;

				var token = CredentialsHelpers.GetPassword(GIT_RESOURCE_NAME, GIT_RESOURCE_USERNAME);
				if (string.IsNullOrWhiteSpace(token))
				{
					await RequireGitAuthenticationAsync();
					token = CredentialsHelpers.GetPassword(GIT_RESOURCE_NAME, GIT_RESOURCE_USERNAME);
				}

				var progressRange = endPercentage - startPercentage;
				var deltafyingStartPercentage = startPercentage + progressRange * 0.25;
				var transferStartPercentage = startPercentage + progressRange * 0.5;
				var options = new PushOptions
				{
					CredentialsProvider = (url, user, cred)
						=> new UsernamePasswordCredentials
						{
							Username = signature.Name,
							Password = token
						},
					OnPackBuilderProgress = (stage, completed, total) =>
					{
						var stageStartPercentage = stage is LibGit2Sharp.Handlers.PackBuilderStage.Counting
							? startPercentage
							: deltafyingStartPercentage;
						var stageEndPercentage = stage is LibGit2Sharp.Handlers.PackBuilderStage.Counting
							? deltafyingStartPercentage
							: transferStartPercentage;

						statusOperation.ReportProgress(
							completed,
							total,
							startPercentage: stageStartPercentage,
							endPercentage: stageEndPercentage);
						return true;
					},
					OnPushTransferProgress = (completed, total, bytes) =>
					{
						statusOperation.ReportProgress(
							completed,
							total,
							startPercentage: transferStartPercentage,
							endPercentage: endPercentage);
						return true;
					},
				};

				var branch = repository.Branches[branchName];
				if (branch is null)
					return GitOperationResult.GenericError;

				if (!branch.IsTracking)
				{
					var origin = repository.Network.Remotes["origin"];
					repository.Branches.Update(
						branch,
						b => b.Remote = origin.Name,
						b => b.UpstreamBranch = branch.CanonicalName);
				}

				return await DoGitOperationAsync<GitOperationResult>(() =>
				{
					try
					{
						repository.Network.Push(branch, options);
					}
					catch (Exception ex)
					{
						return IsAuthorizationException(ex)
							? GitOperationResult.AuthorizationError
							: GitOperationResult.GenericError;
					}

					return GitOperationResult.Success;
				});
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex.Message);
				return IsAuthorizationException(ex)
					? GitOperationResult.AuthorizationError
					: GitOperationResult.GenericError;
			}
		}

		private static async Task HandlePullResultAsync(GitOperationResult result, string? message)
		{
			if (result is GitOperationResult.AuthorizationError)
			{
				await RequireGitAuthenticationAsync();
			}
			else if (result is GitOperationResult.GenericError or GitOperationResult.UncommittedChangesError)
			{
				var viewModel = new DynamicDialogViewModel()
				{
					TitleText = Strings.GitError.GetLocalizedResource(),
					SubtitleText = result is GitOperationResult.UncommittedChangesError
						? Strings.PullUncommittedChangesError.GetLocalizedResource()
						: message,
					CloseButtonText = Strings.Close.GetLocalizedResource(),
					DynamicButtons = DynamicDialogButtons.Cancel
				};
				var dialog = new DynamicDialog(viewModel);
				await dialog.TryShowAsync();
			}
		}

		private static ReturnResult ToReturnResult(GitOperationResult result)
		{
			return result is GitOperationResult.Success
				? ReturnResult.Success
				: ReturnResult.Failed;
		}

		private static void SetGitActionExecutionState(bool isExecuting)
		{
			MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
			{
				_implementation.IsExecutingGitAction = isExecuting;
			});
		}

		public static async Task RequireGitAuthenticationAsync()
		{
			var pending = true;
			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Accept", "application/json");
			client.DefaultRequestHeaders.Add("User-Agent", "Files App");

			JsonDocument? codeJsonContent;
			try
			{
				var codeResponse = await client.PostAsync(
					$"https://github.com/login/device/code?client_id={_clientId}&scope=repo",
					new StringContent(""));

				if (!codeResponse.IsSuccessStatusCode)
				{
					await DynamicDialogFactory.GetFor_GitHubConnectionError().TryShowAsync();
					return;
				}

				await using var codeStream = await codeResponse.Content.ReadAsStreamAsync();
				codeJsonContent = await JsonDocument.ParseAsync(codeStream);
				if (codeJsonContent is null)
				{
					await DynamicDialogFactory.GetFor_GitHubConnectionError().TryShowAsync();
					return;
				}
			}
			catch
			{
				await DynamicDialogFactory.GetFor_GitHubConnectionError().TryShowAsync();
				return;
			}

			var userCode = codeJsonContent.RootElement.GetProperty("user_code").GetString() ?? string.Empty;
			var deviceCode = codeJsonContent.RootElement.GetProperty("device_code").GetString() ?? string.Empty;
			var interval = codeJsonContent.RootElement.GetProperty("interval").GetInt32();
			var expiresIn = codeJsonContent.RootElement.GetProperty("expires_in").GetInt32();

			var loginCTS = new CancellationTokenSource();
			var viewModel = new GitHubLoginDialogViewModel(userCode, Strings.ConnectGitHubDescription.GetLocalizedResource(), loginCTS);

			var dialog = _dialogService.GetDialog(viewModel);
			var loginDialogTask = dialog.TryShowAsync();

			while (!loginCTS.Token.IsCancellationRequested && pending && expiresIn > 0)
			{
				try
				{
					var loginResponse = await client.PostAsync(
					$"https://github.com/login/oauth/access_token?client_id={_clientId}&device_code={deviceCode}&grant_type=urn:ietf:params:oauth:grant-type:device_code",
					new StringContent(""));

					expiresIn -= interval;

					if (!loginResponse.IsSuccessStatusCode)
					{
						dialog.Hide();
						break;
					}

					await using var loginStream = await loginResponse.Content.ReadAsStreamAsync();
					using var loginJsonContent = await JsonDocument.ParseAsync(loginStream);
					if (loginJsonContent is null)
					{
						dialog.Hide();
						break;
					}

					if (loginJsonContent.RootElement.TryGetProperty("error", out var error))
					{
						if (error.GetString() == "authorization_pending")
						{
							await Task.Delay(TimeSpan.FromSeconds(interval));
							continue;
						}

						dialog.Hide();
						break;
					}

					var token = loginJsonContent.RootElement.GetProperty("access_token").GetString();
					if (token is null)
						continue;

					pending = false;

					CredentialsHelpers.SavePassword(
						GIT_RESOURCE_NAME,
						GIT_RESOURCE_USERNAME,
						token);

					viewModel.Subtitle = Strings.AuthorizationSucceded.GetLocalizedResource();
					viewModel.LoginConfirmed = true;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex.Message);
					dialog.Hide();
					break;
				}
			}

			await loginDialogTask;
		}

		public static bool IsRepositoryEx([NotNullWhen(true)] string? path, [NotNullWhen(true)] out string? repoRootPath)
		{
			repoRootPath = path;

			var rootPath = SystemIO.Path.GetPathRoot(path);
			if (string.IsNullOrEmpty(rootPath))
				return false;

			var repositoryRootPath = GetGitRepositoryPath(path, rootPath);
			if (string.IsNullOrEmpty(repositoryRootPath))
				return false;

			if (IsRepoValid(repositoryRootPath))
			{
				repoRootPath = repositoryRootPath;
				return true;
			}

			return false;
		}

		public static GitItemModel GetGitInformationForItem(Repository repository, string path, bool getStatus = true, bool getCommit = true)
		{
			var rootRepoPath = repository.Info.WorkingDirectory;
			var relativePath = SystemIO.Path.GetRelativePath(rootRepoPath, path).Replace('\\', '/');
			if (relativePath == ".")
				relativePath = string.Empty;

			Commit? commit = null;
			if (getCommit)
			{
				commit = GetLastCommitForFile(repository, relativePath);
				//var commit = repository.Commits.QueryBy(relativePath).FirstOrDefault()?.Commit; // Considers renames but slow
			}

			ChangeKind? changeKind = null;
			string? changeKindHumanized = null;
			if (getStatus)
			{
				changeKind = ChangeKind.Unmodified;
				string[]? pathsToCompare = relativePath.Length == 0 ? null : [relativePath];
				foreach (TreeEntryChanges c in repository.Diff.Compare<TreeChanges>(
					repository.Commits.FirstOrDefault()?.Tree,
					DiffTargets.Index | DiffTargets.WorkingDirectory,
					pathsToCompare))
				{
					if (relativePath.Length == 0 ||
						c.Path.Equals(relativePath, StringComparison.Ordinal) ||
						(c.Path.Length > relativePath.Length &&
						c.Path.StartsWith(relativePath, StringComparison.Ordinal) &&
						c.Path[relativePath.Length] == '/'))
					{
						changeKind = c.Status;
						break;
					}
				}

				if (changeKind is not ChangeKind.Ignored)
				{
					changeKindHumanized = changeKind switch
					{
						ChangeKind.Added => Strings.Added.GetLocalizedResource(),
						ChangeKind.Deleted => Strings.Deleted.GetLocalizedResource(),
						ChangeKind.Modified => Strings.Modified.GetLocalizedResource(),
						ChangeKind.Untracked => Strings.Untracked.GetLocalizedResource(),
						_ => null,
					};
				}
			}

			var gitItemModel = new GitItemModel()
			{
				Status = changeKind,
				StatusHumanized = changeKindHumanized,
				LastCommitDate = commit?.Author.When,
				LastCommitMessage = commit?.MessageShort,
				LastCommitAuthor = commit?.Author.Name,
				LastCommitSha = commit?.Sha,
				Path = relativePath,
			};

			return gitItemModel;
		}

		// Remove saved credentails
		public static void RemoveSavedCredentials()
		{
			CredentialsHelpers.DeleteSavedPassword(GIT_RESOURCE_NAME, GIT_RESOURCE_USERNAME);
		}

		// Get saved credentails
		public static string GetSavedCredentials()
		{
			return CredentialsHelpers.GetPassword(GIT_RESOURCE_NAME, GIT_RESOURCE_USERNAME);
		}

		public static async Task InitializeRepositoryAsync(string? path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return;

			try
			{
				Repository.Init(path);
			}
			catch (LibGit2SharpException ex)
			{
				_logger.LogWarning(ex.Message);
				await DynamicDialogFactory.GetFor_GitCannotInitializeqRepositoryHere().TryShowAsync();
			}
		}

		// Method already moved into abstraction
		private static bool IsRepoValid(string path)
		{
			return SafetyExtensions.IgnoreExceptions(() => Repository.IsValid(path));
		}

		// Method already moved into abstraction
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

		// Method already moved into abstraction
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

		// Method already moved into abstraction
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

		// Method already moved into abstraction
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

		// Method already moved into abstraction
		private static bool IsAuthorizationException(Exception ex)
		{
			return
				ex.Message.Contains("status code: 401", StringComparison.OrdinalIgnoreCase) ||
				ex.Message.Contains("authentication replays", StringComparison.OrdinalIgnoreCase);
		}

		// Method already moved into abstraction
		private static async Task<T?> DoGitOperationAsync<T>(Func<object> payload, bool useSemaphore = false)
		{
			if (useSemaphore)
				await GitOperationSemaphore.WaitAsync();

			try
			{
				return (T)await Task.Run(payload);
			}
			finally
			{
				if (useSemaphore)
					GitOperationSemaphore.Release();
			}
		}

		/// <summary>
		/// Gets repository information from a GitHub URL.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public static (string RepoUrl, string RepoName) GetRepoInfo(string url)
		{
			var match = GitHubRepositoryRegex.Match(url);

			if (!match.Success)
				return (string.Empty, string.Empty);

			string platform = match.Groups["domain"].Value;
			string userOrOrg = match.Groups["user"].Value;
			string repoName = match.Groups["repo"].Value;

			string repoUrl = $"https://{platform}.com/{userOrOrg}/{repoName}";
			return (repoUrl, repoName);
		}

		/// <summary>
		/// Checks if the provided URL is a valid GitHub URL.
		/// </summary>
		/// <param name="url">The URL to validate.</param>
		/// <returns>True if the URL is a valid GitHub URL; otherwise, false.</returns>
		public static bool IsValidRepoUrl(string url)
		{
			return GitHubRepositoryRegex.IsMatch(url);
		}

		public static async Task CloneRepoAsync(string repoUrl, string repoName, string targetDirectory)
		{
			var statusOperation = new GitStatusCenterOperation(
				GitStatusCenterOperationKind.Clone,
				targetDirectory,
				canProvideProgress: true,
				operationTarget: repoName,
				isCancelable: true);
			var cancellationToken = statusOperation.CancellationToken;
			var result = ReturnResult.Failed;

			try
			{
				await Task.Run(() =>
				{
					var cloneOptions = new CloneOptions
					{
						FetchOptions =
						{
							OnTransferProgress = progress =>
							{
								cancellationToken.ThrowIfCancellationRequested();
								statusOperation.ReportProgress(
									progress.ReceivedObjects,
									progress.TotalObjects,
									endPercentage: 80,
									reportTotalItems: true);
								return true;
							},
							OnProgress = _ => !cancellationToken.IsCancellationRequested,
						},
						OnCheckoutProgress = (path, completed, total) =>
						{
							cancellationToken.ThrowIfCancellationRequested();
							statusOperation.ReportProgress(
								completed,
								total,
								path,
								80,
								100);
						},
					};

					Repository.Clone(repoUrl, targetDirectory, cloneOptions);
				}, cancellationToken);
				result = ReturnResult.Success;
			}
			catch (Exception ex)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					result = ReturnResult.Cancelled;
				}
				else
				{
					UIHelpers.CloseAllDialogs();
					await Task.Delay(500);
					await DynamicDialogFactory.ShowFor_CannotCloneRepo(ex.Message);
				}
			}
			finally
			{
				statusOperation.Complete(result);
			}
		}

		// Method already moved into abstraction
		[GeneratedRegex(@"^(?:https?:\/\/)?(?:www\.)?(?<domain>github|gitlab)\.com\/(?<user>[^\/]+)\/(?<repo>[^\/]+?)(?=\.git|\/|$)(?:\.git)?(?:\/)?", RegexOptions.IgnoreCase)]
		private static partial Regex GitHubRepositoryRegex { get; }

		#endregion
	}
}
