// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System.IO;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using static Files.App.Helpers.InteropHelpers;

namespace Files.App
{
	/// <summary>
	/// Represents the base entry point of the Files app.
	/// </summary>
	/// <remarks>
	/// Gets called at the first time when the app launched or activated.
	/// </remarks>
	internal sealed class Program
	{
		private const uint CWMO_DEFAULT = 0;
		private const uint INFINITE = 0xFFFFFFFF;

		public static Semaphore? Pool { get; set; }

		static Program()
		{
			var pool = new Semaphore(0, 1, $"Files-{AppLifecycleHelper.AppEnvironment}-Instance", out var isNew);

			if (!isNew)
			{
				// Resume cached instance
				pool.Release();

				// Redirect to the main process
				var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
				var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());
				RedirectActivationTo(instance, AppInstance.GetCurrent().GetActivatedEventArgs());

				// Kill the current process
				Environment.Exit(0);
			}

			pool.Dispose();
		}

		/// <summary>
		/// Initializes the process; the entry point of the process.
		/// </summary>
		/// <remarks>
		/// <see cref="Main"/> cannot be declared to be async because this prevents Narrator from reading XAML elements in a WinUI app.
		/// </remarks>
		[STAThread]
		private static void Main()
		{
			WinRT.ComWrappersSupport.InitializeComWrappers();

			Server.AppInstanceMonitor.StartMonitor(Environment.ProcessId);

			var OpenTabInExistingInstance = ApplicationData.Current.LocalSettings.Values.Get("OpenTabInExistingInstance", true);
			var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

			if (activatedArgs.Data is ICommandLineActivatedEventArgs cmdLineArgs)
			{
				var operation = cmdLineArgs.Operation;
				var cmdLineString = operation.Arguments;
				var parsedCommands = CommandLineParser.ParseUntrustedCommands(cmdLineString);

				if (parsedCommands is not null)
				{
					foreach (var command in parsedCommands)
					{
						switch (command.Type)
						{
							case ParsedCommandType.ExplorerShellCommand:
								if (!Constants.UserEnvironmentPaths.ShellPlaces.ContainsKey(command.Payload.ToUpperInvariant()))
								{
									OpenShellCommandInExplorer(command.Payload, Environment.ProcessId);

									// Exit
									return;
								}

								break;

							default:
								break;
						}
					}
				}

				// Always open a new instance for OpenDialog, never open new instance for "-Tag" command
				if (parsedCommands is null || !parsedCommands.Any(x => x.Type == ParsedCommandType.OutputPath) &&
					(OpenTabInExistingInstance || parsedCommands.Any(x => x.Type == ParsedCommandType.TagFiles)))
				{
					var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
					var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());

					if (!instance.IsCurrent)
					{
						RedirectActivationTo(instance, activatedArgs);
						return;
					}
				}
			}
			else if (activatedArgs.Data is ILaunchActivatedEventArgs tileArgs)
			{
				if (tileArgs.Arguments is not null &&
					!tileArgs.Arguments.Contains($"files.exe", StringComparison.OrdinalIgnoreCase) &&
					FileExtensionHelpers.IsExecutableFile(tileArgs.Arguments))
				{
					if (File.Exists(tileArgs.Arguments))
					{
						OpenFileFromTile(tileArgs.Arguments);
						return;
					}
				}
			}

			if (OpenTabInExistingInstance)
			{
				if (activatedArgs.Data is ILaunchActivatedEventArgs launchArgs)
				{
					var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
					var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());
					if (!instance.IsCurrent && !string.IsNullOrWhiteSpace(launchArgs.Arguments))
					{
						RedirectActivationTo(instance, activatedArgs);
						return;
					}
				}
				else if (activatedArgs.Data is IProtocolActivatedEventArgs protocolArgs)
				{
					var parsedArgs = protocolArgs.Uri.Query.TrimStart('?').Split('=');
					if ((parsedArgs.Length == 2 && parsedArgs[0] == "cmd") ||
						parsedArgs.Length == 1) // Treat Win+E & Open file location as command line launch
					{
						var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
						var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());
						if (!instance.IsCurrent)
						{
							RedirectActivationTo(instance, activatedArgs);
							return;
						}
					}
				}
				else if (activatedArgs.Data is IFileActivatedEventArgs)
				{
					var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
					var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());
					if (!instance.IsCurrent)
					{
						RedirectActivationTo(instance, activatedArgs);
						return;
					}
				}
			}

			var currentInstance = AppInstance.FindOrRegisterForKey((-Environment.ProcessId).ToString());
			if (currentInstance.IsCurrent)
				currentInstance.Activated += OnActivated;

			ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -Environment.ProcessId;

			Application.Start((p) =>
			{
				var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
				SynchronizationContext.SetSynchronizationContext(context);

				_ = new App();
			});
		}

		/// <summary>
		/// Gets invoked when the application is activated.
		/// </summary>
		private static async void OnActivated(object? sender, AppActivationArguments args)
		{
			// WINUI3: Verify if needed or OnLaunched is called
			if (App.Current is App thisApp)
				await thisApp.OnActivatedAsync(args);
		}

		/// <summary>
		/// Redirects the activation to the main process.
		/// </summary>
		/// <remarks>
		/// Redirects on another thread and uses a non-blocking wait method to wait for the redirection to complete.
		/// </remarks>
		public static void RedirectActivationTo(AppInstance keyInstance, AppActivationArguments args)
		{
			IntPtr eventHandle = CreateEvent(IntPtr.Zero, true, false, null);

			Task.Run(() =>
			{
				keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
				SetEvent(eventHandle);
			});

			_ = CoWaitForMultipleObjects(
				CWMO_DEFAULT,
				INFINITE,
				1,
				new IntPtr[] { eventHandle },
				out uint handleIndex);
		}

		public static void OpenShellCommandInExplorer(string shellCommand, int pid)
		{
			Win32API.OpenFolderInExistingShellWindow(shellCommand);
		}

		public static void OpenFileFromTile(string filePath)
		{
			IntPtr eventHandle = CreateEvent(IntPtr.Zero, true, false, null);

			Task.Run(() =>
			{
				LaunchHelper.LaunchAppAsync(filePath, null, null).Wait();
				SetEvent(eventHandle);
			});

			_ = CoWaitForMultipleObjects(
				CWMO_DEFAULT,
				INFINITE,
				1,
				new IntPtr[] { eventHandle },
				out uint handleIndex);
		}
	}
}
