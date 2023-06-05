// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class CreateFolderWithSelectionAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "CreateFolderWithSelection".GetLocalizedResource();

		public string Description { get; } = "CreateFolderWithSelectionDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconNewFolder");

		public bool IsExecutable => context.ShellPage is not null && context.HasSelection;

		public CreateFolderWithSelectionAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return UIFilesystemHelpers.CreateFolderWithSelectionAsync(context.ShellPage!);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.HasSelection):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
