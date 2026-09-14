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
			new([0x52, 0xFF, 0x2C, 0xE1, 0x66, 0xA8, 0x77, 0x4C, 0x9A, 0x90, 0xF5, 0x70, 0xA7, 0xAA, 0x2C, 0x6B]), // Windows Terminal (stable)
			new([0x1F, 0x3F, 0x63, 0x86, 0x54, 0x64, 0xEC, 0x40, 0x89, 0xCE, 0xDA, 0x4E, 0xBA, 0x97, 0x7E, 0xE2]), // Windows Terminal Preview
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

		private static unsafe bool IsWindowsTerminalDefault()
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
					consoleClsid = new([0x47, 0xA9, 0xAC, 0x2E, 0x5F, 0x7F, 0xFA, 0x4C, 0xBA, 0x87, 0x8F, 0x7F, 0xBE, 0xEF, 0xBE, 0x69]);
					Guid markerIid = new([0xC0, 0x6B, 0x6E, 0x74, 0x05, 0xAB, 0x38, 0x4E, 0xAB, 0x14, 0x71, 0xE8, 0x67, 0x63, 0x14, 0x1F]);
					var result = PInvoke.CoCreateInstance(&consoleClsid, null, CLSCTX.CLSCTX_LOCAL_SERVER, &markerIid, out var marker);
					return result.Succeeded && marker is not null;
				}

				return consoleClsid != new Guid([0xC0, 0x10, 0x3D, 0xB2, 0x2E, 0xE5, 0x1E, 0x41, 0x9D, 0x5B, 0xC0, 0x9F, 0xDF, 0x70, 0x9C, 0x7D])
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
