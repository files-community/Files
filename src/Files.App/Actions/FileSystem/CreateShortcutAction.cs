using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class CreateShortcutAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "CreateShortcut".GetLocalizedResource();

		public string Description => "CreateShortcutDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconShortcut");

		public override bool IsExecutable => context.HasSelection && context.CanCreateItem && UIHelpers.CanShowDialog;

		public CreateShortcutAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var currentPath = context.ShellPage?.FilesystemViewModel.WorkingDirectory;

			if (App.LibraryManager.TryGetLibrary(currentPath ?? string.Empty, out var library) && !library.IsEmpty)
			{
				currentPath = library.DefaultSaveFolder;
			}

			foreach (ListedItem selectedItem in context.SelectedItems)
			{
				var fileName = string.Format("ShortcutCreateNewSuffix".GetLocalizedResource(), selectedItem.Name) + ".lnk";
				var filePath = Path.Combine(currentPath ?? string.Empty, fileName);

				if (!await FileOperationsHelpers.CreateOrUpdateLinkAsync(filePath, selectedItem.ItemPath))
					await UIFilesystemHelpers.HandleShortcutCannotBeCreated(fileName, selectedItem.ItemPath);
			}
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.CanCreateItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
