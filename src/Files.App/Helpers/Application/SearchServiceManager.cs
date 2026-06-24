// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using Windows.ApplicationModel;

namespace Files.App.Helpers.Application;

/// <summary>
/// Manages the lifecycle of the files-search-service sidecar process.
///
/// In packaged (Store/sideload) builds the service is declared in the MSIX
/// manifest as a <c>desktop6:Service</c> and installed by Windows at package
/// install time. SCM starts it automatically at login — no UAC prompt, no
/// HKCU\Run entry needed. Files.App is a pure gRPC client.
///
/// In unpackaged dev builds (no SCM registration) the service is started
/// directly as a child process and a HKCU\Run entry is written so it
/// survives reboots during development.
/// </summary>
internal static class SearchServiceManager
{
	private const string ServiceName = "FilesSearchService";
	private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
	private const string RunValueName = "FilesSearchService";
	private const string ExeName = "files-search-service.exe";
	private const string ProcessName = "files-search-service";

	public static void EnsureRunning()
	{
#if DEBUG
		// Debug manifest omits desktop6:Service so VS can sideload without admin.
		// Always spawn the exe directly; SCM has no registration for it.
		EnsureProcessRunning();
#else
		if (IsPackaged())
			EnsureServiceRunning();
		else
			EnsureProcessRunning();
#endif
	}

	public static void RemoveStartupRegistration()
	{
		// Packaged Release builds are managed by SCM — nothing to clean up.
#if !DEBUG
		if (IsPackaged())
			return;
#endif
		using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
		key?.DeleteValue(RunValueName, throwOnMissingValue: false);
	}

	// Packaged mode: ask SCM to start the service if it isn't already running.
	private static void EnsureServiceRunning()
	{
		try
		{
			using var sc = new ServiceController(ServiceName);
			if (sc.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.Paused)
				sc.Start();
		}
		catch (InvalidOperationException)
		{
			// Service not installed yet (e.g. first run before SCM has processed
			// the manifest). Nothing to do — SCM will start it on next login.
		}
	}

	// Dev / unpackaged mode: start the exe directly and register HKCU\Run.
	private static void EnsureProcessRunning()
	{
		var exePath = ResolveExePath();
		if (exePath is null || !File.Exists(exePath))
			return;

		// In dev mode the service uses TCP loopback (port 50299) instead of a
		// named pipe — named pipes require ACL privileges we don't have outside SCM.
		// Setting FILES_SEARCH_SERVICE_URL makes both this process (the gRPC client)
		// and the child service process (which inherits the env) use TCP.
		Environment.SetEnvironmentVariable("FILES_SEARCH_SERVICE_URL", "http://localhost:50299");

		RegisterStartup(exePath);
		LaunchIfNotRunning(exePath);
	}

	private static void RegisterStartup(string exePath)
	{
		using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
		key.SetValue(RunValueName, $"\"{exePath}\"");
	}

	private static void LaunchIfNotRunning(string exePath)
	{
		// Kill any stale instances first — in dev mode the HKCU\Run entry or a
		// previous debug session may have left a process bound to the TCP
		// loopback port (FILES_SEARCH_SERVICE_URL), which causes
		// AddressInUseException on the next start.
		foreach (var stale in Process.GetProcessesByName(ProcessName))
		{
			try { stale.Kill(entireProcessTree: true); stale.WaitForExit(2000); }
			catch { }
		}

		Process.Start(new ProcessStartInfo
		{
			FileName = exePath,
			UseShellExecute = false,
			CreateNoWindow = true,
		});
	}

	private static string? ResolveExePath()
	{
		try
		{
			return Path.Combine(Package.Current.InstalledLocation.Path, "SearchService", ExeName);
		}
		catch
		{
			return Path.Combine(AppContext.BaseDirectory, "SearchService", ExeName);
		}
	}

	private static bool IsPackaged()
	{
		try
		{
			_ = Package.Current;
			return true;
		}
		catch
		{
			return false;
		}
	}
}
