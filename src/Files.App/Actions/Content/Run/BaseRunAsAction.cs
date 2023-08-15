// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal abstract class BaseRunAsAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;

		private readonly string _verb;

		public abstract string Label { get; }

		public abstract string Description { get; }

		public abstract RichGlyph Glyph { get; }

		public bool IsExecutable =>
			_context.SelectedItem is not null &&
			(FileExtensionHelpers.IsExecutableFile(_context.SelectedItem.FileExtension) ||
			(_context.SelectedItem is ShortcutItem shortcut &&
			shortcut.IsExecutable));

		public BaseRunAsAction(string verb)
		{
			_verb = verb;
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();

			_context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await ContextMenu.InvokeVerb(_verb, _context.SelectedItem!.ItemPath);
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
