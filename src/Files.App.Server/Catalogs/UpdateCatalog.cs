// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Foundation;
using WinRT.Interop;
using Windows.Services.Store;
using Windows.Storage;

namespace Files.App.Server;

internal static class UpdateCatalog
{
	private static readonly string environment = "cd_app_env_placeholder".ToLowerInvariant();
	private static readonly HttpClient client = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(3) });
	private static readonly TimeSpan interval = environment.Contains("preview", StringComparison.Ordinal) ? TimeSpan.FromHours(2) : TimeSpan.FromHours(5);
	private static readonly CancellationTokenSource stopping = new();
	private static readonly Lazy<Task> initialization = new(InitializeCoreAsync);
	internal static readonly CachedTask<bool> ReleaseNotes = new(CheckReleaseNotesAsync, TimeSpan.FromHours(2));
	internal static readonly CachedTask<bool> SideloadUpdate = new(CheckSideloadAsync, interval);
	internal static readonly CachedTask<IReadOnlyList<StorePackageUpdate>> StoreUpdates = new(CheckStoreAsync, interval);
	private static readonly CachedTask<bool> launcher = new(async () =>
	{
		await CheckAndUpdateFilesLauncherAsync();
		return true;
	}, TimeSpan.FromDays(1));

	internal static Task InitializeAsync() => initialization.Value;

	private static async Task InitializeCoreAsync()
	{
		await RefreshAsync();
		_ = RunAsync();
	}

	private static async Task RunAsync()
	{
		using var timer = new PeriodicTimer(interval);
		try
		{
			while (await timer.WaitForNextTickAsync(stopping.Token))
				await RefreshAsync();
		}
		catch (OperationCanceledException) when (stopping.IsCancellationRequested)
		{
		}
	}

	private static async Task RefreshAsync()
	{
		await SafetyExtensions.IgnoreExceptions(ReleaseNotes.GetAsync, Program.Logger);
		if (environment is "sideloadstable" or "sideloadpreview")
			await SafetyExtensions.IgnoreExceptions(SideloadUpdate.GetAsync, Program.Logger);
		else if (environment is "storestable" or "storepreview")
			await SafetyExtensions.IgnoreExceptions(StoreUpdates.GetAsync, Program.Logger);
		ServerEvents.Notify(ServerChangeNames.Updates);
	}

	internal static void Invalidate()
	{
		StoreUpdates.Invalidate();
		SideloadUpdate.Invalidate();
	}

	internal static Task UpdateLauncherAsync() => launcher.GetAsync();
	internal static void Stop() => stopping.Cancel();

	private static async Task<IReadOnlyList<StorePackageUpdate>> CheckStoreAsync()
	{
		IAsyncOperation<IReadOnlyList<StorePackageUpdate>>? operation = null;
		await ServerHost.InvokeAsync(() =>
		{
			var context = StoreContext.GetDefault();
			InitializeWithWindow.Initialize(context, ServerHost.GetDialogWindowHandle());
			operation = context.GetAppAndOptionalStorePackageUpdatesAsync();
		});
		return (await operation!).ToList().AsReadOnly();
	}

	private static async Task<bool> CheckReleaseNotesAsync()
	{
		var version = Package.Current.Id.Version;
		using var response = await client.GetAsync($"https://files.community/blog/posts/v{version.Major}-{version.Minor}-{version.Build}?minimal", HttpCompletionOption.ResponseHeadersRead);
		return response.IsSuccessStatusCode;
	}

	private static async Task<bool> CheckSideloadAsync()
	{
		var name = Package.Current.Id.Name;
		var url = name switch
		{
			"Files" => "https://cdn.files.community/files/stable/Files.Package.appinstaller",
			"FilesPreview" => "https://cdn.files.community/files/preview/Files.Package.appinstaller",
			_ => null,
		};
		if (url is null)
			return false;
		await using var stream = await client.GetStreamAsync(url);
		var document = await XDocument.LoadAsync(stream, LoadOptions.None, stopping.Token);
		var root = document.Root;
		var bundle = root?.Element(root.Name.Namespace + "MainBundle");
		var installed = Package.Current.Id.Version;
		return bundle?.Attribute("Name")?.Value == name &&
			Version.TryParse(root?.Attribute("Version")?.Value, out var available) &&
			available > new Version(installed.Major, installed.Minor, installed.Build, installed.Revision);
	}

	private static async Task CheckAndUpdateFilesLauncherAsync()
	{
		try
		{
			var destFolderPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, "Files");
			var destExeFilePath = Path.Combine(destFolderPath, "Files.App.Launcher.exe");
			var branchFilePath = Path.Combine(destFolderPath, "Branch.txt");

			// If Files.App.Launcher.exe doesn't exist, no need to update it.
			if (!File.Exists(destExeFilePath))
				return;

			// Check if the launcher file is associated with the current branch of the app.
			if (File.Exists(branchFilePath))
			{
				try
				{
					var branch = await File.ReadAllTextAsync(branchFilePath, Encoding.UTF8);
					if (!string.Equals(branch.Trim(), "files-dev", StringComparison.OrdinalIgnoreCase))
						return;
				}
				catch
				{
				}
			}
			else
			{
				try
				{
					// Create branch file for users updating from versions earlier than v4.0.20.
					await File.WriteAllTextAsync(branchFilePath, "files-dev", Encoding.UTF8);
				}
				catch
				{
				}
			}

			var srcExeFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/FilesOpenDialog/Files.App.Launcher.exe"));
			var destFolder = await StorageFolder.GetFolderFromPathAsync(destFolderPath);

			await srcExeFile.CopyAsync(destFolder, "Files.App.Launcher.exe", NameCollisionOption.ReplaceExisting);

			Program.Logger.LogInformation("Files.App.Launcher updated.");
		}
		catch (Exception ex)
		{
			Program.Logger?.LogError(ex, ex.Message);
			return;
		}
	}
}
