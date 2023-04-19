using Files.App.Commands;
using Files.App.Contexts;
using System.Diagnostics;
using Windows.Storage;

namespace Files.App.Actions
{
	internal class OpenTerminalAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public virtual string Label { get; } = "OpenTerminal".GetLocalizedResource();

		public virtual string Description => "OpenTerminalDescription".GetLocalizedResource();

		public virtual HotKey HotKey { get; } = new(Keys.Oem3, KeyModifiers.Ctrl);

		public RichGlyph Glyph { get; } = new("\uE756");

		private bool isExecutable;
		public bool IsExecutable => isExecutable;

		public OpenTerminalAction()
		{
			isExecutable = GetIsExecutable();
			context.PropertyChanged += Context_PropertyChanged;
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
			if (paths.Length is 0)
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
			if (context.HasSelection)
			{
				return context.SelectedItems!
					.Where(item => item.PrimaryItemAttribute is StorageItemTypes.Folder)
					.Select(item => item.ItemPath)
					.ToArray();
			}
			else if (context.Folder is not null)
				return new string[1] { context.Folder.ItemPath };

			return Array.Empty<string>();
		}

		private bool GetIsExecutable()
		{
			if (context.PageType is ContentPageTypes.None or ContentPageTypes.Home or ContentPageTypes.RecycleBin or ContentPageTypes.ZipFolder)
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
					SetProperty(ref isExecutable, GetIsExecutable(), nameof(IsExecutable));
					break;
			}
		}
	}
}
