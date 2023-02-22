using Files.App.CommandLine;
using Files.App.Helpers;
using Files.App.Shell;
using Files.Core.Helpers;
using Files.Core.Extensions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using static UWPToWinAppSDKUpgradeHelpers.InteropHelpers;

namespace Files.App
{
	internal class Program
	{
		// Note: We can't declare Main to be async because in a WinUI app
		// This prevents Narrator from reading XAML elements
		// https://github.com/microsoft/WindowsAppSDK-Samples/blob/main/Samples/AppLifecycle/Instancing/cs-winui-packaged/CsWinUiDesktopInstancing/CsWinUiDesktopInstancing/Program.cs
		// STAThread has no effect if main is async, needed for Clipboard
		[STAThread]
		private static void Main()
		{
			WinRT.ComWrappersSupport.InitializeComWrappers();

			var proc = Process.GetCurrentProcess();
			var alwaysOpenNewInstance = ApplicationData.Current.LocalSettings.Values.Get("AlwaysOpenANewInstance", false);
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
								if (!CommonPaths.ShellPlaces.ContainsKey(command.Payload.ToUpperInvariant()))
								{
									OpenShellCommandInExplorer(command.Payload, proc.Id);

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
					(!alwaysOpenNewInstance || parsedCommands.Any(x => x.Type == ParsedCommandType.TagFiles)))
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

			if (activatedArgs.Data is ILaunchActivatedEventArgs tileArgs)
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

			if (!alwaysOpenNewInstance)
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
					if (parsedArgs.Length == 2 && parsedArgs[0] == "cmd") // Treat as command line launch
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

			var currentInstance = AppInstance.FindOrRegisterForKey((-proc.Id).ToString());
			if (currentInstance.IsCurrent)
			{
				currentInstance.Activated += OnActivated;
			}
			ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -proc.Id;

			Application.Start((p) =>
			{
				var context = new DispatcherQueueSynchronizationContext(
					DispatcherQueue.GetForCurrentThread());
				SynchronizationContext.SetSynchronizationContext(context);
				new App();
			});
		}

		private static void OnActivated(object? sender, AppActivationArguments args)
		{
			if (App.Current is App thisApp)
			{
				// WINUI3: Verify if needed or OnLaunched is called
				thisApp.OnActivated(args);
			}
		}

		private const uint CWMO_DEFAULT = 0;
		private const uint INFINITE = 0xFFFFFFFF;

		// Do the redirection on another thread, and use a non-blocking wait method to wait for the redirection to complete
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
			=> Win32API.OpenFolderInExistingShellWindow(shellCommand);

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
