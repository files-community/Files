using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Files.App.Actions
{
	internal class OpenTerminalAction : ObservableObject, IAction
	{
		private readonly string[] emptyStrings = Array.Empty<string>();

		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public virtual string Label { get; } = "OpenTerminal".GetLocalizedResource();

		public virtual HotKey HotKey { get; } = new((VirtualKey)192, VirtualKeyModifiers.Control);

		public RichGlyph Glyph { get; } = new("\uE756");

		public bool IsExecutable =>
			context.PageType is not ContentPageTypes.None &&
			context.PageType is not ContentPageTypes.Home &&
			context.PageType is not ContentPageTypes.RecycleBin &&
			context.PageType is not ContentPageTypes.ZipFolder &&
			!(context.PageType is ContentPageTypes.SearchResults &&
			!context.SelectedItems.Any(item => item.PrimaryItemAttribute is StorageItemTypes.Folder));

		public OpenTerminalAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged(nameof(IsExecutable));
		}

		public Task ExecuteAsync()
		{
			var terminalStartInfo = GetProcessStartInfo();
			if (terminalStartInfo is not null)
			{
				App.Window.DispatcherQueue.TryEnqueue(() =>
				{
					try
					{
						Process.Start(terminalStartInfo);
					}
					catch (Win32Exception)
					{
					}
				});
			}

			return Task.CompletedTask;
		}

		protected virtual ProcessStartInfo? GetProcessStartInfo()
		{
			var paths = GetPaths();
			if (paths.Length == 0)
				return null;

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
				Arguments = args.ToString()
			};
		}

		protected string[] GetPaths()
		{
			var paths = context.ShellPage?.SlimContentPage?.SelectedItems?
				.Where(item => item.PrimaryItemAttribute is StorageItemTypes.Folder)
				.Select(item => item.ItemPath)
				.ToArray();

			if (paths is null || paths.Length == 0)
			{
				paths = context.ShellPage?.FilesystemViewModel.WorkingDirectory is not null
					? new string[1] { context.ShellPage!.FilesystemViewModel.WorkingDirectory }
					: emptyStrings;
			}

			return paths;
		}
	}
}
