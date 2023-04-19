using Files.App.Commands;
using Files.App.Contexts;
using Files.Backend.Enums;

namespace Files.App.Actions
{
	internal class CreateFolderAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Folder".GetLocalizedResource();

		public string Description => "CreateFolderDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(baseGlyph: "\uE8B7");

		public override bool IsExecutable => context.ShellPage is not null && UIHelpers.CanShowDialog;

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
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
