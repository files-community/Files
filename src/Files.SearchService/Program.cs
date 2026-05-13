// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.SearchService.Grpc;
using Files.SearchService.Index;
using Files.SearchService.Throttle;
using Files.SearchService.Watch;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;

namespace Files.SearchService;

/// <summary>
/// Entry point. Runs as a Windows Service when started by SCM;
/// falls back to a console process for dev / unpackaged mode.
/// </summary>
internal static class Program
{
	// Named pipe used in production (SCM/SYSTEM mode).
	internal static string PipeName =>
		Environment.GetEnvironmentVariable("FILES_SEARCH_PIPE") ?? "files-search";

	// TCP port used in dev/console mode (avoids named-pipe ACL issues).
	internal const int DevTcpPort = 50299;

	internal static async Task Main(string[] args)
	{
		if (!Environment.UserInteractive)
		{
			// Started by SCM — hand off to ServiceBase.
			ServiceBase.Run(new SearchWindowsService());
			return;
		}

		// Dev / console mode — run until Ctrl+C.
		using var cts = new CancellationTokenSource();
		Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
		try
		{
			await RunAsync(cts.Token);
		}
		catch (Exception ex) when (!cts.IsCancellationRequested)
		{
			if (IsNamedPipeConflict(ex))
			{
				Console.Error.WriteLine(
					$"[error] Named pipe '{PipeName}' is already in use — the Windows service may be running. " +
					$"Set FILES_SEARCH_PIPE to a different name to run a dev instance alongside it. " +
					$"Example:  $env:FILES_SEARCH_PIPE = 'files-search-dev'");
			}

			var log = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Files", "search-service-crash.log");
			Directory.CreateDirectory(Path.GetDirectoryName(log)!);
			await File.WriteAllTextAsync(log, ex.ToString());
			Console.Error.WriteLine($"[crash] {ex}");
			throw;
		}
	}

	// Walk the exception chain looking for the signature Kestrel emits when a
	// named pipe is already held by another process (typically the SCM service):
	// AddressInUseException wrapping UnauthorizedAccessException.
	private static bool IsNamedPipeConflict(Exception ex)
	{
		for (var e = ex; e is not null; e = e.InnerException)
		{
			if (e.Message.Contains(PipeName, StringComparison.OrdinalIgnoreCase) &&
				e.InnerException is UnauthorizedAccessException)
				return true;
		}
		return false;
	}

