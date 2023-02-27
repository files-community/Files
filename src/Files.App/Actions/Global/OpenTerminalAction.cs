using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Files.App.Actions
{
	internal class OpenTerminalAction : IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "OpenTerminal".GetLocalizedResource();

		public virtual HotKey HotKey { get; } = new((VirtualKey)192, VirtualKeyModifiers.Control);

		public RichGlyph Glyph { get; } = new RichGlyph("\uE756");

		public Task ExecuteAsync()
		{
			var terminalStartInfo = GetProcessStartInfo();
			if (terminalStartInfo is not null)
			{
				try
				{
					App.Window.DispatcherQueue.TryEnqueue(() => Process.Start(terminalStartInfo));
				}
				catch (OperationCanceledException)
				{
				}
			}

			return Task.CompletedTask;
		}

		protected virtual ProcessStartInfo? GetProcessStartInfo()
		{
			var path = GetPath();
			if (path == string.Empty)
				return null;

			return new()
			{
				FileName = "wt.exe",
				Arguments = $"-d {path}"
			};
		}

		protected string GetPath()
		{
			// Return folder path if there is a folder selected, otherwise the current directory.
			return context.ShellPage?.SlimContentPage?.SelectedItem?.PrimaryItemAttribute is StorageItemTypes.Folder
				? context.ShellPage.SlimContentPage.SelectedItem.ItemPath
				: context.ShellPage?.FilesystemViewModel.WorkingDirectory ?? string.Empty;
		}
	}
}
