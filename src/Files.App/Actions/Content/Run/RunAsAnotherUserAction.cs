// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Shell;
using Files.Backend.Helpers;

namespace Files.App.Actions
{
	internal class RunAsAnotherUserAction : ObservableObject, IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		public bool IsExecutable => context.SelectedItem is not null &&
			FileExtensionHelpers.IsExecutableFile(context.SelectedItem.FileExtension);
		public string Label => "BaseLayoutContextFlyoutRunAsAnotherUser/Text".GetLocalizedResource();
		public string Description => "RunAsAnotherUserDescription".GetLocalizedResource();
		public RichGlyph Glyph => new("\uE7EE");

		public RunAsAnotherUserAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			await ContextMenu.InvokeVerb("runasuser", context.SelectedItem!.ItemPath);
		}

		public void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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