	internal static async Task RunAsync(CancellationToken stopping)
	{
		// NOTE: ApplyBackgroundPriority is deferred until after the initial
		// bootstrap finishes. PROCESS_MODE_BACKGROUND_BEGIN throttles ALL I/O
		// (including reading index.bin) to IDLE priority, which turned a 15-second
		// index load into multiple minutes. We're a good citizen *after* we're useful.
		ProcessThrottle.StartPolling();

		try
		{
			var root        = ResolveRoot();
			var indexDir    = ResolveIndexDir();
			var persistPath = Path.Combine(indexDir, "index.bin");

			var index = new FileIndex();

			// Start the gRPC server before bootstrapping so the named pipe is
			// open immediately. Clients that connect during indexing see
			// IsIndexing=true and get empty search results until ready.
			var builder = WebApplication.CreateBuilder();
			builder.Services.AddGrpc();
			builder.Services.AddSingleton(index);

			if (Environment.UserInteractive)
			{
				// Dev / console mode: use TCP loopback — avoids named-pipe ACL
				// restrictions that reject PipeSecurity from non-elevated accounts.
				builder.WebHost.ConfigureKestrel(o =>
					o.ListenLocalhost(DevTcpPort, lo => lo.Protocols = HttpProtocols.Http2));
			}
			else
			{
				// SCM service mode (SYSTEM): named pipe with explicit DACL so the
				// user-session client can connect across the account boundary.
				builder.Services.Configure<NamedPipeTransportOptions>(o =>
				{
					o.CurrentUserOnly = false;
					o.PipeSecurity = CreatePipeSecurity();
				});
				builder.WebHost.ConfigureKestrel(o =>
					o.ListenNamedPipe(PipeName, lo =>
						lo.Protocols = HttpProtocols.Http2));
			}

			var app = builder.Build();
			app.MapGrpcService<SearchGrpcService>();

			await app.StartAsync(stopping);

			// Bootstrap runs after the pipe is listening so searches can
			// fall back to legacy while the index builds.
			await IndexBootstrapper.BootstrapAsync(index, root, indexDir, stopping);

			// Now that the index is loaded and queries are fast, drop to background
			// I/O priority so the watcher and periodic persistence don't compete with
			// foreground apps. The startup load is where we needed full priority.
			ProcessThrottle.ApplyBackgroundPriority();

			using var watcher = new ChangeWatcher(root, index);

			// On buffer overflow: events were lost — stop, re-index, restart.
			// Guard against concurrent overflow triggers.
			int _rebuilding = 0;
			watcher.Overflow += () =>
			{
				if (Interlocked.CompareExchange(ref _rebuilding, 1, 0) != 0) return;
				_ = Task.Run(async () =>
				{
					try
					{
						watcher.Stop();
						await IndexBootstrapper.BootstrapAsync(index, root, indexDir, stopping);
						watcher.Start();
					}
					catch (OperationCanceledException) { }
					catch (Exception ex) { Console.Error.WriteLine($"[watcher] re-index failed: {ex.Message}"); }
					finally { Interlocked.Exchange(ref _rebuilding, 0); }
				}, stopping);
			};

			watcher.Start();

			// Persist watcher changes back to disk every 5 minutes so restarts are fast.
			using var saveTimer = new Timer(_ =>
			{
				if (!index.IsDirty || index.IsIndexing) return;
				var records = index.GetAllRecords();
				index.MarkClean();
				_ = IndexPersistence.SaveAsync(persistPath, records, stopping)
					.ContinueWith(
						t => Console.Error.WriteLine($"[persist] periodic save failed: {t.Exception?.GetBaseException().Message}"),
						TaskContinuationOptions.OnlyOnFaulted);
			}, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

			await app.WaitForShutdownAsync(stopping);
		}
		finally
		{
			ProcessThrottle.StopPolling();
		}
	}

	private static string ResolveRoot()
	{
		var configured = Environment.GetEnvironmentVariable("FILES_SEARCH_ROOT");
		if (configured is not null) return configured;

		// When running as LocalSystem the UserProfile folder resolves to the system
		// service profile (C:\Windows\system32\config\systemprofile), not a real user
		// home. Detect this by checking for "system32\config" in the path and fall back
		// to the drive root so USN enumeration covers the whole volume. Per-query scope
		// filtering via scopePaths narrows results to each user's view at search time.
		var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (userProfile.Contains(@"system32\config", StringComparison.OrdinalIgnoreCase))
			return Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? @"C:\";

		return userProfile;
	}

	private static string ResolveIndexDir() =>
		Environment.GetEnvironmentVariable("FILES_SEARCH_INDEX_DIR")
		?? Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Files", "search-index");

	/// <summary>
	/// Builds the named pipe DACL for the LocalSystem → user-session topology.
	///
	/// Grant:
	///   SYSTEM             — FullControl              (service owns the pipe)
	///   Administrators     — FullControl              (admin diagnostics / tooling)
	///   AuthenticatedUsers — ReadWrite | Synchronize  (Files.App runs as the logged-in user)
	///
	/// Synchronize is required because <c>NamedPipeClientStream</c> with
	/// <c>PipeOptions.Asynchronous</c> waits on the pipe handle for overlapped I/O.
	/// Granting only ReadWrite throws UnauthorizedAccessException on ConnectAsync
	/// from a user-context client to a LocalSystem-owned pipe.
	///
	/// Deny entries are intentionally absent; the default implicit deny covers
	/// unauthenticated / anonymous callers.
	/// </summary>
	private static PipeSecurity CreatePipeSecurity()
	{
		var security = new PipeSecurity();

		security.AddAccessRule(new PipeAccessRule(
			new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
			PipeAccessRights.FullControl,
			AccessControlType.Allow));

		security.AddAccessRule(new PipeAccessRule(
			new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
			PipeAccessRights.FullControl,
			AccessControlType.Allow));

		security.AddAccessRule(new PipeAccessRule(
			new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
			PipeAccessRights.ReadWrite | PipeAccessRights.Synchronize,
			AccessControlType.Allow));

		return security;
	}
}
