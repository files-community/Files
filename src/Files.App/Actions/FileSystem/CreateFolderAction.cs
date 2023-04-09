using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.Backend.Enums;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class CreateFolderAction : BaseUIAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public override string Label { get; } = "Folder".GetLocalizedResource();

		public override string Description => "TODO: Need to be described.";

		public override RichGlyph Glyph { get; } = new RichGlyph(baseGlyph: "\uE8B7");

		public override bool IsExecutable => context.ShellPage is not null && UIHelpers.CanShowDialog;

		public CreateFolderAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public override Task ExecuteAsync()
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
