// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class RunAsAnotherUserAction : ObservableObject, IAction
	{
		public IContentPageContext context;

		public bool IsExecutable =>
			context.SelectedItem is not null &&
			(FileExtensionHelpers.IsExecutableFile(context.SelectedItem.FileExtension) ||
			(context.SelectedItem is ShortcutItem shortcut &&
			shortcut.IsExecutable));

		public string Label
			=> "BaseLayoutContextFlyoutRunAsAnotherUser/Text".GetLocalizedResource();

		public string Description
			=> "RunAsAnotherUserDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE7EE");

		public RunAsAnotherUserAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await ContextMenu.InvokeVerb("runasuser", context.SelectedItem!.ItemPath);
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
