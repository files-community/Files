using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class CreateShortcutFromDialogAction : IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Shortcut".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconShortcut");

		public async Task ExecuteAsync()
		{
			await UIFilesystemHelpers.CreateShortcutFromDialogAsync(context.ShellPage);
		}
	}
}
