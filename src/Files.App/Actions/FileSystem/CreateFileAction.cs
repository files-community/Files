// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class CreateFileAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.File.GetLocalizedResource();

		public string Description
			=> Strings.NewFile.GetLocalizedResource();

		public RichGlyph Glyph
			=> new(baseGlyph: "\uE7C3");

		public override bool IsExecutable =>
			context.CanCreateItem &&
			UIHelpers.CanShowDialog;

		public CreateFileAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is not null)
				_ = UIFilesystemHelpers.CreateFileFromDialogResultTypeAsync(AddItemDialogItemType.File, null, context.ShellPage);

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
