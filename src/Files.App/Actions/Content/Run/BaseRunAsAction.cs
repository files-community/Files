// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal abstract class BaseRunAsAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		private readonly string _verb;

		public abstract string Label { get; }

		public abstract string Description { get; }

		public abstract RichGlyph Glyph { get; }

		public bool IsExecutable =>
			ContentPageContext.SelectedItem is not null &&
			(FileExtensionHelpers.IsExecutableFile(ContentPageContext.SelectedItem.FileExtension) ||
			(ContentPageContext.SelectedItem is ShortcutItem shortcut &&
			shortcut.IsExecutable));

		public BaseRunAsAction(string verb)
		{
			_verb = verb;

			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await ContextMenu.InvokeVerb(_verb, ContentPageContext.SelectedItem!.ItemPath);
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
