// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;
using System.Text;
using Windows.Storage;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal partial class OpenTerminalAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		// DelegationTerminal CLSIDs registered by Windows Terminal. When one of these
		// is the user's default, launching wt.exe gives the user's chosen profile and
		// supports multi-tab. Source: microsoft/terminal policies/WindowsTerminal.admx.
		private static readonly string[] WindowsTerminalDelegationClsids =
		[
			"{E12CFF52-A866-4C77-9A90-F570A7AA2C6B}", // Windows Terminal (stable)
			"{86633F1F-6454-40EC-89CE-DA4EBA977EE2}", // Windows Terminal Preview
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
				var path = paths[0] + (paths[0].EndsWith('\\') ? "\\" : "");

				var args = new StringBuilder($"-d \"{path}\"");
				for (int i = 1; i < paths.Length; i++)
				{
					path = paths[i] + (paths[i].EndsWith('\\') ? "\\" : "");
					args.Append($" ; nt -d \"{path}\"");
				}

				return new()
				{
					FileName = "wt.exe",
					Arguments = args.ToString(),
					UseShellExecute = true
				};
			}

			// Fall back to launching cmd.exe; the system hosts it in whichever
			// terminal the user has configured (Console Host, or "Let Windows decide").
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
				if (key?.GetValue("DelegationTerminal") is string clsid)
					return WindowsTerminalDelegationClsids.Contains(clsid, StringComparer.OrdinalIgnoreCase);
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
					.Where(item => item.PrimaryItemAttribute is StorageItemTypes.Folder)
					.Select(item => item.ItemPath)
					.ToArray();
			}
			else if (context.Folder is not null)
			{
				return [context.Folder.ItemPath];
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

			return context.SelectedItems.Any(item => item.PrimaryItemAttribute is StorageItemTypes.Folder) || !isFolderNull;
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
