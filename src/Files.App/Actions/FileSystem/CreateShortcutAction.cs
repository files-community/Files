// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Actions
{
	internal class CreateShortcutAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "CreateShortcut".GetLocalizedResource();

		public string Description
			=> "CreateShortcutDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconShortcut");

		public override bool IsExecutable =>
			context.HasSelection &&
			context.CanCreateItem &&
			UIHelpers.CanShowDialog;

		public CreateShortcutAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var currentPath = context.ShellPage?.FilesystemViewModel.WorkingDirectory;

			if (App.LibraryManager.TryGetLibrary(currentPath ?? string.Empty, out var library) && !library.IsEmpty)
				currentPath = library.DefaultSaveFolder;

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
