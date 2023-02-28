using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.Backend.Enums;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class CreateFolderAction : IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Folder".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(baseGlyph: "\uE8B7");

		public async Task ExecuteAsync()
		{
			UIFilesystemHelpers.CreateFileFromDialogResultType(AddItemDialogItemType.Folder, null!, context.ShellPage!);
		}
	}
}
