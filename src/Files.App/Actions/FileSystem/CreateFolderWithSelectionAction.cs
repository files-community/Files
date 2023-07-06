// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class CreateFolderWithSelectionAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "CreateFolderWithSelection".GetLocalizedResource();

		public string Description
			=> "CreateFolderWithSelectionDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconNewFolder");

		public bool IsExecutable =>
			context.ShellPage is not null &&
			context.HasSelection;

		public CreateFolderWithSelectionAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

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
