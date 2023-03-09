using CommunityToolkit.Mvvm.ComponentModel;
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
	internal class CreateFolderAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Folder".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(baseGlyph: "\uE8B7");

		public bool IsExecutable => context.ShellPage is not null;

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

		public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
