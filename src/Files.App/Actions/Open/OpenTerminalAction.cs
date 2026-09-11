// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.System.Com;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal partial class OpenTerminalAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		// DelegationTerminal CLSIDs registered by Windows Terminal. When one of these
		// is the user's default, launching wt.exe gives the user's chosen profile and
		// supports multi-tab. Source: microsoft/terminal policies/WindowsTerminal.admx.
		private static readonly Guid[] WindowsTerminalDelegationClsids =
		[
			new("E12CFF52-A866-4C77-9A90-F570A7AA2C6B"), // Windows Terminal (stable)
			new("86633F1F-6454-40EC-89CE-DA4EBA977EE2"), // Windows Terminal Preview
		];

		public virtual string Label
			=> Strings.OpenTerminal.GetLocalizedResource();

		public virtual string Description
			=> Strings.OpenTerminalDescription.GetLocalizedResource();

		public virtual ActionCategory Category
			=> ActionCategory.Open;

		public virtual HotKey HotKey
			=> new(Keys.Oem3, KeyModifiers.Ctrl);

		public RichGlyph Glyph
			=> new("\uE756");

		public virtual bool IsExecutable
			=> GetIsExecutable();

		public virtual bool IsAccessibleGlobally
			=> true;

		public OpenTerminalAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var paths = GetPaths();
			if (paths.Length is 0)
				return Task.CompletedTask;

			var terminalStartInfo = GetProcessStartInfo(paths);
			if (terminalStartInfo is null)
				return Task.CompletedTask;

			MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
			{
				try
				{
					Process.Start(terminalStartInfo);
				}
				catch (Win32Exception)
				{
				}
			});

			return Task.CompletedTask;
		}

		protected virtual ProcessStartInfo? GetProcessStartInfo(string[] paths)
		{
			if (paths.Length is 0)
				return null;

			if (IsWindowsTerminalDefault())
			{
				var startInfo = new ProcessStartInfo
				{
					FileName = "wt.exe",
					UseShellExecute = false,
					ArgumentList = { "-d", paths[0] }
				};

				for (int i = 1; i < paths.Length; i++)
				{
					startInfo.ArgumentList.Add(";");
					startInfo.ArgumentList.Add("nt");
					startInfo.ArgumentList.Add("-d");
					startInfo.ArgumentList.Add(paths[i]);
				}

				return startInfo;
			}

			// Launch cmd.exe when Windows Terminal is not the effective default host.
			return new()
			{
				FileName = "cmd.exe",
				WorkingDirectory = paths[0],
				UseShellExecute = true
			};
		}

		private static bool IsWindowsTerminalDefault()
		{
			try
			{
				using var key = Registry.CurrentUser.OpenSubKey(@"Console\%%Startup");
				var consoleClsid = Guid.TryParse(key?.GetValue("DelegationConsole") as string, out var consoleClsidResult) ? consoleClsidResult : Guid.Empty;
				var terminalClsid = Guid.TryParse(key?.GetValue("DelegationTerminal") as string, out var terminalClsidResult) ? terminalClsidResult : Guid.Empty;

				// Windows treats either missing or zero CLSID as "Let Windows decide".
				if (consoleClsid == Guid.Empty || terminalClsid == Guid.Empty)
				{
					// Windows 11 22H2 introduced Terminal as the automatic default.
					if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621))
						return false;

					// Match conhost's IDefaultTerminalMarker probe on the stable console server.
					// Source: microsoft/terminal src/server/IoDispatchers.cpp.
					consoleClsid = new("2EACA947-7F5F-4CFA-BA87-8F7FBEEFBE69");
					Guid markerIid = new("746E6BC0-AB05-4E38-AB14-71E86763141F");
					return ComHelpers.CanCreateInstance(consoleClsid, CLSCTX.CLSCTX_LOCAL_SERVER, markerIid);
				}

				return consoleClsid != new Guid("B23D10C0-E52E-411E-9D5B-C09FDF709C7D")
					&& WindowsTerminalDelegationClsids.Contains(terminalClsid);
			}
			catch
			{
			}

			return false;
		}

		protected virtual string[] GetPaths()
		{
			if (context.HasSelection)
			{
				return context.SelectedItems!
					.Where(item => item.PrimaryItemAttribute is StorageItemTypes.Folder && !item.IsArchive)
					.Select(item => item.ItemPath!)
					.ToArray();
			}
			else if (context.Folder is not null)
			{
				return [context.Folder.ItemPath!];
			}

			return [];
		}

		private bool GetIsExecutable()
		{
			if (context.PageType is ContentPageTypes.None or ContentPageTypes.Home or ContentPageTypes.RecycleBin or ContentPageTypes.ZipFolder or ContentPageTypes.ReleaseNotes or ContentPageTypes.Settings)
				return false;

			var isFolderNull = context.Folder is null;

			if (!context.HasSelection && isFolderNull)
				return false;

			if (context.SelectedItems.Count > Constants.Actions.MaxSelectedItems)
				return false;

			return context.HasSelection
				? context.SelectedItems.Any(item => item.PrimaryItemAttribute is StorageItemTypes.Folder && !item.IsArchive)
				: !isFolderNull;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.Folder):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
