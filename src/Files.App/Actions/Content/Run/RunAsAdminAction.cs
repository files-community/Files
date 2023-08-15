// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal class RunAsAdminAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "RunAsAdministrator".GetLocalizedResource();

		public string Description
			=> "RunAsAdminDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE7EF");

		public bool IsExecutable =>
			context.SelectedItem is not null &&
			(FileExtensionHelpers.IsExecutableFile(context.SelectedItem.FileExtension) ||
			(context.SelectedItem is ShortcutItem shortcut &&
			shortcut.IsExecutable));

		public RunAsAdminAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await ContextMenu.InvokeVerb("runas", context.SelectedItem!.ItemPath);
		}

		public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.Folder):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
