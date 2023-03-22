using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class CreateShortcutFromDialogAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Shortcut".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconShortcut");

		public async Task ExecuteAsync()
		{
			if (context.ShellPage is not null)
				await UIFilesystemHelpers.CreateShortcutFromDialogAsync(context.ShellPage);
		}
	}
}
