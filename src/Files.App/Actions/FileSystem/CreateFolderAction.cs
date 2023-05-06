// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class CreateFolderAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Folder".GetLocalizedResource();

		public string Description => "CreateFolderDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(baseGlyph: "\uE8B7");

		public override bool IsExecutable => context.CanCreateItem && !context.HasSelection && UIHelpers.CanShowDialog;

		public CreateFolderAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (context.ShellPage is not null)
				UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.Folder, null!, context.ShellPage);
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanCreateItem):
				case nameof(IContentPageContext.HasSelection):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
