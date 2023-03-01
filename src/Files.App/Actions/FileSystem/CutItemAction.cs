using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class CutItemAction : IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "BaseLayoutItemContextFlyoutCut/Text".GetLocalizedResource();

		public HotKey HotKey = new(VirtualKey.X, VirtualKeyModifiers.Control);

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconCut");

		public async Task ExecuteAsync()
		{
			UIFilesystemHelpers.CutItem(context.ShellPage);
		}
	}
}