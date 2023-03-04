using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.Backend.Enums;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class CreateFolderAction : ObservableObject, IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Folder".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(baseGlyph: "\uE8B7");

		public bool IsExecutable => context.ShellPage is not null;

		public CreateFolderAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}


		public async Task ExecuteAsync()
		{
			UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.Folder, null!, context.ShellPage!);
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
